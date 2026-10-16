using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.ImageSources;
using FluentAssertions;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class AvatarViewImageTests : BaseViewTest
{
	public AvatarViewImageTests()
	{
		Assert.IsType<IAvatarView>(new Maui.Views.AvatarView(), exactMatch: false);
	}

	public enum EmptyImageSourceKind
	{
		File,
		Uri,
		Stream,
		Gravatar,
	}

	[Fact]
	public void DefaultImageSource()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.ImageSource.Should().BeNull();
	}

	[Fact]
	public void DefaultImageSourceProperties()
	{
		var source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		var avatarView = new Maui.Views.AvatarView
		{
			ImageSource = source
		};
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeAssignableTo<Image>();
		if (avatarView.Content is Image avatarImage)
		{
			avatarImage.Should().NotBeNull();
			avatarImage.Aspect.Should().Be(Aspect.AspectFill);
			avatarImage.Source.Should().Be(source);
			avatarImage.Width.Should().Be(-1);
			avatarImage.Height.Should().Be(-1);
			avatarImage.Clip.Should().BeNull();
			avatarImage.BackgroundColor.Should().BeNull();
		}
	}

	[Fact]
	public void ImageSourceBackgroundColor()
	{
		var source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		var avatarView = new Maui.Views.AvatarView
		{
			WidthRequest = 73,
			HeightRequest = 37,
			ImageSource = source,
			BackgroundColor = Colors.Azure,
		};
		avatarView.Arrange(new Rect(0, 0, 73, 73));
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeAssignableTo<Image>();
		if (avatarView.Content is Image avatarImage)
		{
			avatarImage.BackgroundColor.Should().BeNull();
		}
	}

	[Fact]
	public void ImageSourceChanged()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.ImageSource.Should().BeNull();
		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "ImageSource")
			{
				signaled = true;
			}
		};

		ImageSource source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		avatarView.ImageSource = source;
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.ImageSource.Should().Be(source);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void ImageSourceChangedToNull()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.ImageSource.Should().BeNull();
		ImageSource source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		avatarView.ImageSource = source;
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.ImageSource.Should().Be(source);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		avatarView.ImageSource = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		avatarView.ImageSource.Should().BeNull();
	}

	[Fact]
	public void ImageSourceIsNotEmpty()
	{
		var avatarView = new Maui.Views.AvatarView();
		ImageSource source = ImageSource.FromFile("File.png");
		avatarView.ImageSource = source;
		avatarView.ImageSource.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void ImageSourceParentSize_WhenStrokeShapeNotSet()
	{
		const int borderWidth = 5;
		const int widthRequest = 73;
		const int heightRequest = 37;
		const int layoutDiameter = widthRequest;
		var padding = new Thickness(0, 5, 10, 15);

		var avatarView = new Maui.Views.AvatarView
		{
			Padding = padding,
			BorderWidth = borderWidth,
			WidthRequest = widthRequest,
			HeightRequest = heightRequest,
			ImageSource = new UriImageSource
			{
				Uri = new Uri("https://aka.ms/campus.jpg"),
			}
		};
		avatarView.Arrange(new Rect(0, 0, 73, 73));

		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeAssignableTo<Image>();
		if (avatarView.Content is not Image avatarImage)
		{
			throw new InvalidCastException($"{nameof(avatarView.Content)} must be of type {nameof(Image)}");
		}

		avatarImage.Clip.Should().BeNull();
	}

	[Fact]
	public void ImageSourceParentSize_WhenStrokeShapeSet()
	{
		const int borderWidth = 5;
		const int widthRequest = 73;
		const int heightRequest = 37;
		const int layoutDiameter = widthRequest;
		var padding = new Thickness(0, 5, 10, 15);

		var avatarView = new Maui.Views.AvatarView
		{
			Padding = padding,
			BorderWidth = borderWidth,
			WidthRequest = widthRequest,
			HeightRequest = heightRequest,
			ImageSource = new UriImageSource()
			{
				Uri = new Uri("https://aka.ms/campus.jpg"),
			},
			StrokeShape = new Rectangle
			{
				Clip = new RectangleGeometry()
			}
		};
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeAssignableTo<Image>();

		avatarView.Arrange(new Rect(0, 0, 73, 73));

		if (avatarView.Content is not Image avatarImage)
		{
			throw new InvalidCastException($"{nameof(avatarView.Content)} must be of type {nameof(Image)}");
		}
		avatarImage.Clip.Should().BeOfType<RectangleGeometry>();
	}

	[Fact]
	public void ImageClip_WhenStrokeShapeChanged()
	{
		var clip = new RectangleGeometry(new Rect(0, 0, 30, 30));
		var avatarView = new Maui.Views.AvatarView
		{
			WidthRequest = 73,
			HeightRequest = 73,
			ImageSource = ImageSource.FromFile("File.png"),
		};
		avatarView.Arrange(new Rect(0, 0, 73, 73));

		if (avatarView.Content is not Image avatarImage)
		{
			throw new InvalidCastException($"{nameof(avatarView.Content)} must be of type {nameof(Image)}");
		}

		// The default stroke shape (a RoundRectangle) has no clip of its own
		avatarImage.Clip.Should().BeNull();

		avatarView.StrokeShape = new Ellipse
		{
			Clip = clip
		};

		avatarImage.Clip.Should().BeSameAs(clip);
	}

	[Fact]
	public void TextIsShownWhenTheImageSourceFailsToLoad()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;

		ReportLoadFailure(image);

		avatarView.Content.Should().BeOfType<Label>().Which.Text.Should().Be("GL");
	}

	[Fact]
	public void ImageIsShownWhenTheImageSourceLoads()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;

		ReportLoadSuccess(image);

		avatarView.Content.Should().BeSameAs(image);
	}

	[Fact]
	public void ImageIsShownAgainWhenTheImageSourceLoadsAfterAFailure()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		ReportLoadFailure(image);

		ReportLoadSuccess(image);

		avatarView.Content.Should().BeSameAs(image);
	}

	[Fact]
	public void ImageIsShownWhenAnotherImageSourceIsSetAfterAFailure()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		ReportLoadFailure(image);
		var otherSource = new UriImageSource { Uri = new Uri("https://aka.ms/campus.jpg") };

		avatarView.ImageSource = otherSource;

		avatarView.Content.Should().BeSameAs(image);
		image.Source.Should().BeSameAs(otherSource);
	}

	// LoadingCompleted(false) means the load was canceled or another load replaced it, and that load reports for itself.
	[Fact]
	public void ImageStaysWhenTheLoadOfTheImageSourceIsCanceled()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;

		((IImageSourcePartEvents)image).LoadingStarted();
		((IImageSourcePartEvents)image).LoadingCompleted(false);

		avatarView.Content.Should().BeSameAs(image);
	}

	[Fact]
	public void TextStaysWhenALoadEndsAfterTheImageSourceWasRemoved()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;

		avatarView.ImageSource = null;
		ReportLoadSuccess(image);
		ReportLoadFailure(image);

		avatarView.Content.Should().BeOfType<Label>();
	}

	// .NET MAUI reports the load of an empty image source as a success on iOS, Mac Catalyst and Windows, and as a failure on Android.
	[Theory]
	[InlineData(EmptyImageSourceKind.File)]
	[InlineData(EmptyImageSourceKind.Uri)]
	[InlineData(EmptyImageSourceKind.Stream)]
	[InlineData(EmptyImageSourceKind.Gravatar)]
	public void TextIsShownWhenTheLoadOfAnEmptyImageSourceSucceeds(EmptyImageSourceKind kind)
	{
		var avatarView = new Maui.Views.AvatarView { Text = "GL", ImageSource = CreateEmptyImageSource(kind) };
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;

		ReportLoadSuccess(image);

		avatarView.Content.Should().BeOfType<Label>().Which.Text.Should().Be("GL");
	}

	[Fact]
	public void ImageIsShownWhenTheEmptyImageSourceGetsAnUri()
	{
		var source = new UriImageSource();
		var avatarView = new Maui.Views.AvatarView { Text = "GL", ImageSource = source };
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		ReportLoadSuccess(image);
		avatarView.Content.Should().BeOfType<Label>();
		source.Uri = new Uri("https://aka.ms/campus.jpg");

		ReportLoadSuccess(image);

		avatarView.Content.Should().BeSameAs(image);
	}

	[Fact]
	public void TextIsShownWhenTheImageSourceBecomesEmpty()
	{
		var source = new UriImageSource { Uri = new Uri("https://aka.ms/campus.jpg") };
		var avatarView = new Maui.Views.AvatarView { Text = "GL", ImageSource = source };
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		ReportLoadSuccess(image);
		avatarView.Content.Should().BeSameAs(image);
		source.ClearValue(UriImageSource.UriProperty);

		ReportLoadSuccess(image);

		avatarView.Content.Should().BeOfType<Label>();
	}

	[Fact]
	public void TextIsShownWhenTheImageSourceFailsToLoadAfterItLoaded()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		ReportLoadSuccess(image);

		ReportLoadFailure(image);

		avatarView.Content.Should().BeOfType<Label>();
	}

	// .NET MAUI reports a failed load without its source, also for a load that another source replaced, and clears the image whichever load failed.
	[Fact]
	public void ImageIsLoadedAgainWhenAReplacedLoadFailsAfterTheNewImageSourceLoaded()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		var imageHandler = new RecordingViewHandler();
		image.Handler = imageHandler;
		var loadEvents = (IImageSourcePartEvents)image;
		loadEvents.LoadingStarted();
		avatarView.ImageSource = new UriImageSource { Uri = new Uri("https://aka.ms/campus.jpg") };
		loadEvents.LoadingStarted();
		loadEvents.LoadingCompleted(true);
		imageHandler.UpdatedProperties.Clear();

		loadEvents.LoadingFailed(new InvalidOperationException("Unable to load image stream from URI."));

		avatarView.Content.Should().BeSameAs(image);
		imageHandler.UpdatedProperties.Should().Equal(nameof(Image.Source));
	}

	[Fact]
	public void ImageIsShownWhenTheNewImageSourceLoadsAfterAReplacedLoadFailed()
	{
		var avatarView = CreateAvatarViewWithAnImageSource();
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		var loadEvents = (IImageSourcePartEvents)image;
		loadEvents.LoadingStarted();
		avatarView.ImageSource = new UriImageSource { Uri = new Uri("https://aka.ms/campus.jpg") };
		loadEvents.LoadingStarted();
		loadEvents.LoadingFailed(new InvalidOperationException("Unable to load image stream from URI."));
		avatarView.Content.Should().BeOfType<Label>();

		loadEvents.LoadingCompleted(true);

		avatarView.Content.Should().BeSameAs(image);
	}

	// After a failed load the text replaces the image, which leaves the tree, where an Element loses the BindingContext it inherits: the bindings
	// of the image source keep working, so a source that a binding changes (the Email of a GravatarImageSource, for example) loads and shows the image.
	[Fact]
	public void BindingsOfTheImageSourceKeepWorkingWhileTheTextIsShown()
	{
		var viewModel = new AvatarViewModel { Uri = new Uri("https://example.invalid/avatar.png") };
		var avatarView = CreateAvatarViewWithABoundImageSource(viewModel, out var source);
		var image = avatarView.Content.Should().BeAssignableTo<Image>().Subject;
		ReportLoadFailure(image);

		viewModel.Uri = new Uri("https://aka.ms/campus.jpg");

		source.BindingContext.Should().BeSameAs(viewModel);
		source.Uri.Should().Be(viewModel.Uri);
		ReportLoadSuccess(image);
		avatarView.Content.Should().BeSameAs(image);
	}

	[Fact]
	public void ImageSourceTakesANewBindingContextWhileTheTextIsShown()
	{
		var avatarView = CreateAvatarViewWithABoundImageSource(new AvatarViewModel { Uri = new Uri("https://example.invalid/avatar.png") }, out var source);
		ReportLoadFailure(avatarView.Content.Should().BeAssignableTo<Image>().Subject);
		var otherViewModel = new AvatarViewModel { Uri = new Uri("https://aka.ms/campus.jpg") };

		avatarView.BindingContext = otherViewModel;

		source.BindingContext.Should().BeSameAs(otherViewModel);
		source.Uri.Should().Be(otherViewModel.Uri);
	}

	static ImageSource CreateEmptyImageSource(EmptyImageSourceKind kind) => kind switch
	{
		EmptyImageSourceKind.File => new FileImageSource(),
		EmptyImageSourceKind.Uri => new UriImageSource(),
		EmptyImageSourceKind.Stream => new StreamImageSource(),
		EmptyImageSourceKind.Gravatar => new GravatarImageSource(),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
	};

	static Maui.Views.AvatarView CreateAvatarViewWithABoundImageSource(AvatarViewModel viewModel, out UriImageSource source)
	{
		source = new UriImageSource();
		source.SetBinding(UriImageSource.UriProperty, new Binding(nameof(AvatarViewModel.Uri)));

		return new Maui.Views.AvatarView
		{
			Text = "GL",
			ImageSource = source,
			BindingContext = viewModel,
		};
	}

	static Maui.Views.AvatarView CreateAvatarViewWithAnImageSource() => new()
	{
		Text = "GL",
		ImageSource = new UriImageSource
		{
			Uri = new Uri("https://example.invalid/avatar.png"),
		},
	};

	// The image handler reports how a load of the image source ended through IImageSourcePartEvents, so the tests report it the same way.
	static void ReportLoadFailure(Image image)
	{
		((IImageSourcePartEvents)image).LoadingStarted();
		((IImageSourcePartEvents)image).LoadingFailed(new InvalidOperationException("Unable to load image stream from URI."));
	}

	static void ReportLoadSuccess(Image image)
	{
		((IImageSourcePartEvents)image).LoadingStarted();
		((IImageSourcePartEvents)image).LoadingCompleted(true);
	}

	sealed class AvatarViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		public Uri? Uri
		{
			get;
			set
			{
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Uri)));
			}
		}
	}

	sealed class RecordingViewHandler : IViewHandler
	{
		public List<string> UpdatedProperties { get; } = [];

		public bool HasContainer { get; set; }

		public object? PlatformView => null;

		public object? ContainerView => null;

		public IView? VirtualView { get; private set; }

		public IMauiContext? MauiContext => null;

		IElement? IElementHandler.VirtualView => VirtualView;

		public void DisconnectHandler()
		{
		}

		public Size GetDesiredSize(double widthConstraint, double heightConstraint) => Size.Zero;

		public void Invoke(string command, object? args = null)
		{
		}

		public void PlatformArrange(Rect frame)
		{
		}

		public void SetMauiContext(IMauiContext mauiContext)
		{
		}

		public void SetVirtualView(IElement view) => VirtualView = view as IView;

		public void UpdateValue(string property) => UpdatedProperties.Add(property);
	}
}
