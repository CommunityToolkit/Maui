using CommunityToolkit.Maui.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class StreamExtensionsTests : BaseTest
{
    const string defaultMimeType = "application/octet-stream";

    [Fact]
    public void GetMimeType_NullStream_ThrowsArgumentNullException()
    {
        Stream? stream = null;
        Assert.Throws<ArgumentNullException>(() => stream!.GetMimeType());
    }

    [Fact]
    public void GetMimeType_EmptyStream_ReturnsDefault()
    {
        using var stream = new MemoryStream([]);
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    [Fact]
    public void GetMimeType_StreamSmallerThanFourBytes_ReturnsDefault()
    {
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    [Fact]
    public void GetMimeType_UnknownHeader_ReturnsDefault()
    {
        using var stream = new MemoryStream([0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04]);
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    [Fact]
    public void GetMimeType_NonReadableStream_ReturnsDefault()
    {
        using var stream = new NonReadableStream();
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    [Fact]
    public void GetMimeType_NonSeekableStream_ReturnsDefault()
    {
        using var inner = new MemoryStream(BuildFtypHeader("isom"));
        using var stream = new NonSeekableStream(inner);
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    [Fact]
    public void GetMimeType_RestoresOriginalStreamPosition()
    {
        using var stream = new MemoryStream(BuildFtypHeader("isom"));
        stream.Position = 5;

        stream.GetMimeType();

        stream.Position.Should().Be(5);
    }

    [Fact]
    public void GetMimeType_DoesNotRewindWhenAtZero()
    {
        using var stream = new MemoryStream(BuildFtypHeader("isom"));
        stream.Position = 0;

        stream.GetMimeType().Should().Be("video/mp4");

        stream.Position.Should().Be(0);
    }

    [Theory]
    [InlineData("isom", "video/mp4")]
    [InlineData("mp42", "video/mp4")]
    [InlineData("avc1", "video/mp4")]
    [InlineData("M4A ", "audio/mp4")]
    [InlineData("M4B ", "audio/mp4")]
    [InlineData("M4P ", "audio/mp4")]
    [InlineData("qt  ", "video/quicktime")]
    [InlineData("3gp4", "video/3gpp")]
    [InlineData("3gp5", "video/3gpp")]
    [InlineData("3gp6", "video/3gpp")]
    [InlineData("3gp7", "video/3gpp")]
    [InlineData("3gpp", "video/3gpp")]
    [InlineData("3g2a", "video/3gpp2")]
    [InlineData("3g2b", "video/3gpp2")]
    [InlineData("3g2c", "video/3gpp2")]
    public void GetMimeType_FtypBrand_ReturnsExpectedMimeType(string brand, string expected)
    {
        using var stream = new MemoryStream(BuildFtypHeader(brand));
        stream.GetMimeType().Should().Be(expected);
    }

    [Fact]
    public void GetMimeType_WebMHeader_ReturnsVideoWebm()
    {
        using var stream = new MemoryStream([0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02, 0x03, 0x04]);
        stream.GetMimeType().Should().Be("video/webm");
    }

    [Fact]
    public void GetMimeType_AviRiffHeader_ReturnsVideoXMsVideo()
    {
        byte[] header =
        [
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            0x00, 0x00, 0x00, 0x00,
            (byte)'A', (byte)'V', (byte)'I', (byte)' '
        ];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("video/x-msvideo");
    }

    [Fact]
    public void GetMimeType_WaveRiffHeader_ReturnsAudioWav()
    {
        byte[] header =
        [
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            0x00, 0x00, 0x00, 0x00,
            (byte)'W', (byte)'A', (byte)'V', (byte)'E'
        ];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("audio/wav");
    }

    [Fact]
    public void GetMimeType_RiffWithUnknownSubtype_ReturnsDefault()
    {
        byte[] header =
        [
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            0x00, 0x00, 0x00, 0x00,
            (byte)'X', (byte)'X', (byte)'X', (byte)'X'
        ];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    [Fact]
    public void GetMimeType_OggHeader_ReturnsApplicationOgg()
    {
        byte[] header = [(byte)'O', (byte)'g', (byte)'g', (byte)'S', 0x00, 0x02, 0x00, 0x00];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("application/ogg");
    }

    [Fact]
    public void GetMimeType_FlacHeader_ReturnsAudioFlac()
    {
        byte[] header = [(byte)'f', (byte)'L', (byte)'a', (byte)'C', 0x00, 0x00, 0x00, 0x22];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("audio/flac");
    }

    [Fact]
    public void GetMimeType_Mp3WithId3Header_ReturnsAudioMpeg()
    {
        byte[] header = [(byte)'I', (byte)'D', (byte)'3', 0x04, 0x00, 0x00, 0x00, 0x00];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("audio/mpeg");
    }

    [Theory]
    [InlineData(0xE0)]
    [InlineData(0xE3)]
    [InlineData(0xF0)]
    [InlineData(0xFB)]
    [InlineData(0xFF)]
    public void GetMimeType_Mp3FrameSync_ReturnsAudioMpeg(byte secondByte)
    {
        byte[] header = [0xFF, secondByte, 0x00, 0x00];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("audio/mpeg");
    }

    [Fact]
    public void GetMimeType_MpegProgramStream_ReturnsVideoMpeg()
    {
        byte[] header = [0x00, 0x00, 0x01, 0xBA, 0x44, 0x00, 0x04, 0x00];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("video/mpeg");
    }

    [Fact]
    public void GetMimeType_MpegTransportStream_ReturnsVideoMp2t()
    {
        byte[] header = [0x47, 0x40, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("video/mp2t");
    }

    [Fact]
    public void GetMimeType_AsfHeader_ReturnsVideoXMsAsf()
    {
        byte[] header =
        [
            0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11,
            0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C
        ];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("video/x-ms-asf");
    }

    [Fact]
    public void GetMimeType_FlvHeader_ReturnsVideoXFlv()
    {
        byte[] header = [(byte)'F', (byte)'L', (byte)'V', 0x01, 0x05, 0x00, 0x00, 0x00];
        using var stream = new MemoryStream(header);
        stream.GetMimeType().Should().Be("video/x-flv");
    }

    [Fact]
    public void GetMimeType_StreamThrowingIOException_ReturnsDefault()
    {
        using var stream = new ThrowingStream(new IOException("boom"));
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    [Fact]
    public void GetMimeType_StreamThrowingNotSupportedException_ReturnsDefault()
    {
        using var stream = new ThrowingStream(new NotSupportedException("boom"));
        stream.GetMimeType().Should().Be(defaultMimeType);
    }

    static byte[] BuildFtypHeader(string brand)
    {
        if (brand.Length is not 4)
        {
            throw new ArgumentException("Brand must be 4 characters", nameof(brand));
        }

        return
        [
            0x00, 0x00, 0x00, 0x20,
            (byte)'f', (byte)'t', (byte)'y', (byte)'p',
            (byte)brand[0], (byte)brand[1], (byte)brand[2], (byte)brand[3],
            0x00, 0x00, 0x02, 0x00
        ];
    }

    sealed class NonReadableStream : MemoryStream
    {
        public override bool CanRead => false;
    }

    sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    sealed class ThrowingStream(Exception exceptionToThrow) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => 1024;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw exceptionToThrow;
        public override long Seek(long offset, SeekOrigin origin) => Position = offset;
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
