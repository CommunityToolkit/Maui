using System.Diagnostics;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>HttpClient extensions.</summary>
static partial class HttpClientExtensions
{
	/// <summary>Get stream from Uri.</summary>
	/// <param name="client"><see href="HttpClient" />.</param>
	/// <param name="uri">Target Uri.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task <see cref="Task{TResult}"/> result.</returns>
	public static async Task<Stream> DownloadStreamAsync(this HttpClient client, Uri uri, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(client);
		ArgumentNullException.ThrowIfNull(uri);

		HttpResponseMessage? response = null;
		try
		{
			response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				Trace.WriteLine($"Could not retrieve {uri}, status code {response.StatusCode}");
				return Stream.Null;
			}

			var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			var responseStream = new ResponseStream(contentStream, response);
			response = null;
			return responseStream;
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"Error getting stream for {uri}: {ex}");
			return Stream.Null;
		}
		finally
		{
			response?.Dispose();
		}
	}

	sealed partial class ResponseStream(Stream innerStream, IDisposable response) : Stream
	{
		public override bool CanRead => innerStream.CanRead;

		public override bool CanSeek => innerStream.CanSeek;

		public override bool CanWrite => innerStream.CanWrite;

		public override long Length => innerStream.Length;

		public override long Position
		{
			get => innerStream.Position;
			set => innerStream.Position = value;
		}

		public override void Flush() => innerStream.Flush();

		public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);

		public override void SetLength(long value) => innerStream.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				innerStream.Dispose();
				response.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}