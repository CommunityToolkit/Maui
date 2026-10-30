using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CommunityToolkit.Maui.DeviceTests;

// Listens on the loopback interface only, so tests of HTTP behavior (custom request headers, artwork download) never touch a real network.
// Android blocks cleartext HTTP unless the app allows it: the MediaElement device-test app sets android:usesCleartextTraffic in its
// AndroidManifest.xml for this server.
public sealed class LoopbackHttpServer : IDisposable
{
	readonly TcpListener listener = new(IPAddress.Loopback, 0);
	readonly CancellationTokenSource cancellationTokenSource = new();
	readonly IReadOnlyDictionary<string, (byte[] Body, string ContentType)> routes;
	readonly IReadOnlySet<string> pathsWithoutContentLength;
	readonly ConcurrentQueue<string> requests = new();
	readonly ConcurrentDictionary<string, TaskCompletionSource> heldPaths = new();

	public LoopbackHttpServer(IReadOnlyDictionary<string, (byte[] Body, string ContentType)> routes, IReadOnlySet<string>? pathsWithoutContentLength = null)
	{
		this.routes = routes;
		this.pathsWithoutContentLength = pathsWithoutContentLength ?? new HashSet<string>();
		listener.Start();
		_ = Task.Run(AcceptLoop);
	}

	public IReadOnlyCollection<string> Requests => requests;

	public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

	public Uri GetUri(string path) => new($"http://127.0.0.1:{Port}{path}");

	public void Dispose()
	{
		foreach (var hold in heldPaths.Values)
		{
			hold.TrySetResult();
		}

		cancellationTokenSource.Cancel();
		listener.Stop();
		cancellationTokenSource.Dispose();
	}

	public string? GetRequestHeader(string method, string path, string headerName)
	{
		var prefix = $"{method} {path} ";
		var request = requests.FirstOrDefault(r => r.StartsWith(prefix, StringComparison.Ordinal));
		if (request is null)
		{
			return null;
		}

		foreach (var line in request.Split("\r\n"))
		{
			var separator = line.IndexOf(':');
			if (separator > 0 && string.Equals(line[..separator], headerName, StringComparison.OrdinalIgnoreCase))
			{
				return line[(separator + 1)..].Trim();
			}
		}

		return null;
	}

	public bool HasRequest(string method, string path) => requests.Any(r => r.StartsWith($"{method} {path} ", StringComparison.Ordinal));

	// Stands in for a slow server, without a delay that a test would have to guess.
	public TaskCompletionSource HoldResponses(string path) =>
		heldPaths.GetOrAdd(path, static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

	async Task AcceptLoop()
	{
		while (!cancellationTokenSource.IsCancellationRequested)
		{
			TcpClient client;
			try
			{
				client = await listener.AcceptTcpClientAsync(cancellationTokenSource.Token);
			}
			catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
			{
				return;
			}

			_ = Task.Run(() => HandleClient(client));
		}
	}

	async Task HandleClient(TcpClient client)
	{
		using (client)
		{
			try
			{
				var stream = client.GetStream();
				var buffer = new byte[8192];
				var header = new StringBuilder();
				while (!header.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
				{
					var read = await stream.ReadAsync(buffer, cancellationTokenSource.Token);
					if (read == 0)
					{
						return;
					}

					header.Append(Encoding.ASCII.GetString(buffer, 0, read));
				}

				var request = header.ToString();
				requests.Enqueue(request);

				var requestLine = request.Split("\r\n")[0].Split(' ');
				var method = requestLine[0];
				var path = requestLine[1];

				if (heldPaths.TryGetValue(path, out var hold))
				{
					await hold.Task.WaitAsync(cancellationTokenSource.Token);
				}

				if (!routes.TryGetValue(path, out var route))
				{
					await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"), cancellationTokenSource.Token);
					return;
				}

				var contentLength = pathsWithoutContentLength.Contains(path) ? string.Empty : $"Content-Length: {route.Body.Length}\r\n";
				var responseHeader = $"HTTP/1.1 200 OK\r\nContent-Type: {route.ContentType}\r\n{contentLength}Accept-Ranges: none\r\nConnection: close\r\n\r\n";
				await stream.WriteAsync(Encoding.ASCII.GetBytes(responseHeader), cancellationTokenSource.Token);
				if (!string.Equals(method, "HEAD", StringComparison.Ordinal))
				{
					await stream.WriteAsync(route.Body, cancellationTokenSource.Token);
				}

				await stream.FlushAsync(cancellationTokenSource.Token);
			}
			catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or IOException or SocketException)
			{
				// The client (ExoPlayer or HttpClient) closed the connection, or the test ended.
			}
		}
	}
}