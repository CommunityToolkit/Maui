using System.Globalization;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaElementMetadataArtworkPathsTests : BaseTest
{
	public static TheoryData<string> CultureNames { get; } = ["en-US", "tr-TR", "az-Latn-AZ"];

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetLocalFilePathKeepsAPlainAbsolutePath(string cultureName)
	{
		SetCurrentCulture(cultureName);
		var path = Path.Combine(Path.GetTempPath(), "art.png");

		MetadataArtworkPaths.GetLocalFilePath(path).Should().Be(path);
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetLocalFilePathKeepsARelativePath(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.GetLocalFilePath("art.png").Should().Be("art.png");
		MetadataArtworkPaths.GetLocalFilePath("folder/art.png").Should().Be("folder/art.png");
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetLocalFilePathConvertsAFileUriToTheLocalPath(string cultureName)
	{
		SetCurrentCulture(cultureName);
		var path = Path.Combine(Path.GetTempPath(), "art.png");

		MetadataArtworkPaths.GetLocalFilePath(new Uri(path).AbsoluteUri).Should().Be(path);
	}

	// tr-TR upper-cases "file" to "FİLE", so a culture-sensitive comparison of the scheme would not match
	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetLocalFilePathIgnoresTheCaseOfTheFileScheme(string cultureName)
	{
		SetCurrentCulture(cultureName);
		var path = Path.Combine(Path.GetTempPath(), "art.png");
		var schemeSeparatorLength = "file:".Length;
		var uriWithoutScheme = new Uri(path).AbsoluteUri[schemeSeparatorLength..];

		MetadataArtworkPaths.GetLocalFilePath("FILE:" + uriWithoutScheme).Should().Be(path);
		MetadataArtworkPaths.GetLocalFilePath("File:" + uriWithoutScheme).Should().Be(path);
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetLocalFilePathUnescapesAFileUri(string cultureName)
	{
		SetCurrentCulture(cultureName);
		var path = Path.Combine(Path.GetTempPath(), "art work.png");
		var fileUri = new Uri(path).AbsoluteUri;

		fileUri.Should().Contain("%20");
		MetadataArtworkPaths.GetLocalFilePath(fileUri).Should().Be(path);
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetLocalFilePathReturnsNullWithoutAPath(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.GetLocalFilePath(null).Should().BeNull();
		MetadataArtworkPaths.GetLocalFilePath(string.Empty).Should().BeNull();
		MetadataArtworkPaths.GetLocalFilePath("   ").Should().BeNull();
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetAndroidAssetPathRemovesTheAssetsPrefixOfFromResource(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.GetAndroidAssetPath("Assets/x.png").Should().Be("x.png");
		MetadataArtworkPaths.GetAndroidAssetPath("Assets/dir/x.png").Should().Be("dir/x.png");
		MetadataArtworkPaths.GetAndroidAssetPath("Assets\\dir\\x.png").Should().Be("dir/x.png");
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetAndroidAssetPathNormalizesPathsWithoutThePrefix(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.GetAndroidAssetPath("x.png").Should().Be("x.png");
		MetadataArtworkPaths.GetAndroidAssetPath("dir\\x.png").Should().Be("dir/x.png");
		MetadataArtworkPaths.GetAndroidAssetPath("/x.png").Should().Be("x.png");
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetAndroidAssetPathOnlyRemovesTheExactPrefix(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.GetAndroidAssetPath("assets/x.png").Should().Be("assets/x.png");
		MetadataArtworkPaths.GetAndroidAssetPath("Assets/İcon.png").Should().Be("İcon.png");
		MetadataArtworkPaths.GetAndroidAssetPath("ASSETS/x.png").Should().Be("ASSETS/x.png");
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void GetAndroidAssetPathReturnsNullWithoutAFile(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.GetAndroidAssetPath("Assets/").Should().BeNull();
		MetadataArtworkPaths.GetAndroidAssetPath("/").Should().BeNull();
		MetadataArtworkPaths.GetAndroidAssetPath(null).Should().BeNull();
		MetadataArtworkPaths.GetAndroidAssetPath(string.Empty).Should().BeNull();
		MetadataArtworkPaths.GetAndroidAssetPath("  ").Should().BeNull();
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void IsHttpUriAcceptsHttpAndHttpsInAnyCase(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.IsHttpUri(new Uri("http://example.com/a.png")).Should().BeTrue();
		MetadataArtworkPaths.IsHttpUri(new Uri("https://example.com/a.png")).Should().BeTrue();
		MetadataArtworkPaths.IsHttpUri(new Uri("HTTP://example.com/a.png")).Should().BeTrue();
		MetadataArtworkPaths.IsHttpUri(new Uri("HtTpS://example.com/a.png")).Should().BeTrue();
	}

	[Theory]
	[MemberData(nameof(CultureNames))]
	public void IsHttpUriRejectsOtherSchemesAndRelativeUris(string cultureName)
	{
		SetCurrentCulture(cultureName);

		MetadataArtworkPaths.IsHttpUri(new Uri(Path.Combine(Path.GetTempPath(), "a.png"))).Should().BeFalse();
		MetadataArtworkPaths.IsHttpUri(new Uri("ftp://example.com/a.png")).Should().BeFalse();
		MetadataArtworkPaths.IsHttpUri(new Uri("ms-appx:///a.png")).Should().BeFalse();
		MetadataArtworkPaths.IsHttpUri(new Uri("a/b.png", UriKind.Relative)).Should().BeFalse();
	}

	static void SetCurrentCulture(string cultureName)
	{
		var culture = CultureInfo.GetCultureInfo(cultureName);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
	}
}