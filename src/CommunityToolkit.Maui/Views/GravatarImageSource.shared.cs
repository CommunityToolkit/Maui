namespace CommunityToolkit.Maui.Views;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

/// <summary>Gravatar image source.</summary>
/// <remarks>Note that <see cref="UriImageSource"/> is sealed and can't be used as a parent!</remarks>
public class GravatarImageSource : StreamImageSource
{
	/// <summary>The backing store for the <see cref="CacheValidity" /> bindable property.</summary>
	public static readonly BindableProperty CacheValidityProperty = BindableProperty.Create(nameof(CacheValidity), typeof(TimeSpan), typeof(UriImageSource), TimeSpan.FromDays(1));

	/// <summary>The backing store for the <see cref="CachingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty CachingEnabledProperty = BindableProperty.Create(nameof(CachingEnabled), typeof(bool), typeof(UriImageSource), true);

	/// <summary>The backing store for the <see cref="Email" /> bindable property.</summary>
	public static readonly BindableProperty? EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(GravatarImageSource), defaultValue: null, propertyChanged: OnEmailPropertyChanged);

	/// <summary>The backing store for the <see cref="Image" /> bindable property.</summary>
	public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(DefaultImage), typeof(GravatarImageSource), defaultValue: DefaultImage.MysteryPerson, propertyChanged: OnDefaultImagePropertyChanged);

	const string defaultGravatarImageAddress = "https://www.gravatar.com/avatar/";
	const int defaultSize = 80;
	static readonly HttpClient singletonHttpClient = new();
	int gravatarSize;

	/// <summary>Initializes a new instance of the <see cref="GravatarImageSource"/> class.</summary>
	public GravatarImageSource()
	{
		Stream = new Func<CancellationToken, Task<Stream>>(cancelationToken => DownloadStreamAsync(Uri, cancelationToken));
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

	/// <summary>Gets a value indicating whether the control URI is empty.</summary>
	public override bool IsEmpty => Uri == null;

	/// <summary>Gets or sets the URI for the image to get.</summary>
	[System.ComponentModel.TypeConverter(typeof(UriTypeConverter))]
	public Uri Uri { get; private set; }

	/// <summary>Gets or sets the control size property.</summary>
	int GravatarSize
	{
		get => gravatarSize;
		set => gravatarSize = Math.Clamp(value, 1, 2048);
	}

	/// <summary>Returns the Uri as a string.</summary>
	/// <returns>String of the URI.</returns>
	public override string ToString()
	{
		return $"Uri: {Uri}, Email:{Email}, Size:{GravatarSize}, Image: {DefaultGravatarName(Image)}, CacheValidity:{CacheValidity}, CachingEnabled:{CachingEnabled}";
	}

	/// <summary>On parent set.</summary>
	protected override void OnParentSet()
	{
		object element = Parent;
		base.OnParentSet();
		if (element is Microsoft.Maui.Controls.Image parentImage)
		{
			var height = parentImage.Height >= 0 ? parentImage.Height : Math.Max(parentImage.HeightRequest, defaultSize);
			var width = parentImage.Width >= 0 ? parentImage.Width : Math.Max(parentImage.WidthRequest, defaultSize);
			var sizeFromParent = (int)Math.Max(width, height);
			if (GravatarSize != sizeFromParent)
			{
				GravatarSize = sizeFromParent;
				HandleNewUriRequested(Email, Image);
			}
		}
	}

	/// <summary>On property changed.</summary>
	/// <param name="propertyName">Property name.</param>
	protected override void OnPropertyChanged(string propertyName)
	{
		if (propertyName is not null && (propertyName == CacheValidityProperty.PropertyName || propertyName == CachingEnabledProperty.PropertyName))
		{
			OnUriChanged();
		}

		base.OnPropertyChanged(propertyName);
	}

	static string DefaultGravatarName(DefaultImage defaultGravatar)
			=> defaultGravatar switch
			{
				DefaultImage.FileNotFound => "404",
				DefaultImage.MysteryPerson => "mp",
				_ => $"{defaultGravatar}".ToLower(),
			};

	static string GetMd5Hash(ReadOnlySpan<char> str)
	{
		if (str.Length is 0)
		{
			return string.Empty;
		}

		using var md5 = MD5.Create();
		Span<byte> hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str.ToArray()));
		return BitConverter.ToString(hash.ToArray(), 0, hash.Length).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
	}

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

	async Task<Stream> DownloadStreamAsync(Uri? uri, CancellationToken cancellationToken)
	{
		try
		{
			return await StreamWrapper.GetStreamAsync(uri, cancellationToken, singletonHttpClient).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Application.Current?.FindMauiContext()?.CreateLogger<GravatarImageSource>()?.LogWarning(ex, "Error getting stream for {Uri}", Uri);
			return System.IO.Stream.Null;
		}
	}

	void HandleNewUriRequested(string? email, DefaultImage image)
	{
		Uri = string.IsNullOrWhiteSpace(email)
			? new Uri($"{defaultGravatarImageAddress}?s={GravatarSize}")
			: new Uri($"{defaultGravatarImageAddress}{GetMd5Hash(email)}?s={GravatarSize}&d={DefaultGravatarName(image)}");

		OnUriChanged();
	}

	void OnUriChanged()
	{
		CancellationTokenSource?.Cancel();
		OnSourceChanged();
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