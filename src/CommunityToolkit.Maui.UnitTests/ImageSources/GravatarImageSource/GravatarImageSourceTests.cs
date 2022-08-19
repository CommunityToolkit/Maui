namespace CommunityToolkit.Maui.UnitTests.ImageSources.GravatarImageSource;

using CommunityToolkit.Maui.ImageSources;
using Xunit;

public class GravatarImageSourceTests : BaseHandlerTest
{
	readonly TimeSpan cacheValidity = new(1, 0, 0);
	readonly bool cachingEnabled = false;
	readonly string email = "dsiegel@avantipoint.com";
	readonly DefaultImage image = DefaultImage.MonsterId;

	[Fact]
	public void ChangeEmail()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email
		};
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), gravatarImageSource.Uri);
	}

	[Fact]
	public void ChangeImage()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Image = image
		};
		Assert.Equal(image, gravatarImageSource.Image);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=0"), gravatarImageSource.Uri);
	}

	[Fact]
	public void ChangeImageWithEmail()
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
	public void Default404Image()
	{
		var gravatarImageSource = new GravatarImageSource()
		{
			Email = email,
			Image = DefaultImage.FileNotFound,
		};
		Assert.Equal(DefaultImage.FileNotFound, gravatarImageSource.Image);
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
	public void DefaultUri()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), gravatarImageSource.Uri);
	}

	[Fact]
	public void IsEmpty()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.False(gravatarImageSource.IsEmpty);
	}

	[Fact]
	public void NullEmailDoesNotCrash()
	{
		var loader = new GravatarImageSource();
		var exception = Record.Exception(() => loader.Email = null);
		Assert.Null(exception);
	}

	[Fact]
	public void TestControlAppLinkEntry()
	{
		AppLinkEntry testControl = new()
		{
			Thumbnail = new GravatarImageSource(),
		};
		Assert.True(testControl.Thumbnail is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.Thumbnail).Uri);
	}

	[Fact]
	public void TestControlAppLinkEntryWithEmail()
	{
		AppLinkEntry testControl = new()
		{
			Thumbnail = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.Thumbnail is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.Thumbnail).Uri);
	}

	[Fact]
	public void TestControlBackButtonBehavior()
	{
		BackButtonBehavior testControl = new()
		{
			IconOverride = new GravatarImageSource(),
		};
		Assert.True(testControl.IconOverride is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.IconOverride).Uri);
	}

	[Fact]
	public void TestControlBackButtonBehaviorWithEmail()
	{
		BackButtonBehavior testControl = new()
		{
			IconOverride = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.IconOverride is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.IconOverride).Uri);
	}

	[Fact]
	public void TestControlBaseShellItem()
	{
		BaseShellItem testControl = new()
		{
			Icon = new GravatarImageSource(),
			FlyoutIcon = new GravatarImageSource(),
		};
		Assert.True(testControl.Icon is GravatarImageSource);
		Assert.True(testControl.FlyoutIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.Icon).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.FlyoutIcon).Uri);
	}

	[Fact]
	public void TestControlBaseShellItemWithEmail()
	{
		BaseShellItem testControl = new()
		{
			Icon = new GravatarImageSource() { Email = email },
			FlyoutIcon = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.Icon is GravatarImageSource);
		Assert.True(testControl.FlyoutIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.Icon).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.FlyoutIcon).Uri);
	}

	[Fact]
	public void TestControlButton()
	{
		Button testControl = new()
		{
			ImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=80"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlButtonWithEmail()
	{
		Button testControl = new()
		{
			ImageSource = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlButtonWithEmailandSize()
	{
		Button testControl = new()
		{
			ImageSource = new GravatarImageSource() { Email = email },
			WidthRequest = 72,
			HeightRequest = 73
		};
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlDataPackage()
	{
		DataPackage testControl = new()
		{
			Image = new GravatarImageSource(),
		};
		Assert.True(testControl.Image is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.Image).Uri);
	}

	[Fact]
	public void TestControlDataPackageWithEmail()
	{
		DataPackage testControl = new()
		{
			Image = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.Image is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.Image).Uri);
	}

	[Fact]
	public void TestControlImage()
	{
		Image testImage = new()
		{
			Source = new GravatarImageSource()
		};
		Assert.True(testImage.Source is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=80"), ((GravatarImageSource)testImage.Source).Uri);
	}

	[Fact]
	public void TestControlImageButton()
	{
		ImageButton testControl = new()
		{
			Source = new GravatarImageSource(),
		};
		Assert.True(testControl.Source is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=80"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageButtonWithEmail()
	{
		ImageButton testControl = new()
		{
			Source = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.Source is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageButtonWithEmailandSize()
	{
		ImageButton testControl = new()
		{
			Source = new GravatarImageSource() { Email = email },
			WidthRequest = 72,
			HeightRequest = 73
		};
		Assert.True(testControl.Source is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageCell()
	{
		ImageCell testControl = new()
		{
			ImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlImageCellWithEmail()
	{
		ImageCell testControl = new()
		{
			ImageSource = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlImageWithEmail()
	{
		Image testImage = new()
		{
			Source = new GravatarImageSource() { Email = email }
		};
		Assert.True(testImage.Source is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=80&d=mp"), ((GravatarImageSource)testImage.Source).Uri);
	}

	[Fact]
	public void TestControlMenuItem()
	{
		MenuItem testControl = new()
		{
			IconImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.IconImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.IconImageSource).Uri);
	}

	[Fact]
	public void TestControlMenuItemControlWithEmail()
	{
		MenuItem testControl = new()
		{
			IconImageSource = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.IconImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.IconImageSource).Uri);
	}

	[Fact]
	public void TestControlPage()
	{
		Page testControl = new()
		{
			IconImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.IconImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.IconImageSource).Uri);
	}

	[Fact]
	public void TestControlPageWithEmail()
	{
		Page testControl = new()
		{
			IconImageSource = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.IconImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.IconImageSource).Uri);
	}

	[Fact]
	public void TestControlSearchHandler()
	{
		SearchHandler testControl = new()
		{
			QueryIcon = new GravatarImageSource(),
			ClearPlaceholderIcon = new GravatarImageSource(),
			ClearIcon = new GravatarImageSource(),
		};
		Assert.True(testControl.QueryIcon is GravatarImageSource);
		Assert.True(testControl.ClearPlaceholderIcon is GravatarImageSource);
		Assert.True(testControl.ClearIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.QueryIcon).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.ClearPlaceholderIcon).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.ClearIcon).Uri);
	}

	[Fact]
	public void TestControlSearchHandlerWithEmail()
	{
		SearchHandler testControl = new()
		{
			QueryIcon = new GravatarImageSource() { Email = email },
			ClearPlaceholderIcon = new GravatarImageSource() { Email = email },
			ClearIcon = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.QueryIcon is GravatarImageSource);
		Assert.True(testControl.ClearPlaceholderIcon is GravatarImageSource);
		Assert.True(testControl.ClearIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.QueryIcon).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.ClearPlaceholderIcon).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.ClearIcon).Uri);
	}

	[Fact]
	public void TestControlShell()
	{
		Shell testControl = new()
		{
			FlyoutBackgroundImage = new GravatarImageSource(),
			FlyoutIcon = new GravatarImageSource(),
		};
		Assert.True(testControl.FlyoutBackgroundImage is GravatarImageSource);
		Assert.True(testControl.FlyoutIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.FlyoutBackgroundImage).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.FlyoutIcon).Uri);
	}

	[Fact]
	public void TestControlShellWithEmail()
	{
		Shell testControl = new()
		{
			FlyoutBackgroundImage = new GravatarImageSource() { Email = email },
			FlyoutIcon = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.FlyoutBackgroundImage is GravatarImageSource);
		Assert.True(testControl.FlyoutIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.FlyoutBackgroundImage).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.FlyoutIcon).Uri);
	}

	[Fact]
	public void TestControlSlider()
	{
		Slider testControl = new()
		{
			ThumbImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.ThumbImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.ThumbImageSource).Uri);
	}

	[Fact]
	public void TestControlSliderWithEmail()
	{
		Slider testControl = new()
		{
			ThumbImageSource = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.ThumbImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.ThumbImageSource).Uri);
	}

	[Fact]
	public void TestControlToolbar()
	{
		Toolbar testControl = new(new View())
		{
			TitleIcon = new GravatarImageSource(),
		};
		Assert.True(testControl.TitleIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.TitleIcon).Uri);
	}

	[Fact]
	public void TestControlToolbarWithEmail()
	{
		Toolbar testControl = new(new View())
		{
			TitleIcon = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.TitleIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=0&d=mp"), ((GravatarImageSource)testControl.TitleIcon).Uri);
	}

	[Fact]
	public async Task TestDefaultStream()
	{
		CancellationTokenSource cts = new();
		var gravatarImageSource = new GravatarImageSource();
		Stream stream = await gravatarImageSource.Stream(cts.Token);
		Assert.Equal(2637, stream.Length);
	}

	[Fact]
	public async Task TestDefaultStreamCanceled()
	{
		CancellationTokenSource cts = new();
		var gravatarImageSource = new GravatarImageSource();
		cts.Cancel();
		Stream stream = await gravatarImageSource.Stream(cts.Token);
		Assert.Equal(Stream.Null, stream);
	}

	[Fact]
	public void ToStringTest()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal("Uri: https://www.gravatar.com/avatar/, Email:, Size:0, Image: mp, CacheValidity:1.00:00:00, CachingEnabled:True", gravatarImageSource.ToString());
	}
}