namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Stream"/> on Windows platform.
/// </summary>
static class StreamExtensions
{
	const string defaultMimeType = "application/octet-stream";

	/// <summary>
	/// Gets the MIME type for a stream by inspecting its leading bytes (magic numbers).
	/// </summary>
	/// <param name="stream">The stream to get the MIME type for.</param>
	/// <returns>
	/// A MIME type string that matches the detected media container/codec, or
	/// <c>"application/octet-stream"</c> when the type cannot be determined.
	/// </returns>
	/// <remarks>
	/// When the stream is seekable, the original position is restored after inspection.
	/// Non-seekable streams cannot be sniffed and will return the default MIME type.
	/// </remarks>
	internal static string GetMimeType(this Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);

		if (!stream.CanRead || !stream.CanSeek)
		{
			return defaultMimeType;
		}

		const int headerSize = 16;
		var originalPosition = stream.Position;

		try
		{
			stream.Position = 0;

			Span<byte> header = stackalloc byte[headerSize];
			var bytesRead = stream.Read(header);
			if (bytesRead < 4)
			{
				return defaultMimeType;
			}

			header = header[..bytesRead];
			return DetectMimeType(header);
		}
		catch (IOException)
		{
			return defaultMimeType;
		}
		catch (NotSupportedException)
		{
			return defaultMimeType;
		}
		finally
		{
			try
			{
				stream.Position = originalPosition;
			}
			catch (IOException)
			{
			}
			catch (NotSupportedException)
			{
			}
		}
	}

	static string DetectMimeType(ReadOnlySpan<byte> header)
	{
		// MP4 / M4A / 3GP / QuickTime: bytes 4-7 == "ftyp"
		if (header.Length >= 12
			&& header[4] is (byte)'f' && header[5] is (byte)'t' && header[6] is (byte)'y' && header[7] is (byte)'p')
		{
			var brand = System.Text.Encoding.ASCII.GetString(header.Slice(8, 4)).TrimEnd();

			return brand switch
			{
				"M4A" or "M4B" or "M4P" => "audio/mp4",
				"qt" => "video/quicktime",
				"3gp4" or "3gp5" or "3gp6" or "3gp7" or "3gpp" => "video/3gpp",
				"3g2a" or "3g2b" or "3g2c" => "video/3gpp2",
				_ => "video/mp4",
			};
		}

		// WebM / Matroska (EBML): 1A 45 DF A3
		if (header.Length >= 4 && header[0] is 0x1A && header[1] is 0x45 && header[2] is 0xDF && header[3] is 0xA3)
		{
			return "video/webm";
		}

		// RIFF containers: AVI ("AVI ") or WAV ("WAVE")
		if (header.Length >= 12
			&& header[0] is (byte)'R' && header[1] is (byte)'I' && header[2] is (byte)'F' && header[3] is (byte)'F')
		{
			if (header[8] is (byte)'A' && header[9] is (byte)'V' && header[10] is (byte)'I' && header[11] is (byte)' ')
			{
				return "video/x-msvideo";
			}

			if (header[8] is (byte)'W' && header[9] is (byte)'A' && header[10] is (byte)'V' && header[11] is (byte)'E')
			{
				return "audio/wav";
			}
		}

		// OGG: "OggS"
		if (header.Length >= 4
			&& header[0] is (byte)'O' && header[1] is (byte)'g' && header[2] is (byte)'g' && header[3] is (byte)'S')
		{
			return "application/ogg";
		}

		// FLAC: "fLaC"
		if (header.Length >= 4
			&& header[0] is (byte)'f' && header[1] is (byte)'L' && header[2] is (byte)'a' && header[3] is (byte)'C')
		{
			return "audio/flac";
		}

		// MP3: ID3 tag or MPEG audio frame sync (0xFF 0xEx/0xFx)
		if (header.Length >= 3
			&& header[0] is (byte)'I' && header[1] is (byte)'D' && header[2] is (byte)'3')
		{
			return "audio/mpeg";
		}

		if (header.Length >= 2 && header[0] is 0xFF && (header[1] & 0xE0) is 0xE0)
		{
			return "audio/mpeg";
		}

		// MPEG-PS / MPEG-TS
		if (header.Length >= 4 && header[0] is 0x00 && header[1] is 0x00 && header[2] is 0x01 && header[3] is 0xBA)
		{
			return "video/mpeg";
		}

		if (header.Length >= 1 && header[0] is 0x47)
		{
			return "video/mp2t";
		}

		// ASF / WMV / WMA: 30 26 B2 75 8E 66 CF 11
		if (header.Length >= 8
			&& header[0] is 0x30 && header[1] is 0x26 && header[2] is 0xB2 && header[3] is 0x75
			&& header[4] is 0x8E && header[5] is 0x66 && header[6] is 0xCF && header[7] is 0x11)
		{
			return "video/x-ms-asf";
		}

		// FLV: "FLV"
		if (header.Length >= 3
			&& header[0] is (byte)'F' && header[1] is (byte)'L' && header[2] is (byte)'V')
		{
			return "video/x-flv";
		}

		return defaultMimeType;
	}
}
