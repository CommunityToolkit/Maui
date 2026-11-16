#pragma warning disable CS0618 // These tests pin the obsolete MetadataArtworkUrl, which keeps working
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaElementMetadataArtworkResolverTests : BaseViewTest
{
	public static TheoryData<string> CultureNames { get; } = ["en-US", "tr-TR", "az-Latn-AZ"];

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void BlankMetadataArtworkUrlHasNoArtwork(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkResolver.FromMetadataArtworkUrl(null).Should().BeNull();
		MetadataArtworkResolver.FromMetadataArtworkUrl(string.Empty).Should().BeNull();
		MetadataArtworkResolver.FromMetadataArtworkUrl("   ").Should().BeNull();
		MetadataArtworkResolver.FromMetadataArtworkUrl("\t\n").Should().BeNull();
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void HttpMetadataArtworkUrlBecomesAUriSource(string cultureName)
	{
		SetCurrentCulture(cultureName);

		(string Url, string TrimmedUrl, string Scheme)[] rows =
		[
			("https://example.com/art.png", "https://example.com/art.png", "https"),
			("http://example.com/a%20b.png?size=2", "http://example.com/a%20b.png?size=2", "http"),
			("HTTPS://Example.COM/Art.png", "HTTPS://Example.COM/Art.png", "https"),
			// Uri.TryCreate trimmed the string in 17.0.0, so padded URLs kept working
			("  https://example.com/art.png", "https://example.com/art.png", "https"),
			("https://example.com/art.png  ", "https://example.com/art.png", "https")
		];

		foreach (var (url, trimmedUrl, scheme) in rows)
		{
			var source = MetadataArtworkResolver.FromMetadataArtworkUrl(url).Should().BeOfType<UriMediaSource>().Subject;

			source.Uri.Should().Be(new Uri(trimmedUrl));
			string.Equals(source.Uri?.Scheme, scheme, StringComparison.Ordinal).Should().BeTrue(url);
		}
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void FileUriMetadataArtworkUrlBecomesAFileUriSource(string cultureName)
	{
		SetCurrentCulture(cultureName);
		var path = Path.Combine(Path.GetTempPath(), "art.png");
		var fileUri = new Uri(path).AbsoluteUri;
		var schemeSeparatorLength = "file:".Length;
		var uriWithoutScheme = fileUri[schemeSeparatorLength..];
		var hashPath = Path.Combine(Path.GetTempPath(), "art#1.png");

		// tr-TR lower-cases "FILE" to "fıle", so a culture-sensitive scheme check would not recognize these
		(string Url, string ExpectedLocalPath)[] rows =
		[
			(fileUri, path),
			("FILE:" + uriWithoutScheme, path),
			("File:" + uriWithoutScheme, path),
			(" " + fileUri, path),
			(new Uri(hashPath).AbsoluteUri, hashPath)
		];

		foreach (var (url, expectedLocalPath) in rows)
		{
			var source = MetadataArtworkResolver.FromMetadataArtworkUrl(url).Should().BeOfType<UriMediaSource>().Subject;

			source.Uri?.IsFile.Should().BeTrue(url);
			source.Uri?.LocalPath.Should().Be(expectedLocalPath, url);
		}
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void AbsolutePathMetadataArtworkUrlKeepsThePathAsWritten(string cultureName)
	{
		Assert.SkipWhen(OperatingSystem.IsWindows(), "A rooted path is relative on Windows");
		SetCurrentCulture(cultureName);

		(string Url, string ExpectedPath)[] rows =
		[
			("/data/user/0/app/cache/art.png", "/data/user/0/app/cache/art.png"),
			// "#" is part of the file name, not a fragment
			("/data/art#1.png", "/data/art#1.png"),
			("/data/IMAGES/İcon.png", "/data/IMAGES/İcon.png"),
			("/data/a\\b.png", "/data/a/b.png"),
			("  /data/art.png", "/data/art.png"),
			("/data/art.png  ", "/data/art.png")
		];

		foreach (var (url, expectedPath) in rows)
		{
			var source = MetadataArtworkResolver.FromMetadataArtworkUrl(url).Should().BeOfType<FileMediaSource>().Subject;

			source.Path.Should().Be(expectedPath, url);
		}
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void DosAndUncPathMetadataArtworkUrlBecomesAFileSource(string cultureName)
	{
		SetCurrentCulture(cultureName);

		string[] urls = [@"C:\Users\Public\art.png", "C:/Users/art.png", @"\\server\share\art.png"];

		foreach (var url in urls)
		{
			var source = MetadataArtworkResolver.FromMetadataArtworkUrl(url).Should().BeOfType<FileMediaSource>().Subject;

			source.Path.Should().Be(url.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
		}
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void PaddedDosPathMetadataArtworkUrlBecomesAFileSourceOfThePathWithoutThePadding(string cultureName)
	{
		SetCurrentCulture(cultureName);

		var source = MetadataArtworkResolver.FromMetadataArtworkUrl(@"  C:\Users\Public\art.png").Should().BeOfType<FileMediaSource>().Subject;

		source.Path.Should().Be(@"C:\Users\Public\art.png".Replace('\\', Path.DirectorySeparatorChar));
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void RootedPathMetadataArtworkUrlIsAnAppPackagePathOnWindows(string cultureName)
	{
		Assert.SkipUnless(OperatingSystem.IsWindows(), "A rooted path is an absolute file path outside of Windows");
		SetCurrentCulture(cultureName);

		MetadataArtworkResolver.FromMetadataArtworkUrl("/art.png").Should().BeOfType<ResourceMediaSource>().Which.Path.Should().Be("/art.png");
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void MetadataArtworkUrlOfAnotherSchemeBecomesAUriSource(string cultureName)
	{
		SetCurrentCulture(cultureName);
		var longDataUri = "data:image/png;base64," + new string('A', 70_000);

		(string Url, string Scheme)[] rows =
		[
			("ftp://127.0.0.1/art.png", "ftp"),
			("content://media/external/images/1", "content"),
			("data:image/png;base64,iVBORw0KGgo=", "data"),
			(longDataUri, "data"),
			("ms-appx:///Assets/art.png", "ms-appx"),
			("ms-appdata:///local/art.png", "ms-appdata"),
			("ht://127.0.0.1/art.png", "ht")
		];

		foreach (var (url, scheme) in rows)
		{
			var source = MetadataArtworkResolver.FromMetadataArtworkUrl(url).Should().BeOfType<UriMediaSource>().Subject;

			source.Uri?.OriginalString.Should().Be(url);
			string.Equals(source.Uri?.Scheme, scheme, StringComparison.Ordinal).Should().BeTrue(url[..Math.Min(url.Length, 40)]);
		}
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void RelativeMetadataArtworkUrlBecomesAnAppPackageSource(string cultureName)
	{
		SetCurrentCulture(cultureName);

		(string Url, string TrimmedUrl)[] rows =
		[
			("exoplayer/art.png", "exoplayer/art.png"),
			(@"exoplayer\art.png", @"exoplayer\art.png"),
			("Assets/art.png", "Assets/art.png"),
			("art.png", "art.png"),
			("images/İcon.png", "images/İcon.png"),
			("IMAGES/ICON.PNG", "IMAGES/ICON.PNG"),
			("not a uri at all", "not a uri at all"),
			("file:foo.png", "file:foo.png"),
			("http:/host/x.png", "http:/host/x.png"),
			// Uri.TryCreate trimmed the string in 17.0.0, so padded paths kept working
			("  exoplayer/art.png", "exoplayer/art.png"),
			("art.png  ", "art.png")
		];

		foreach (var (url, trimmedUrl) in rows)
		{
			var source = MetadataArtworkResolver.FromMetadataArtworkUrl(url).Should().BeOfType<ResourceMediaSource>().Subject;
			var expectedSource = (ResourceMediaSource)MediaSource.FromResource(trimmedUrl);

			string.Equals(source.Path, expectedSource.Path, StringComparison.Ordinal).Should().BeTrue(url);
		}
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void MetadataArtworkUrlThatIsNeitherAbsoluteNorRelativeHasNoArtwork(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkResolver.FromMetadataArtworkUrl("http://").Should().BeNull();
		MetadataArtworkResolver.FromMetadataArtworkUrl("http://[not a host/art.png").Should().BeNull();
	}

	[Fact]
	public void ResolveWithoutArtworkHasNoSource()
	{
		MediaElement mediaElement = new();

		var resolved = MetadataArtworkResolver.Resolve(mediaElement);

		resolved.Source.Should().BeNull();
		resolved.MetadataArtworkUrl.Should().BeEmpty();
	}

	[Fact]
	public void ResolveUsesMetadataArtworkUrlWhileMetadataArtworkSourceIsNull()
	{
		MediaElement mediaElement = new() { MetadataArtworkUrl = "https://example.com/art.png" };

		var resolved = MetadataArtworkResolver.Resolve(mediaElement);

		resolved.Source.Should().BeOfType<UriMediaSource>().Which.Uri.Should().Be(new Uri("https://example.com/art.png"));
		resolved.MetadataArtworkUrl.Should().Be("https://example.com/art.png");
	}

	[Fact]
	public void ResolveKeepsTheRawMetadataArtworkUrlOfAPaddedString()
	{
		MediaElement mediaElement = new() { MetadataArtworkUrl = "  https://example.com/art.png  " };

		var resolved = MetadataArtworkResolver.Resolve(mediaElement);

		resolved.Source.Should().BeOfType<UriMediaSource>();
		resolved.MetadataArtworkUrl.Should().Be("  https://example.com/art.png  ");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ResolveUsesMetadataArtworkSourceWhicheverWasSetFirst(bool sourceFirst)
	{
		MediaElement mediaElement = new();
		var artworkSource = MediaSource.FromFile(Path.Combine(Path.GetTempPath(), "art.png"));

		if (sourceFirst)
		{
			mediaElement.MetadataArtworkSource = artworkSource;
			mediaElement.MetadataArtworkUrl = "https://example.com/art.png";
		}
		else
		{
			mediaElement.MetadataArtworkUrl = "https://example.com/art.png";
			mediaElement.MetadataArtworkSource = artworkSource;
		}

		var resolved = MetadataArtworkResolver.Resolve(mediaElement);

		resolved.Source.Should().BeSameAs(artworkSource);
		resolved.MetadataArtworkUrl.Should().BeNull();
	}

	[Fact]
	public void ClearingMetadataArtworkSourceFallsBackToMetadataArtworkUrl()
	{
		MediaElement mediaElement = new()
		{
			MetadataArtworkUrl = "https://example.com/art.png",
			MetadataArtworkSource = MediaSource.FromFile(Path.Combine(Path.GetTempPath(), "art.png"))
		};

		mediaElement.MetadataArtworkSource = null;

		var resolved = MetadataArtworkResolver.Resolve(mediaElement);

		resolved.Source.Should().BeOfType<UriMediaSource>();
		resolved.MetadataArtworkUrl.Should().Be("https://example.com/art.png");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ClearingMetadataArtworkUrlLeavesNoArtwork(string? clearedUrl)
	{
		MediaElement mediaElement = new() { MetadataArtworkUrl = "https://example.com/art.png" };

		mediaElement.MetadataArtworkUrl = clearedUrl!;

		var resolved = MetadataArtworkResolver.Resolve(mediaElement);

		resolved.Source.Should().BeNull();
		resolved.MetadataArtworkUrl.Should().Be(clearedUrl);
	}

	[Fact]
	public void SettingOneArtworkPropertyNeverChangesTheOther()
	{
		MediaElement mediaElement = new();
		List<string?> changedProperties = [];
		mediaElement.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
		var artworkSource = MediaSource.FromUri("https://example.com/source.png");

		mediaElement.MetadataArtworkUrl = "https://example.com/url.png";
		mediaElement.MetadataArtworkSource.Should().BeNull();
		changedProperties.Should().OnlyContain(name => name == nameof(MediaElement.MetadataArtworkUrl));

		changedProperties.Clear();
		mediaElement.MetadataArtworkSource = artworkSource;
		mediaElement.MetadataArtworkUrl.Should().Be("https://example.com/url.png");
		changedProperties.Should().OnlyContain(name => name == nameof(MediaElement.MetadataArtworkSource));

		changedProperties.Clear();
		mediaElement.MetadataArtworkSource = null;
		mediaElement.MetadataArtworkUrl.Should().Be("https://example.com/url.png");
		changedProperties.Should().OnlyContain(name => name == nameof(MediaElement.MetadataArtworkSource));
	}

	[Fact]
	public void MappedMetadataArtworkUrlIsANewUnparentedSourceOnEachRead()
	{
		MediaElement mediaElement = new() { MetadataArtworkUrl = "https://example.com/art.png", BindingContext = new object() };

		var first = MetadataArtworkResolver.Resolve(mediaElement).Source;
		var second = MetadataArtworkResolver.Resolve(mediaElement).Source;

		first.Should().NotBeNull();
		second.Should().NotBeNull();
		first.Should().NotBeSameAs(second);
		first?.Parent.Should().BeNull();
		first?.BindingContext.Should().BeNull();
	}

	[Fact]
	public void BindingsOnBothArtworkPropertiesStayIndependent()
	{
		var viewModel = new ArtworkViewModel { Url = "https://example.com/bound.png" };
		MediaElement mediaElement = new();
		mediaElement.SetBinding(MediaElement.MetadataArtworkUrlProperty, nameof(ArtworkViewModel.Url), BindingMode.OneWay);
		mediaElement.SetBinding(MediaElement.MetadataArtworkSourceProperty, nameof(ArtworkViewModel.Artwork), BindingMode.OneWay);
		mediaElement.BindingContext = viewModel;

		var urlOnly = MetadataArtworkResolver.Resolve(mediaElement);
		urlOnly.Source.Should().BeOfType<UriMediaSource>().Which.Uri.Should().Be(new Uri("https://example.com/bound.png"));

		var boundSource = MediaSource.FromFile(Path.Combine(Path.GetTempPath(), "art.png"));
		viewModel.Artwork = boundSource;
		MetadataArtworkResolver.Resolve(mediaElement).Source.Should().BeSameAs(boundSource);
		mediaElement.MetadataArtworkUrl.Should().Be("https://example.com/bound.png");

		viewModel.Artwork = null;
		MetadataArtworkResolver.Resolve(mediaElement).Source.Should().BeOfType<UriMediaSource>();

		viewModel.Artwork = boundSource;
		mediaElement.MetadataArtworkUrl = "https://example.com/code.png";
		var laterSource = MediaSource.FromFile(Path.Combine(Path.GetTempPath(), "later.png"));
		viewModel.Artwork = laterSource;
		MetadataArtworkResolver.Resolve(mediaElement).Source.Should().BeSameAs(laterSource);
	}

	[Fact]
	public void StyleArtworkUrlIsUsedUntilAnArtworkSourceIsSet()
	{
		Style style = new(typeof(MediaElement))
		{
			Setters = { new Setter { Property = MediaElement.MetadataArtworkUrlProperty, Value = "https://example.com/style.png" } }
		};
		MediaElement mediaElement = new() { Style = style };
		var artworkSource = MediaSource.FromFile(Path.Combine(Path.GetTempPath(), "art.png"));

		MetadataArtworkResolver.Resolve(mediaElement).Source.Should().BeOfType<UriMediaSource>().Which.Uri.Should().Be(new Uri("https://example.com/style.png"));

		mediaElement.MetadataArtworkSource = artworkSource;
		MetadataArtworkResolver.Resolve(mediaElement).Source.Should().BeSameAs(artworkSource);
		mediaElement.MetadataArtworkUrl.Should().Be("https://example.com/style.png");

		mediaElement.MetadataArtworkSource = null;
		MetadataArtworkResolver.Resolve(mediaElement).Source.Should().BeOfType<UriMediaSource>().Which.Uri.Should().Be(new Uri("https://example.com/style.png"));
		mediaElement.MetadataArtworkUrl.Should().Be("https://example.com/style.png");
	}

	[Fact]
	public void MetadataArtworkUrlIsObsoleteAsAWarningWithOneMessage()
	{
		MemberInfo?[] obsoleteMembers =
		[
			typeof(MediaElement).GetProperty(nameof(MediaElement.MetadataArtworkUrl)),
			typeof(MediaElement).GetField(nameof(MediaElement.MetadataArtworkUrlProperty)),
			typeof(IMediaElement).GetProperty(nameof(IMediaElement.MetadataArtworkUrl))
		];

		foreach (var member in obsoleteMembers)
		{
			var obsolete = member?.GetCustomAttribute<ObsoleteAttribute>();

			obsolete.Should().NotBeNull();
			obsolete?.IsError.Should().BeFalse();
			obsolete?.Message.Should().Be(MetadataArtworkResolver.MetadataArtworkUrlObsoleteMessage);
		}
	}

	static void SetCurrentCulture(string cultureName)
	{
		var culture = CultureInfo.GetCultureInfo(cultureName);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
	}

	sealed class ArtworkViewModel : INotifyPropertyChanged
	{
		string url = string.Empty;
		MediaSource? artwork;

		public event PropertyChangedEventHandler? PropertyChanged;

		public string Url
		{
			get => url;
			set => Set(ref url, value);
		}

		public MediaSource? Artwork
		{
			get => artwork;
			set => Set(ref artwork, value);
		}

		void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
