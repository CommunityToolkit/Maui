#nullable enable
using System;
using System.Globalization;
using System.IO;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class ByteArrayToImageSourceConverter_Tests
	{
		[Test]
		public void ByteArrayToImageSourceConverter()
		{
			var byteArray = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

			var memoryStream = new MemoryStream(byteArray);

			var expectedValue = ImageSource.FromStream(() => memoryStream);

			var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

			var result = byteArrayToImageSourceConverter.Convert(byteArray, typeof(ByteArrayToImageSourceConverter), null, CultureInfo.CurrentCulture);

			Assert.IsTrue(StreamEquals(GetStreamFromImageSource((ImageSource?)result), memoryStream));
		}


		[TestCase("Random String Value")]
		public void InvalidConverterValuesReturnsNull(object value)
		{
			var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

			Assert.Throws<ArgumentException>(() => byteArrayToImageSourceConverter.Convert(value, typeof(ByteArrayToImageSourceConverter), null, CultureInfo.CurrentCulture));
		}

		Stream? GetStreamFromImageSource(ImageSource? imageSource)
		{
			var streamImageSource = (StreamImageSource?)imageSource;

			var cancellationToken = System.Threading.CancellationToken.None;
			var task = streamImageSource?.Stream(cancellationToken);
			return task?.Result;
		}

		bool StreamEquals(Stream? a, Stream? b)
		{
			if (a == b)
				return true;

			if (a == null
				|| b == null
				|| a.Length != b.Length)
			{
				return false;
			}

			for (var i = 0; i < a.Length; i++)
			{
				if (a.ReadByte() != b.ReadByte())
					return false;
			}

			return true;
		}
	}
}