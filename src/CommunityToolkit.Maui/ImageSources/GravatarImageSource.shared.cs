using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.ImageSources;

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
/// <summary>Gravatar image source.</summary>
/// <remarks>
/// Note that <see cref="UriImageSource"/> is sealed and can't be used as a parent!
/// <para>
/// Cancellation: <see cref="IStreamImageSource.GetStreamAsync(CancellationToken)"/> and the default <see cref="StreamImageSource.Stream"/> function throw an <see cref="OperationCanceledException"/>
/// only when the <see cref="CancellationToken"/> passed to them is canceled, because one load of a <see cref="GravatarImageSource"/> never cancels another.
/// A load that you start with <see cref="CancellationToken.None"/> never throws it, and .NET MAUI catches it for the loads that it cancels itself.
/// <see cref="ImageSource.Cancel"/> ends the loads that were started without a cancelable token, which then return <see cref="System.IO.Stream.Null"/>.
/// </para>
/// </remarks>
public partial class GravatarImageSource : StreamImageSource, IStreamImageSource, IDisposable
{
	static readonly Lazy<HttpClient> singletonHttpClientHolder = new();

	readonly TimeSpan cancellationTokenSourceTimeout = TimeSpan.FromMilliseconds(737);

	CancellationTokenSource? uriUpdateTokenSource;

	CancellationTokenSource tokenlessLoadsTokenSource = new();

	Uri? lastDispatch;

	bool isDisposed;

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
	/// <remarks>
	/// As Gravatar requires, the address is trimmed and converted to lower case before it is hashed,
	/// so <c>" User@Example.com "</c> and <c>"user@example.com"</c> load the same image.
	/// The value of this property is not changed.
	/// </remarks>
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

	static HttpClient SingletonHttpClient => singletonHttpClientHolder.Value;

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

	/// <summary>Returns the Uri as a string.</summary>
	/// <returns>String of the URI.</returns>
	public override string ToString()
	{
		return $"Uri: {Uri}\nEmail: {Email}\nSize: {GravatarSize}\nImage: {DefaultGravatarName(Image)}\nCacheValidity: {CacheValidity}\nCachingEnabled: {CachingEnabled}";
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>Ends the loads that were started without a cancelable token.</summary>
	/// <returns>A task that completes with <see langword="true"/>.</returns>
	public override Task<bool> Cancel()
	{
		// .NET MAUI calls Cancel() when a recycled ListView or TableView ImageCell disappears, and it applies whatever picture loads last.
		// Loads with a cancelable token are not touched: .NET MAUI's image handlers cancel those through their own token.
		// The canceled source is not disposed: a load can read the field just before the swap, and getting Token from a disposed source throws an ObjectDisposedException.
		Interlocked.Exchange(ref tokenlessLoadsTokenSource, new CancellationTokenSource()).Cancel();

		return Task.FromResult(true);
	}

	// .NET MAUI's StreamImageSource gives all the loads of a source one token, which the next load cancels, even for a caller that passed no token,
	// and .NET MAUI awaits some loads in async void methods that cannot catch (on Android, a Shell's FlyoutIcon and its bottom tab icons).
	async Task<System.IO.Stream> IStreamImageSource.GetStreamAsync(CancellationToken cancellationToken)
	{
		if (IsEmpty)
		{
			return System.IO.Stream.Null;
		}

		if (cancellationToken.CanBeCanceled)
		{
			return await Stream(cancellationToken);
		}

		var tokenlessLoadsToken = tokenlessLoadsTokenSource.Token;

		try
		{
			return await Stream(tokenlessLoadsToken);
		}
		catch (OperationCanceledException) when (tokenlessLoadsToken.IsCancellationRequested)
		{
			// A caller without a token cannot handle the exception, and .NET MAUI awaits some of these loads in async void methods.
			return System.IO.Stream.Null;
		}
	}

	/// <summary>Disposes the resources used by the <see cref="GravatarImageSource"/>.</summary>
	/// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (disposing)
		{
			uriUpdateTokenSource?.Cancel();
			uriUpdateTokenSource?.Dispose();
			uriUpdateTokenSource = null;
		}

		isDisposed = true;
	}

	/// <summary>On parent set.</summary>
	protected override void OnParentSet()
	{
		base.OnParentSet();
		PlatformOnParentSet();

		if (Parent is not VisualElement parentElement)
		{
			GravatarSize = GravatarImageSourceDefaults.GravatarSize;
			return;
		}

		SetBinding(ParentWidthProperty, BindingBase.Create<VisualElement, double>(static p => p.Width, source: parentElement, mode: BindingMode.OneWay));
		SetBinding(ParentHeightProperty, BindingBase.Create<VisualElement, double>(static p => p.Height, source: parentElement, mode: BindingMode.OneWay));
	}

	partial void PlatformOnParentSet();

	static string DefaultGravatarName(DefaultImage defaultGravatar) => defaultGravatar switch
	{
		DefaultImage.FileNotFound => "404",
		DefaultImage.MysteryPerson => "mp",
		_ => defaultGravatar.ToString().ToLower(),
	};

	// Gravatar hashes the address with leading and trailing white space removed and in lower case (https://docs.gravatar.com/rest/hash/),
	// and it still accepts the MD5 hash used here.
	static string? GetGravatarHash(string? email) => email?.Trim().ToLowerInvariant().GetMd5Hash(string.Empty).ToLowerInvariant();

	static async void OnDefaultImagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var gravatarImageSource = (GravatarImageSource)bindable;
		await gravatarImageSource.HandleNewUriRequested(gravatarImageSource.Email, (DefaultImage)newValue, CancellationToken.None);
	}

	static async void OnEmailPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var gravatarImageSource = (GravatarImageSource)bindable;
		await gravatarImageSource.HandleNewUriRequested((string?)newValue, gravatarImageSource.Image, CancellationToken.None);
	}

	static async void OnSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is not (int intNewValue and > -1))
		{
			return;
		}

		var gravatarImageSource = (GravatarImageSource)bindable;
		if (gravatarImageSource.GravatarSize is null)
		{
			gravatarImageSource.GravatarSize = intNewValue;
			return;
		}

		gravatarImageSource.GravatarSize = Math.Min(gravatarImageSource.ParentWidth, gravatarImageSource.ParentHeight);
		await gravatarImageSource.HandleNewUriRequested(gravatarImageSource.Email, gravatarImageSource.Image, CancellationToken.None);
	}

	Task HandleNewUriRequested(string? email, DefaultImage image, CancellationToken token)
	{
		if (GravatarSize is null)
		{
			return Task.CompletedTask;
		}

		Uri = IsEmpty
			? new Uri($"{GravatarImageSourceDefaults.Url}?s={GravatarSize}")
			: new Uri($"{GravatarImageSourceDefaults.Url}{GetGravatarHash(email)}?s={GravatarSize}&d={DefaultGravatarName(image)}");

		return OnUriChanged(token);
	}

	async Task OnUriChanged(CancellationToken token)
	{
		if (Uri.Equals(lastDispatch))
		{
			return;
		}

		var uriUpdateToken = ResetUriUpdateTokenSource(token);

		try
		{
			await Task.Delay(cancellationTokenSourceTimeout, uriUpdateToken);
			lastDispatch = Uri;
			await Dispatcher.DispatchIfRequiredAsync(OnSourceChanged, uriUpdateToken);
		}
		catch (OperationCanceledException) when (uriUpdateToken.IsCancellationRequested)
		{
			// Do nothing
		}
	}

	CancellationToken ResetUriUpdateTokenSource(CancellationToken token)
	{
		uriUpdateTokenSource?.Cancel();
		uriUpdateTokenSource?.Dispose();

		uriUpdateTokenSource = token.CanBeCanceled
			? CancellationTokenSource.CreateLinkedTokenSource(token)
			: new CancellationTokenSource();

		return uriUpdateTokenSource.Token;
	}
}