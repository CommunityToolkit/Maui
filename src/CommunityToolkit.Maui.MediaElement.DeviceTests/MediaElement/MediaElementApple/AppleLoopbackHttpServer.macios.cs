using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// A minimal HTTP/1.1 server on the loopback interface (no network traffic leaves the machine). It serves byte arrays by path,
/// honours <c>Range</c> requests the way AVFoundation issues them, and records every request so tests can assert what the
/// platform player actually sent (for example the custom HTTP headers of <c>UriMediaSource.HttpHeaders</c>).
/// </summary>
sealed class AppleLoopbackHttpServer : IDisposable
{
	readonly TcpListener listener = new(IPAddress.Loopback, 0);
	readonly Dictionary<string, (byte[] Content, string ContentType)> resources = new(StringComparer.Ordinal);
	readonly List<AppleHttpRequest> requests = [];
	readonly CancellationTokenSource cancellation = new();
	Task? acceptLoop;

	public AppleLoopbackHttpServer()
	{
		listener.Start();
		Port = ((IPEndPoint)listener.LocalEndpoint).Port;
		acceptLoop = Task.Run(AcceptAsync);
	}

	public int Port { get; }

	public IReadOnlyList<AppleHttpRequest> Requests
	{
		get
		{
			lock (requests)
			{
				return [.. requests];
			}
		}
	}

	public void Add(string path, byte[] content, string contentType)
	{
		lock (resources)
		{
			resources[path] = (content, contentType);
		}
	}

	public Uri GetUri(string path) => new($"http://127.0.0.1:{Port}{path}");

	public void Dispose()
	{
		cancellation.Cancel();
		listener.Stop();
		try
		{
			acceptLoop?.Wait(TimeSpan.FromSeconds(2));
		}
		catch (AggregateException)
		{
		}

		cancellation.Dispose();
	}

	async Task AcceptAsync()
	{
		while (!cancellation.IsCancellationRequested)
		{
			TcpClient client;
			try
			{
				client = await listener.AcceptTcpClientAsync(cancellation.Token);
			}
			catch (Exception) when (cancellation.IsCancellationRequested)
			{
				return;
			}
			catch (SocketException)
			{
				return;
			}

			_ = Task.Run(() => ServeAsync(client));
		}
	}

	async Task ServeAsync(TcpClient client)
	{
		using (client)
		{
			try
			{
				var stream = client.GetStream();
				var head = await ReadHeadAsync(stream);
				if (head is null)
				{
					return;
				}

				var lines = head.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
				var requestLine = lines[0].Split(' ');
				var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				foreach (var line in lines.Skip(1))
				{
					var separator = line.IndexOf(':');
					if (separator > 0)
					{
						headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
					}
				}

				var request = new AppleHttpRequest(requestLine[0], requestLine[1], headers);
				lock (requests)
				{
					requests.Add(request);
				}

				(byte[] Content, string ContentType) resource;
				bool found;
				lock (resources)
				{
					found = resources.TryGetValue(request.Path, out resource);
				}

				if (!found)
				{
					await WriteAsync(stream, "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n", []);
					return;
				}

				var content = resource.Content;
				var start = 0;
				var end = content.Length - 1;
				var status = "200 OK";
				var extra = string.Empty;
				if (headers.TryGetValue("Range", out var range) && range.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
				{
					var parts = range["bytes=".Length..].Split('-');
					if (parts[0].Length > 0)
					{
						start = Math.Min(int.Parse(parts[0]), content.Length);
						end = parts.Length > 1 && parts[1].Length > 0 ? Math.Min(int.Parse(parts[1]), content.Length - 1) : content.Length - 1;
					}
					else if (parts.Length > 1 && parts[1].Length > 0)
					{
						start = Math.Max(0, content.Length - int.Parse(parts[1]));
					}

					status = "206 Partial Content";
					extra = $"Content-Range: bytes {start}-{end}/{content.Length}\r\n";
				}

				var length = Math.Max(0, end - start + 1);
				var header = $"HTTP/1.1 {status}\r\nContent-Type: {resource.ContentType}\r\nContent-Length: {length}\r\nAccept-Ranges: bytes\r\n{extra}Connection: close\r\n\r\n";
				await WriteAsync(stream, header, request.Method == "HEAD" ? [] : content.AsMemory(start, length).ToArray());
			}
			catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
			{
				// The player closes connections it no longer needs.
			}
		}
	}

	static async Task<string?> ReadHeadAsync(NetworkStream stream)
	{
		var buffer = new List<byte>();
		var one = new byte[1];
		while (await stream.ReadAsync(one) == 1)
		{
			buffer.Add(one[0]);
			var count = buffer.Count;
			if (count >= 4 && buffer[count - 4] == '\r' && buffer[count - 3] == '\n' && buffer[count - 2] == '\r' && buffer[count - 1] == '\n')
			{
				return Encoding.ASCII.GetString([.. buffer]);
			}
		}

		return null;
	}

	static async Task WriteAsync(NetworkStream stream, string head, byte[] body)
	{
		await stream.WriteAsync(Encoding.ASCII.GetBytes(head));
		if (body.Length > 0)
		{
			await stream.WriteAsync(body);
		}

		await stream.FlushAsync();
	}
}

/// <summary>One request received by <see cref="AppleLoopbackHttpServer"/>.</summary>
sealed record AppleHttpRequest(string Method, string Path, IReadOnlyDictionary<string, string> Headers);