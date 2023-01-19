﻿using System.Globalization;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;
using Xunit;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.UnitTests.Views.MediaElement;
public class MediaSourceConverterTests
{
	[Fact]
	public void MediaSourceConverterShouldConvertFromStringType()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSourceCanConvert = mediaSourceConverter.CanConvertFrom(null, typeof(string));

		mediaSourceCanConvert.Should().BeTrue();
	}

	[Fact]
	public void MediaSourceConverterShouldConvertToStringType()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSourceCanConvert = mediaSourceConverter.CanConvertTo(null, typeof(string));

		mediaSourceCanConvert.Should().BeTrue();
	}

	[Fact]
	public void MediaSourceConverterWithEmbedPrefixShouldBeResourceMediaSource()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSource = mediaSourceConverter.ConvertFrom(null, CultureInfo.InvariantCulture, "embed://file.mp4");

		mediaSource.Should().BeAssignableTo<ResourceMediaSource>();
	}

	[Fact]
	public void MediaSourceConverterWithFileSystemPrefixShouldBeResourceMediaSource()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSource = mediaSourceConverter.ConvertFrom(null, CultureInfo.InvariantCulture, "filesystem://file.mp4");

		mediaSource.Should().BeAssignableTo<FileMediaSource>();
	}
}
