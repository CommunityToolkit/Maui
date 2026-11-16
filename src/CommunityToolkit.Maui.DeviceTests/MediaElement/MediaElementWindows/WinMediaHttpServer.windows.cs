using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CommunityToolkit.Maui.DeviceTests;

sealed record WinMediaRequest(string Method, string Path, IReadOnlyDictionary<string, string> Headers)
{
	public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
}

sealed class WinMediaResponse
{
	public int StatusCode { get; init; } = 200;

	public string ReasonPhrase { get; init; } = "OK";

	public IReadOnlyList<(string Name, string Value)> Headers { get; init; } = [];

	public byte[] Body { get; init; } = [];

	public bool OmitContentLength { get; init; }

	public TimeSpan Delay { get; init; }

	public static WinMediaResponse NotFound() => new() { StatusCode = 404, ReasonPhrase = "Not Found" };

	public static WinMediaResponse ServerError() => new() { StatusCode = 500, ReasonPhrase = "Internal Server Error" };

	public static WinMediaResponse Text(string text, string contentType) =>
		new() { Headers = [("Content-Type", contentType)], Body = Encoding.UTF8.GetBytes(text) };

	public static WinMediaResponse ServeBytes(WinMediaRequest request, byte[] content, string contentType)
	{
		var headers = new List<(string, string)> { ("Content-Type", contentType), ("Accept-Ranges", "bytes") };

		if (request.Method is "GET" && TryParseRange(request.Header("Range"), content.Length, out var start, out var end))
		{
			headers.Add(("Content-Range", $"bytes {start}-{end}/{content.Length}"));
			return new() { StatusCode = 206, ReasonPhrase = "Partial Content", Headers = headers, Body = content[start..(end + 1)] };
		}

		return new() { Headers = headers, Body = content };
	}

	static bool TryParseRange(string? rangeHeader, int length, out int start, out int end)
	{
		start = end = 0;
		if (rangeHeader is null || !rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var parts = rangeHeader["bytes=".Length..].Split('-');
		if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out start))
		{
			return false;
		}

		end = int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var requestedEnd) ? Math.Min(requestedEnd, length - 1) : length - 1;
		return start <= end;
	}
}

// Raw sockets on the loopback interface: no admin rights or URL reservation are needed.
sealed class WinMediaHttpServer : IAsyncDisposable
{
	readonly TcpListener listener = new(IPAddress.Loopback, 0);
	readonly CancellationTokenSource cancellation = new();
	readonly Lock gate = new();
	readonly List<WinMediaRequest> requests = [];
	readonly List<Task> connections = [];
	Task? acceptLoop;

	public Func<WinMediaRequest, WinMediaResponse> Handler { get; set; } = _ => WinMediaResponse.NotFound();

	public Uri BaseUri => new($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");

	public IReadOnlyList<WinMediaRequest> Requests
	{
		get
		{
			lock (gate)
			{
				return [.. requests];
			}
		}
	}

	public Uri UriFor(string path) => new(BaseUri, path);

	public WinMediaHttpServer Start()
	{
		listener.Start();
		acceptLoop = Task.Run(AcceptLoopAsync);
		return this;
	}

	public async ValueTask DisposeAsync()
	{
		await cancellation.CancelAsync();
		listener.Stop();

		Task[] pending;
		lock (gate)
		{
			pending = [.. connections];
		}

		try
		{
			await Task.WhenAll([acceptLoop ?? Task.CompletedTask, .. pending]).WaitAsync(TimeSpan.FromSeconds(5));
		}
		catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or SocketException or ObjectDisposedException or IOException)
		{
			// Connections that a client aborted are not a test failure.
		}

		cancellation.Dispose();
	}

	async Task AcceptLoopAsync()
	{
		try
		{
			while (!cancellation.IsCancellationRequested)
			{
				var client = await listener.AcceptTcpClientAsync(cancellation.Token);
				var connection = Task.Run(() => HandleConnectionAsync(client));
				lock (gate)
				{
					connections.Add(connection);
				}
			}
		}
		catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
		{
			// The server was stopped.
		}
	}

	async Task HandleConnectionAsync(TcpClient client)
	{
		using (client)
		{
			try
			{
				var stream = client.GetStream();
				var request = await ReadRequestAsync(stream, cancellation.Token);
				if (request is null)
				{
					return;
				}

				lock (gate)
				{
					requests.Add(request);
				}

				var response = Handler(request);
				if (response.Delay > TimeSpan.Zero)
				{
					await Task.Delay(response.Delay, cancellation.Token);
				}

				await WriteResponseAsync(stream, request, response, cancellation.Token);
			}
			catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException or InvalidOperationException)
			{
				// The client went away (it cancelled, or it read what it needed and closed the connection).
			}
		}
	}

	static async Task<WinMediaRequest?> ReadRequestAsync(NetworkStream stream, CancellationToken token)
	{
		var text = new StringBuilder();
		var buffer = new byte[1024];

		while (!text.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
		{
			var read = await stream.ReadAsync(buffer, token);
			if (read is 0 || text.Length > 16 * 1024)
			{
				return null;
			}

			text.Append(Encoding.ASCII.GetString(buffer, 0, read));
		}

		var lines = text.ToString().Split("\r\n", StringSplitOptions.None);
		var requestLine = lines[0].Split(' ');
		if (requestLine.Length < 2)
		{
			return null;
		}

		var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var line in lines.Skip(1).TakeWhile(line => line.Length > 0))
		{
			var separator = line.IndexOf(':', StringComparison.Ordinal);
			if (separator > 0)
			{
				headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
			}
		}

		return new WinMediaRequest(requestLine[0], requestLine[1], headers);
	}

	static async Task WriteResponseAsync(NetworkStream stream, WinMediaRequest request, WinMediaResponse response, CancellationToken token)
	{
		var head = new StringBuilder();
		head.Append(CultureInfo.InvariantCulture, $"HTTP/1.1 {response.StatusCode} {response.ReasonPhrase}\r\n");
		foreach (var (name, value) in response.Headers)
		{
			head.Append(CultureInfo.InvariantCulture, $"{name}: {value}\r\n");
		}

		if (!response.OmitContentLength)
		{
			head.Append(CultureInfo.InvariantCulture, $"Content-Length: {response.Body.Length}\r\n");
		}

		head.Append("Connection: close\r\n\r\n");

		await stream.WriteAsync(Encoding.ASCII.GetBytes(head.ToString()), token);
		if (request.Method is not "HEAD" && response.Body.Length > 0)
		{
			await stream.WriteAsync(response.Body, token);
		}

		await stream.FlushAsync(token);
	}
}