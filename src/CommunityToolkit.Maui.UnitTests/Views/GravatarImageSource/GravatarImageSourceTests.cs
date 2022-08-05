namespace CommunityToolkit.Maui.UnitTests.Views.GravatarImageSource;
using CommunityToolkit.Maui.Core;
using Xunit;

public class GravatarImageSourceTests : BaseHandlerTest
{
	readonly string email = "dsiegel@avantipoint.com";
	readonly TimeSpan cacheValidity = new(1, 0, 0);
	readonly bool cachingEnabled = false;
	readonly DefaultImage image = DefaultImage.MonsterId;

	public GravatarImageSourceTests()
	{
		Assert.IsAssignableFrom<IGravatarImageSource>(new Maui.Views.GravatarImageSource());
	}

	[Fact]
	public void ConstructorTest()
	{
		var gravatarImageSource = new Maui.Views.GravatarImageSource()
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
		var gravatarImageSource = new Maui.Views.GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.Defaultimage, gravatarImageSource.Image);
	}

	[Fact]
	public void DefaultEmail()
	{
		var gravatarImageSource = new Maui.Views.GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.DefaultEmail, gravatarImageSource.Email);
	}

	[Fact]
	public void DefaultCacheValidity()
	{
		var gravatarImageSource = new Maui.Views.GravatarImageSource();
		Assert.Equal(new TimeSpan(1, 0, 0, 0), gravatarImageSource.CacheValidity);
	}

	[Fact]
	public void DefaultCachingEnabled()
	{
		var gravatarImageSource = new Maui.Views.GravatarImageSource();
		Assert.True(gravatarImageSource.CachingEnabled);
	}

	[Fact]
	public void DefaultUri()
	{
		var gravatarImageSource = new Maui.Views.GravatarImageSource();
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), gravatarImageSource.Uri);
	}

	[Fact]
	public void TestChangeImage()
	{
		var gravatarImageSource = new Maui.Views.GravatarImageSource
		{
			Image = image
		};
		Assert.Equal(image, gravatarImageSource.Image);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=0"), gravatarImageSource.Uri);
	}

	[Fact]
	public void TestChangeImageWithEmail()
	{
		var gravatarImageSource = new Maui.Views.GravatarImageSource
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
		var gravatarImageSource = new Maui.Views.GravatarImageSource
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
			Source = new Maui.Views.GravatarImageSource()
		};
		Assert.True(testImage.Source is Maui.Views.GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=80"), ((Maui.Views.GravatarImageSource)testImage.Source).Uri);
	}

	[Fact]
	public void TestImageControlWithEmail()
	{
		Image testImage = new()
		{
			Source = new Maui.Views.GravatarImageSource() { Email = email }
		};
		Assert.True(testImage.Source is Maui.Views.GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp"), ((Maui.Views.GravatarImageSource)testImage.Source).Uri);
	}

	[Fact]
	public void NullEmailDoesNotCrash()
	{
		var loader = new Maui.Views.GravatarImageSource();
		var exception = Record.Exception(() => loader.Email = null);
		Assert.Null(exception);
	}
}