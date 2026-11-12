using System.Globalization;
using CommunityToolkit.Maui.ImageSources;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.ImageSources;

public class GravatarImageSourceTests : BaseViewTest
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
			Source = new GravatarImageSource
			{
				Email = email,
			}
		};

		((GravatarImageSource)testControl.Source).Image = DefaultImage.FileNotFound;
		testControl.Arrange(new Rect(0, 0, 37, 73));
		Assert.Equal(DefaultImage.FileNotFound, ((GravatarImageSource)testControl.Source).Image);
	}

	[Fact]
	public void DefaultCacheValidity()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.CacheValidity, gravatarImageSource.CacheValidity);
	}

	[Fact]
	public void DefaultCachingEnabled()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.CachingEnabled, gravatarImageSource.CachingEnabled);
	}

	[Fact]
	public void DefaultDefaultImage()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.Image, gravatarImageSource.Image);
	}

	[Fact]
	public void DefaultEmail()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.Email, gravatarImageSource.Email);
	}

	[Fact]
	public void DefaultUri()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.Uri, gravatarImageSource.Uri);
	}

	[Fact]
	public void DefaultParentHeight()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.ParentHeight, gravatarImageSource.ParentHeight);
	}

	[Fact]
	public void DefaultParentWidth()
	{
		var gravatarImageSource = new GravatarImageSource();
		Assert.Equal(GravatarImageSourceDefaults.ParentWidth, gravatarImageSource.ParentWidth);
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
			WidthRequest = 37,
			HeightRequest = 73,
			ImageSource = new GravatarImageSource(),
		};
		testControl.Arrange(new Rect(0, 0, 37, 73));
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/?s=37"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void TestControlButtonWithEmail()
	{
		Button testControl = new()
		{
			WidthRequest = 37,
			HeightRequest = 73,
			ImageSource = new GravatarImageSource() { Email = email },
		};
		testControl.Arrange(new Rect(0, 0, 37, 73));
		Assert.True(testControl.ImageSource is GravatarImageSource);
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void ButtonWithoutSizeRequestsRequestsTheDefaultSize()
	{
		Button testControl = new()
		{
			ImageSource = new GravatarImageSource { Email = email },
		};
		testControl.Arrange(new Rect(0, 0, 37, 73));

		Assert.Equal(new Uri($"https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s={GravatarImageSourceDefaults.GravatarSize}&d=mp"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	// A Button sizes itself to its picture, so a request that followed the Button's laid-out size would grow with each picture.
	[Fact]
	public void ButtonThatGrowsForItsPictureRequestsTheSameSize()
	{
		var gravatarImageSource = new GravatarImageSource { Email = email };
		Button testControl = new()
		{
			ImageSource = gravatarImageSource,
		};
		testControl.Arrange(new Rect(0, 0, 300, 44));
		var requestedUri = gravatarImageSource.Uri;

		testControl.Arrange(new Rect(0, 0, 300, 124));

		Assert.Equal(new Uri($"https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s={GravatarImageSourceDefaults.GravatarSize}&d=mp"), requestedUri);
		Assert.Equal(requestedUri, gravatarImageSource.Uri);
	}

	[Theory]
	[InlineData(37d, 73d, 37)]
	[InlineData(-1d, 44d, 44)]
	[InlineData(120d, -1d, 120)]
	public void ButtonRequestsTheSmallerOfItsSizeRequests(double widthRequest, double heightRequest, int expectedSize)
	{
		Button testControl = new()
		{
			WidthRequest = widthRequest,
			HeightRequest = heightRequest,
			ImageSource = new GravatarImageSource { Email = email },
		};

		Assert.Equal(new Uri($"https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s={expectedSize}&d=mp"), ((GravatarImageSource)testControl.ImageSource).Uri);
	}

	[Fact]
	public void ButtonRequestsANewSizeWhenItsSizeRequestChanges()
	{
		Button testControl = new()
		{
			HeightRequest = 44,
			ImageSource = new GravatarImageSource { Email = email },
		};
		var gravatarImageSource = (GravatarImageSource)testControl.ImageSource;

		testControl.HeightRequest = 60;
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=60&d=mp"), gravatarImageSource.Uri);

		testControl.HeightRequest = -1;
		Assert.Equal(new Uri($"https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s={GravatarImageSourceDefaults.GravatarSize}&d=mp"), gravatarImageSource.Uri);
	}

	[Fact]
	public void SourceMovedFromAButtonToAnImageRequestsTheSizeOfTheImage()
	{
		var gravatarImageSource = new GravatarImageSource { Email = email };
		Button button = new()
		{
			ImageSource = gravatarImageSource,
		};
		button.ImageSource = null;

		Image testControl = new()
		{
			Source = gravatarImageSource,
		};
		testControl.Arrange(new Rect(0, 0, 37, 73));

		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), gravatarImageSource.Uri);
	}

	[Fact]
	public void TestControlImage()
	{
		Image testControl = new()
		{
			Source = new GravatarImageSource()
		};
		Assert.True(testControl.Source is GravatarImageSource);
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		testControl.Arrange(new Rect(0, 0, 37, 73));
		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=monsterid"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Theory]
	[InlineData("dsiegel@avantipoint.com")]
	[InlineData("DSIEGEL@AVANTIPOINT.COM")]
	[InlineData("DSiegel@AvantiPoint.com")]
	[InlineData(" dsiegel@avantipoint.com ")]
	[InlineData("\tDSiegel@AvantiPoint.com\r\n")]
	public void UriUsesHashOfTrimmedLowerCaseEmail(string emailAddress)
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Email = emailAddress
		};

		Image testControl = new()
		{
			Source = gravatarImageSource
		};
		testControl.Arrange(new Rect(0, 0, 37, 73));

		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), gravatarImageSource.Uri);
		Assert.Equal(emailAddress, gravatarImageSource.Email);
	}

	[Theory]
	[InlineData("dsiegel@avantipoint.com")]
	[InlineData("DSIEGEL@AVANTIPOINT.COM")]
	[InlineData("DSiegel@AvantiPoint.com")]
	[InlineData(" dsiegel@avantipoint.com ")]
	[InlineData("\tDSiegel@AvantiPoint.com\r\n")]
	public void ChangingEmailUpdatesUriWithHashOfTrimmedLowerCaseEmail(string emailAddress)
	{
		var gravatarImageSource = new GravatarImageSource();

		Image testControl = new()
		{
			Source = gravatarImageSource
		};
		testControl.Arrange(new Rect(0, 0, 37, 73));

		gravatarImageSource.Email = emailAddress;

		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), gravatarImageSource.Uri);
		Assert.Equal(emailAddress, gravatarImageSource.Email);
	}

	[Fact]
	public void TestControlPage()
	{
		Page testControl = new()
		{
			IconImageSource = new GravatarImageSource(),
		};
		Assert.True(testControl.IconImageSource is GravatarImageSource);
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		testControl.Arrange(new Rect(0, 0, 37, 73));
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
		var gravatarImageSource = new GravatarImageSource();
		await using var stream = await gravatarImageSource.Stream(TestContext.Current.CancellationToken);
		stream.Should().NotBeNull();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestDefaultStreamCanceled()
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var gravatarImageSource = new GravatarImageSource();
		cts.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gravatarImageSource.Stream(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OverlappingLoadsWithoutATokenBothReturnThePicture()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email,
			Stream = async cancellationToken =>
			{
				await release.Task.WaitAsync(cancellationToken);
				return new MemoryStream([1, 2, 3]);
			}
		};
		var streamImageSource = (IStreamImageSource)gravatarImageSource;

		var firstLoad = streamImageSource.GetStreamAsync(CancellationToken.None);
		var secondLoad = streamImageSource.GetStreamAsync(CancellationToken.None);
		release.SetResult();

		await using var firstStream = await firstLoad.WaitAsync(TestContext.Current.CancellationToken);
		await using var secondStream = await secondLoad.WaitAsync(TestContext.Current.CancellationToken);

		firstStream.Length.Should().Be(3);
		secondStream.Length.Should().Be(3);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task LoadCanceledByItsCallerThrowsOperationCanceledException()
	{
		var neverReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var callerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email,
			Stream = async cancellationToken =>
			{
				await neverReleased.Task.WaitAsync(cancellationToken);
				return new MemoryStream([1, 2, 3]);
			}
		};

		var load = ((IStreamImageSource)gravatarImageSource).GetStreamAsync(callerCancellationTokenSource.Token);
		callerCancellationTokenSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => load);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancelEndsALoadStartedWithoutATokenWithoutThrowing()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email,
			Stream = async cancellationToken =>
			{
				await release.Task.WaitAsync(cancellationToken);
				return new MemoryStream([1, 2, 3]);
			}
		};

		var load = ((IStreamImageSource)gravatarImageSource).GetStreamAsync(CancellationToken.None);
		var wasCanceled = await gravatarImageSource.Cancel();
		release.SetResult();

		var stream = await load.WaitAsync(TestContext.Current.CancellationToken);

		wasCanceled.Should().BeTrue();
		stream.Should().BeSameAs(System.IO.Stream.Null);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancelDoesNotEndALoadStartedWithACancelableToken()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var callerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email,
			Stream = async cancellationToken =>
			{
				await release.Task.WaitAsync(cancellationToken);
				return new MemoryStream([1, 2, 3]);
			}
		};

		var load = ((IStreamImageSource)gravatarImageSource).GetStreamAsync(callerCancellationTokenSource.Token);
		await gravatarImageSource.Cancel();
		release.SetResult();

		await using var stream = await load.WaitAsync(TestContext.Current.CancellationToken);

		stream.Length.Should().Be(3);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task LoadStartedWithoutATokenAfterCancelReturnsThePicture()
	{
		var gravatarImageSource = new GravatarImageSource
		{
			Email = email,
			Stream = cancellationToken => cancellationToken.IsCancellationRequested
				? Task.FromCanceled<System.IO.Stream>(cancellationToken)
				: Task.FromResult<System.IO.Stream>(new MemoryStream([1, 2, 3]))
		};

		await gravatarImageSource.Cancel();

		await using var stream = await ((IStreamImageSource)gravatarImageSource).GetStreamAsync(CancellationToken.None).WaitAsync(TestContext.Current.CancellationToken);

		stream.Length.Should().Be(3);
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
		Toolbar testControl = new(new MockView())
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

	[Theory]
	[InlineData("tr-TR", DefaultImage.Identicon, "identicon")]
	[InlineData("tr-TR", DefaultImage.MonsterId, "monsterid")]
	[InlineData("tr-TR", DefaultImage.Wavatar, "wavatar")]
	[InlineData("tr-TR", DefaultImage.Retro, "retro")]
	[InlineData("tr-TR", DefaultImage.Robohash, "robohash")]
	[InlineData("tr-TR", DefaultImage.Blank, "blank")]
	[InlineData("tr-TR", DefaultImage.FileNotFound, "404")]
	[InlineData("tr-TR", DefaultImage.MysteryPerson, "mp")]
	[InlineData("az-Latn-AZ", DefaultImage.Identicon, "identicon")]
	[InlineData("az-Latn-AZ", DefaultImage.MonsterId, "monsterid")]
	public void DefaultImageUsesGravatarKeywordInEveryCulture(string cultureName, DefaultImage defaultImage, string expectedKeyword)
	{
		// Gravatar only knows the ASCII keywords, so a culture-sensitive lower-casing ("monsterıd") makes it serve its own logo instead.
		Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);
		Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureName);
		Image testControl = new()
		{
			Source = new GravatarImageSource
			{
				Email = email,
				Image = defaultImage
			}
		};

		testControl.Arrange(new Rect(0, 0, 37, 73));

		Assert.Equal(new Uri($"https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d={expectedKeyword}"), ((GravatarImageSource)testControl.Source).Uri);
	}

	[Theory]
	[InlineData("dsiegel@avantipoint.com")]
	[InlineData("DSIEGEL@AVANTIPOINT.COM")]
	[InlineData("DSiegel@AvantiPoint.com")]
	[InlineData(" dsiegel@avantipoint.com ")]
	[InlineData("\tDSiegel@AvantiPoint.com\r\n")]
	public void UriUsesHashOfTrimmedLowerCaseEmailInTurkish(string emailAddress)
	{
		// The I in the address maps to a dotless i with a culture-sensitive lower-casing, which would change the hash.
		Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
		var gravatarImageSource = new GravatarImageSource
		{
			Email = emailAddress
		};
		Image testControl = new()
		{
			Source = gravatarImageSource
		};

		testControl.Arrange(new Rect(0, 0, 37, 73));

		Assert.Equal(new Uri("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=37&d=mp"), gravatarImageSource.Uri);
	}
}