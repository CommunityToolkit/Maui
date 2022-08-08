namespace CommunityToolkit.Maui.UnitTests.Views.GravatarImageSource;

using CommunityToolkit.Maui.Views;
using Xunit;

public class GravatarImageSourceTests : BaseHandlerTest
{
	readonly string email = "dsiegel@avantipoint.com";
	readonly TimeSpan cacheValidity = new(1, 0, 0);
	readonly bool cachingEnabled = false;
	readonly DefaultImage image = DefaultImage.MonsterId;

	[Fact]
	public void ConstructorTest()
	{
		var gravatarImageSource = new GravatarImageSource()
		{
			CacheValidity = cacheValidity,
			CachingEnabled = cachingEnabled,
			Email = email,
			Image = image
		};

		Assert.Equal(cacheValidity, gravatarImageSource.CacheValidity);
		Assert.True(cachingEnabled == gravatarImageSource.CachingEnabled);
		Assert.Equal(email, gravatarImageSource.Email);
		Assert.Equal(image, gravatarImageSource.Image);
	}

	[Fact]
	public void DefaultDefaultImage()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(DefaultImage.MysteryPerson, gravatarImageSource.Image);
	}

	[Fact]
	public void DefaultEmail()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Null(gravatarImageSource.Email);
	}

	[Fact]
	public void DefaultCacheValidity()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(new TimeSpan(1, 0, 0, 0), gravatarImageSource.CacheValidity);
	}

	[Fact]
	public void DefaultCachingEnabled()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.True(gravatarImageSource.CachingEnabled);
	}

	[Fact]
	public void DefaultUri()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), gravatarImageSource.Uri);
	}

	[Fact]
	public void TestChangeImage()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Image = image
		};
		Assert.Equal(image, gravatarImageSource.Image);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=0"), gravatarImageSource.Uri);
	}

	[Fact]
	public void TestChangeImageWithEmail()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email,
			Image = image
		};
		Assert.Equal(image, gravatarImageSource.Image);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=monsterid"), gravatarImageSource.Uri);
	}

	[Fact]
	public void TestChangeEmail()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email
		};
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), gravatarImageSource.Uri);
	}

	[Fact]
	public void TestImageControl()
	{
		Image testImage = new()
		{
			Source = new GravatarImageSource()
		};
		Assert.True(testImage.Source is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=80"), ((GravatarImageSource)testImage.Source).Uri);
	}

	[Fact]
	public void TestImageControlWithEmail()
	{
		Image testImage = new()
		{
			Source = new GravatarImageSource() { Email = email }
		};
		Assert.True(testImage.Source is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp"), ((GravatarImageSource)testImage.Source).Uri);
	}

	[Fact]
	public void NullEmailDoesNotCrash()
	{
		var loader = new GravatarImageSource();
		var exception = Record.Exception(() => loader.Email = null);
		Assert.Null(exception);
	}
}