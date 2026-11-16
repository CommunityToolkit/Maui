using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class UriMediaSourceTests : BaseViewTest
{
	[Fact]
	public void HttpHeaders_DefaultIsEmpty()
	{
		var source = new UriMediaSource();

		source.HttpHeaders.Should().NotBeNull();
		source.HttpHeaders.Should().BeEmpty();
	}

	[Fact]
	public void HttpHeaders_SetAndGetRoundtrip()
	{
		const string authHeaderName = "Authorization";
		const string authHeaderValue = "Bearer abc123";
		const string customHeaderName = "X-Custom-Header";
		const string customHeaderValue = "custom-value";

		var source = new UriMediaSource();
		source.HttpHeaders.Add(authHeaderName, authHeaderValue);
		source.HttpHeaders.Add(customHeaderName, customHeaderValue);

		source.HttpHeaders.Should().HaveCount(2);
		source.HttpHeaders[authHeaderName].Should().Be(authHeaderValue);
		source.HttpHeaders[customHeaderName].Should().Be(customHeaderValue);
	}

	[Fact]
	public void HttpHeaders_SetTriggersSourceChanged()
	{
		var source = new UriMediaSource();
		var sourceChangedFired = false;

		var mediaElement = new MediaElement { Source = source };
		mediaElement.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(MediaElement.Source))
			{
				sourceChangedFired = true;
			}
		};

		source.HttpHeaders.Add("Authorization", "Bearer abc123");

		sourceChangedFired.Should().BeTrue();
	}

	[Fact]
	public void HttpHeaders_MutatingExistingDictionaryTriggersSourceChanged()
	{
		var source = new UriMediaSource();
		var sourceChangedCount = 0;

		var mediaElement = new MediaElement { Source = source };
		mediaElement.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(MediaElement.Source))
			{
				sourceChangedCount++;
			}
		};

		source.HttpHeaders["Authorization"] = "Bearer test-token";
		source.HttpHeaders["X-Test-Header"] = "Test";

		sourceChangedCount.Should().Be(2);
	}

	[Fact]
	public void FromUri_WithHeaders_CreatesUriMediaSourceWithHeaders()
	{
		var uri = new Uri("https://example.com/video.mp4");
		var headers = new Dictionary<string, string>
		{
			["Authorization"] = "Bearer abc123"
		};

		var source = MediaSource.FromUri(uri, headers);

		var uriSource = source.Should().BeOfType<UriMediaSource>().Which;
		uriSource.Uri.Should().Be(uri);
		uriSource.HttpHeaders.Should().HaveCount(1);
		uriSource.HttpHeaders["Authorization"].Should().Be("Bearer abc123");
	}

	[Fact]
	public void FromUri_WithNoHeaders_CreatesUriMediaSourceWithEmptyHeaders()
	{
		var uri = new Uri("https://example.com/video.mp4");

		var source = MediaSource.FromUri(uri);

		var uriSource = source.Should().BeOfType<UriMediaSource>().Which;
		uriSource.HttpHeaders.Should().NotBeNull();
		uriSource.HttpHeaders.Should().BeEmpty();
	}

	[Fact]
	public void FromUri_WithHeaders_RelativeUri_Throws()
	{
		var uri = new Uri("relative/path", UriKind.Relative);
		var headers = new Dictionary<string, string>
		{
			["Authorization"] = "Bearer abc123"
		};

		var act = () => MediaSource.FromUri(uri, headers);

		act.Should().Throw<ArgumentException>().WithParameterName("uri");
	}
}