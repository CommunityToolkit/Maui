using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.ImageSources;

/// <summary>Gravatar image source.</summary>
/// <remarks>Note that <see cref="UriImageSource"/> is sealed and can't be used as a parent!</remarks>
public partial class GravatarImageSource : StreamImageSource
{
	static readonly Lazy<HttpClient> singletonHttpClientHolder = new();

	readonly TimeSpan cancellationTokenSourceTimeout = TimeSpan.FromMilliseconds(737);

	Uri? lastDispatch;

	/// <summary>Initializes a new instance of the <see cref="GravatarImageSource"/> class.</summary>
	public GravatarImageSource()
	{
		Stream = cancellationToken => SingletonHttpClient.DownloadStreamAsync(Uri, cancellationToken);
	}

	/// <summary>Gets a value indicating whether the control email is empty.</summary>
	public override bool IsEmpty => string.IsNullOrEmpty(Email);

	/// <summary>Gets or sets a <see cref="TimeSpan"/> structure that indicates the length of time after which the image cache becomes invalid.</summary>
	[BindableProperty]
	public partial TimeSpan CacheValidity { get; set; } = GravatarImageSourceDefaults.CacheValidity;

	/// <summary>Gets or sets a Boolean value that indicates whether caching is enabled on this <see cref="GravatarImageSource"/> object.</summary>
	[BindableProperty]
	public partial bool CachingEnabled { get; set; } = GravatarImageSourceDefaults.CachingEnabled;

	/// <summary>Gets or sets the email address.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnEmailPropertyChanged))]
	public partial string? Email { get; set; } = GravatarImageSourceDefaults.Email;

	/// <summary>Gets or sets the default image.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnDefaultImagePropertyChanged))]
	public partial DefaultImage Image { get; set; } = GravatarImageSourceDefaults.Image;

	/// <summary>Gets or sets the URI for the image to get.</summary>
	[System.ComponentModel.TypeConverter(typeof(Microsoft.Maui.Controls.UriTypeConverter))]
	public Uri Uri { get; set; } = GravatarImageSourceDefaults.Uri;

	/// <summary>Gets the parent height.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnSizePropertyChanged))]
	internal partial int ParentHeight { get; set; } = GravatarImageSourceDefaults.ParentHeight;

	/// <summary>Gets the parent width.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnSizePropertyChanged))]
	internal partial int ParentWidth { get; set; } = GravatarImageSourceDefaults.ParentWidth;

	/// <summary>Gets or sets the image size.</summary>
	/// <remarks>
	/// Size is limited to be in the range of 1 to 2048.
	/// A null value indicates that the size has not yet been set
	/// </remarks>
	int? GravatarSize
	{
		get;
		set
		{
			if (value is null)
			{
				field = null;
			}
			else
			{
				field = Math.Clamp(value.Value, 1, 2048);
			}
		}
	}

	static HttpClient SingletonHttpClient => singletonHttpClientHolder.Value;

	/// <summary>Returns the Uri as a string.</summary>
	/// <returns>String of the URI.</returns>
	public override string ToString()
	{
		return $"Uri: {Uri}\nEmail: {Email}\nSize: {GravatarSize}\nImage: {DefaultGravatarName(Image)}\nCacheValidity: {CacheValidity}\nCachingEnabled: {CachingEnabled}";
	}

	/// <summary>On parent set.</summary>
	protected override void OnParentSet()
	{
		base.OnParentSet();

		if (Parent is not VisualElement parentElement)
		{
			GravatarSize = GravatarImageSourceDefaults.GravatarSize;
			return;
		}

		SetBinding(ParentWidthProperty, BindingBase.Create<VisualElement, double>(static p => p.Width, source: parentElement, mode: BindingMode.OneWay));
		SetBinding(ParentHeightProperty, BindingBase.Create<VisualElement, double>(static p => p.Height, source: parentElement, mode: BindingMode.OneWay));
	}

	static string DefaultGravatarName(DefaultImage defaultGravatar) => defaultGravatar switch
	{
		DefaultImage.FileNotFound => "404",
		DefaultImage.MysteryPerson => "mp",
		_ => defaultGravatar.ToString().ToLower(),
	};

	static async void OnDefaultImagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		await gravatarImageSource.HandleNewUriRequested(gravatarImageSource.Email, (DefaultImage)newValue, gravatarImageSource.CancellationTokenSource?.Token ?? CancellationToken.None);
	}

	static async void OnEmailPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		await gravatarImageSource.HandleNewUriRequested((string?)newValue, gravatarImageSource.Image, gravatarImageSource.CancellationTokenSource?.Token ?? CancellationToken.None);
	}

	static async void OnSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is not (int intNewValue and > -1))
		{
			return;
		}

		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		if (gravatarImageSource.GravatarSize is null)
		{
			gravatarImageSource.GravatarSize = intNewValue;
			return;
		}

		gravatarImageSource.GravatarSize = Math.Min(gravatarImageSource.ParentWidth, gravatarImageSource.ParentHeight);
		await gravatarImageSource.HandleNewUriRequested(gravatarImageSource.Email, gravatarImageSource.Image, gravatarImageSource.CancellationTokenSource?.Token ?? CancellationToken.None);
	}

	Task HandleNewUriRequested(string? email, DefaultImage image, CancellationToken token)
	{
		if (GravatarSize is null)
		{
			return Task.CompletedTask;
		}

		Uri = IsEmpty
			? new Uri($"{GravatarImageSourceDefaults.Url}?s={GravatarSize}")
			: new Uri($"{GravatarImageSourceDefaults.Url}{email?.GetMd5Hash(string.Empty).ToLowerInvariant()}?s={GravatarSize}&d={DefaultGravatarName(image)}");

		return OnUriChanged(token);
	}

	async Task OnUriChanged(CancellationToken token)
	{
		if (Uri.Equals(lastDispatch))
		{
			return;
		}

		try
		{
			await Task.Delay(cancellationTokenSourceTimeout, token);
			await (CancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);
			lastDispatch = Uri;
			await Dispatcher.DispatchIfRequiredAsync(OnSourceChanged).WaitAsync(token);
		}
		catch (TaskCanceledException)
		{
			// Do nothing
		}
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

	/// <summary>A generated 'monster' with different colors, faces, etc.</summary>
	MonsterId,

	/// <summary>Generated faces with differing features and backgrounds.</summary>
	Wavatar,

	/// <summary>Awesome generated, 8-bit arcade-style pixilated faces.</summary>
	Retro,

	/// <summary>A generated robot with different colors, faces, etc.</summary>
	Robohash,

	/// <summary>A transparent PNG image.</summary>
	Blank,
}