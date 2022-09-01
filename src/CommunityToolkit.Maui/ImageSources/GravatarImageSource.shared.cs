namespace CommunityToolkit.Maui.ImageSources;

using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls;

/// <summary>Gravatar image source.</summary>
/// <remarks>Note that <see cref="UriImageSource"/> is sealed and can't be used as a parent!</remarks>
public class GravatarImageSource : StreamImageSource, IDisposable
{
	/// <summary>The backing store for the <see cref="CacheValidity" /> bindable property.</summary>
	public static readonly BindableProperty CacheValidityProperty = BindableProperty.Create(nameof(CacheValidity), typeof(TimeSpan), typeof(GravatarImageSource), TimeSpan.FromDays(1));

	/// <summary>The backing store for the <see cref="CachingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty CachingEnabledProperty = BindableProperty.Create(nameof(CachingEnabled), typeof(bool), typeof(GravatarImageSource), true);

	/// <summary>The backing store for the <see cref="Email" /> bindable property.</summary>
	public static readonly BindableProperty EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(GravatarImageSource), defaultValue: null, propertyChanged: OnEmailPropertyChanged);

	/// <summary>The backing store for the <see cref="Image" /> bindable property.</summary>
	public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(DefaultImage), typeof(GravatarImageSource), defaultValue: DefaultImage.MysteryPerson, propertyChanged: OnDefaultImagePropertyChanged);

	/// <summary>The backing store for the <see cref="ParentHeight" /> bindable property.</summary>
	internal static readonly BindableProperty ParentHeightProperty = BindableProperty.Create(nameof(ParentHeight), typeof(int), typeof(GravatarImageSource), defaultValue: defaultSize, propertyChanged: OnSizePropertyChanged);

	/// <summary>The backing store for the <see cref="ParentWidth" /> bindable property.</summary>
	internal static readonly BindableProperty ParentWidthProperty = BindableProperty.Create(nameof(ParentWidth), typeof(int), typeof(GravatarImageSource), defaultValue: defaultSize, propertyChanged: OnSizePropertyChanged);

	const int cancellationTokenSourceTimeout = 737;
	const string defaultGravatarImageAddress = "https://www.gravatar.com/avatar/";
	const int defaultSize = 80;
	static readonly HttpClient singletonHttpClient = new();
	int gravatarSize = -1;
	Uri? lastDispatch;
	CancellationTokenSource? tokenSource;

	/// <summary>Initializes a new instance of the <see cref="GravatarImageSource"/> class.</summary>
	public GravatarImageSource()
	{
		Stream = new Func<CancellationToken, Task<Stream>>(cancelationToken => singletonHttpClient.DownloadStreamAsync(Uri, cancelationToken));
		Uri = new Uri(defaultGravatarImageAddress);
	}

	/// <summary>Gets or sets a <see cref="TimeSpan"/> structure that indicates the length of time after which the image cache becomes invalid.</summary>
	public TimeSpan CacheValidity
	{
		get => (TimeSpan)GetValue(CacheValidityProperty);
		set => SetValue(CacheValidityProperty, value);
	}

	/// <summary>Gets or sets a Boolean value that indicates whether caching is enabled on this <see cref="GravatarImageSource"/> object.</summary>
	public bool CachingEnabled
	{
		get => (bool)GetValue(CachingEnabledProperty);
		set => SetValue(CachingEnabledProperty, value);
	}

	/// <summary>Gets or sets the email address.</summary>
	public string? Email
	{
		get => (string)GetValue(EmailProperty);
		set => SetValue(EmailProperty, value);
	}

	/// <summary>Gets or sets the default image.</summary>
	public DefaultImage Image
	{
		get => (DefaultImage)GetValue(ImageProperty);
		set => SetValue(ImageProperty, value);
	}

	/// <summary>Gets a value indicating whether the control email is empty.</summary>
	public override bool IsEmpty => string.IsNullOrEmpty(Email);

	/// <summary>Gets or sets the URI for the image to get.</summary>
	[System.ComponentModel.TypeConverter(typeof(UriTypeConverter))]
	public Uri Uri { get; set; }

	/// <summary>Gets the parent height.</summary>
	internal int ParentHeight => (int)GetValue(ParentHeightProperty);

	/// <summary>Gets the parent width.</summary>
	internal int ParentWidth => (int)GetValue(ParentWidthProperty);

	/// <summary>Gets or sets the image size.</summary>
	/// <remarks>Size is limited to be in the range of 1 to 2048.</remarks>
	int GravatarSize
	{
		get => gravatarSize;
		set => gravatarSize = Math.Clamp(value, 1, 2048);
	}

	/// <summary>Dispose <see cref="GravatarImageSource"/>.</summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>Returns the Uri as a string.</summary>
	/// <returns>String of the URI.</returns>
	public override string ToString()
	{
		return $"Uri: {Uri}, Email: {Email}, Size: {GravatarSize}, Image: {DefaultGravatarName(Image)}, CacheValidity: {CacheValidity}, CachingEnabled: {CachingEnabled}";
	}

	/// <summary>Dispose <see cref="GravatarImageSource"/>.</summary>
	/// <param name="isDisposing">Is disposing.</param>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposing && tokenSource is not null)
		{
			tokenSource.Dispose();
		}
	}

	/// <summary>On parent set.</summary>
	protected override void OnParentSet()
	{
		base.OnParentSet();
		if (Parent is not VisualElement parentElement || parentElement is null)
		{
			GravatarSize = defaultSize;
			return;
		}

		SetBinding(ParentWidthProperty, new Binding(nameof(VisualElement.Width), BindingMode.OneWay, source: parentElement));
		SetBinding(ParentHeightProperty, new Binding(nameof(VisualElement.Height), BindingMode.OneWay, source: parentElement));
	}

	static string DefaultGravatarName(DefaultImage defaultGravatar)
			=> defaultGravatar switch
			{
				DefaultImage.FileNotFound => "404",
				DefaultImage.MysteryPerson => "mp",
				_ => $"{defaultGravatar}".ToLower(),
			};

	static void OnDefaultImagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		gravatarImageSource.HandleNewUriRequested(gravatarImageSource.Email, (DefaultImage)newValue);
	}

	static void OnEmailPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		gravatarImageSource.HandleNewUriRequested((string?)newValue, gravatarImageSource.Image);
	}

	static void OnSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is not int intNewValue || intNewValue <= -1)
		{
			return;
		}

		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		if (gravatarImageSource.GravatarSize == -1)
		{
			gravatarImageSource.GravatarSize = intNewValue;
			return;
		}

		gravatarImageSource.GravatarSize = Math.Min(intNewValue, gravatarImageSource.GravatarSize);
		gravatarImageSource.HandleNewUriRequested(gravatarImageSource.Email, gravatarImageSource.Image);
	}

	void HandleNewUriRequested(string? email, DefaultImage image)
	{
		if (GravatarSize == -1)
		{
			return;
		}

		Uri = IsEmpty
			? new Uri($"{defaultGravatarImageAddress}?s={GravatarSize}")
			: new Uri($"{defaultGravatarImageAddress}{email?.GetMd5Hash(string.Empty).ToLowerInvariant()}?s={GravatarSize}&d={DefaultGravatarName(image)}");

		OnUriChanged();
	}

	void OnUriChanged()
	{
		if (tokenSource is not null)
		{
			tokenSource.Cancel();
			tokenSource.Dispose();
		}

		if (Uri.Equals(lastDispatch))
		{
			return;
		}

		tokenSource = new CancellationTokenSource();
		_ = Task.Delay(cancellationTokenSourceTimeout, tokenSource.Token).ContinueWith(task =>
		{
			if (task.IsFaulted && task.Exception is not null)
			{
				throw task.Exception;
			}

			if (task.Status == TaskStatus.Canceled)
			{
				return;
			}

			CancellationTokenSource?.Cancel();
			lastDispatch = Uri;
			Dispatcher.DispatchIfRequired(OnSourceChanged);
		});
	}
}

/// <summary>Default image enumerator.</summary>
public enum DefaultImage
{
	/// <summary>(mystery-person) A simple, cartoon-style silhouetted outline of a person (does not vary by email hash)</summary>
	MysteryPerson = 0,

	/// <summary>404: Do not load any image if none is associated with the email hash, instead return an HTTP 404 (File Not Found) response.</summary>
	FileNotFound,

	/// <summary>A geometric pattern based on an email hash.</summary>
	Identicon,

	/// <summary>A generated 'monster' with different colours, faces, etc.</summary>
	MonsterId,

	/// <summary>Generated faces with differing features and backgrounds.</summary>
	Wavatar,

	/// <summary>Awesome generated, 8-bit arcade-style pixilated faces.</summary>
	Retro,

	/// <summary>A generated robot with different colours, faces, etc.</summary>
	Robohash,

	/// <summary>A transparent PNG image.</summary>
	Blank,
}