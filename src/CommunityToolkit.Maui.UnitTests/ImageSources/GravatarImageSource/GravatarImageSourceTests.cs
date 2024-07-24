using CommunityToolkit.Maui.ImageSources;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.ImageSources;

public class GravatarImageSourceTests : BaseHandlerTest
{
	readonly TimeSpan cacheValidity = new(1, 0, 0);
	readonly bool cachingEnabled = false;
	readonly string email = "dsiegel@avantipoint.com";
	readonly DefaultImage image = DefaultImage.MonsterId;

	[Fact]
	public void ChangingEmailAndImageWithNoSizeDoesNotUpdateUri()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email,
			Image = image
		};
		Assert.Equal(image, gravatarImageSource.Image);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), gravatarImageSource.Uri);
	}

	[Fact]
	public void ChangingEmailWithNoSizeDoesNotUpdateUri()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email
		};
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), gravatarImageSource.Uri);
	}

	[Fact]
	public void ChangingImageWithNoSizeDoesNotUpdateUri()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Image = image
		};
		Assert.Equal(image, gravatarImageSource.Image);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), gravatarImageSource.Uri);
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
		Image testControl = new()
		{
			Source = new GravatarImageSource()
			{
				Email = email,
			}
		};

		((GravatarImageSource)testControl.Source).Image = DefaultImage.FileNotFound;
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(DefaultImage.FileNotFound, ((GravatarImageSource)testControl.Source).Image);
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
	public void IsDisposed()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.False(gravatarImageSource.IsDisposed);
		gravatarImageSource.Dispose();
		Assert.True(gravatarImageSource.IsDisposed);
	}

	[Fact]
	public void IsDisposedDisposeTokenSource()
	{
		Image testControl = new()
		{
			Source = new GravatarImageSource()
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));

		Assert.False(((GravatarImageSource)testControl.Source).IsDisposed);
		((GravatarImageSource)testControl.Source).Dispose();
		Assert.True(((GravatarImageSource)testControl.Source).IsDisposed);
	}

	[Fact]
	public void IsEmpty()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.True(gravatarImageSource.IsEmpty);
	}

	[Fact]
	public void IsEmptyNot()
	{
		var gravatarImageSource = new GravatarImageSource()
		{
			Email = email,
		};
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
	public void TestBindableObjectBackButtonBehavior()
	{
		BackButtonBehavior testControl = new()
		{
			IconOverride = new GravatarImageSource(),
		};
		Assert.True(testControl.IconOverride is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.IconOverride).Uri);
	}

	[Fact]
	public void TestBindableObjectSearchHandler()
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
	public void TestControlButton()
	{
		Button testControl = new()
		{
			ImageSource = new GravatarImageSource(),
		};
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=37"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlButtonWithEmail()
	{
		Button testControl = new()
		{
			ImageSource = new GravatarImageSource() { Email = email },
		};
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlImage()
	{
		Image testControl = new()
		{
			Source = new GravatarImageSource()
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=37"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageButton()
	{
		ImageButton testControl = new()
		{
			Source = new GravatarImageSource(),
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=37"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageButtonWithEmail()
	{
		ImageButton testControl = new()
		{
			Source = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageButtonWithoutEmail()
	{
		ImageButton testControl = new()
		{
			Source = new GravatarImageSource() { Image = image },
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=37"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageWithEmail()
	{
		Image testControl = new()
		{
			Source = new GravatarImageSource()
			{
				Email = email,
			}
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlImageWithEmailAndImage()
	{
		Image testControl = new()
		{
			Source = new GravatarImageSource()
			{
				Email = email,
				Image = image
			}
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=monsterid"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Fact]
	public void TestControlPage()
	{
		Page testControl = new()
		{
			IconImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.IconImageSource is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.IconImageSource).Uri);
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
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.FlyoutBackgroundImage).Uri);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.FlyoutIcon).Uri);
	}

	[Fact]
	public void TestControlSlider()
	{
		Slider testControl = new()
		{
			ThumbImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.ThumbImageSource is GravatarImageSource);
		testControl.Layout(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.ThumbImageSource).Uri);
	}

	[Fact]
	public void TestDataPackage()
	{
		DataPackage testControl = new()
		{
			Image = new GravatarImageSource(),
		};
		Assert.True(testControl.Image is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.Image).Uri);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestDefaultStream()
	{
		CancellationTokenSource cts = new();
		var gravatarImageSource = new GravatarImageSource();
		await using var stream = await gravatarImageSource.Stream(cts.Token);
		stream.Should().NotBeNull();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestDefaultStreamCanceled()
	{
		CancellationTokenSource cts = new();
		var gravatarImageSource = new GravatarImageSource();
		cts.Cancel();
		Stream stream = await gravatarImageSource.Stream(cts.Token);
		Assert.Equal(Stream.Null, stream);
	}

	[Fact]
	public void TestElementAppLinkEntry()
	{
		AppLinkEntry testControl = new()
		{
			Thumbnail = new GravatarImageSource(),
		};
		Assert.True(testControl.Thumbnail is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.Thumbnail).Uri);
	}

	[Fact]
	public void TestElementAppLinkEntryWithEmail()
	{
		AppLinkEntry testControl = new()
		{
			Thumbnail = new GravatarImageSource() { Email = email },
		};
		Assert.True(testControl.Thumbnail is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.Thumbnail).Uri);
	}

	[Fact]
	public void TestElementImageCell()
	{
		ImageCell testControl = new()
		{
			ImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestElementMenuItem()
	{
		MenuItem testControl = new()
		{
			IconImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.IconImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.IconImageSource).Uri);
	}

	[Fact]
	public void TestNavigableElementBaseShellItem()
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
	public void TestToolbar()
	{
		Toolbar testControl = new(new View())
		{
			TitleIcon = new GravatarImageSource(),
		};
		Assert.True(testControl.TitleIcon is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/"), ((GravatarImageSource)testControl.TitleIcon).Uri);
	}

	[Fact]
	public void ToStringTest()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal("Uri: https://www.gravatar.com/avatar/\nEmail: \nSize: \nImage: mp\nCacheValidity: 1.00:00:00\nCachingEnabled: True", gravatarImageSource.ToString());
	}
}