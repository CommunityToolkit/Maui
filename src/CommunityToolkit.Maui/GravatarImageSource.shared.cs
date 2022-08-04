using System;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using UriTypeConverter = Microsoft.Maui.Controls.UriTypeConverter;

namespace CommunityToolkit.Maui;

/// <summary>Gravatar image source.</summary>
/// <remarks>Note that <see cref="UriImageSource"/> is sealed and can't be used as a parent!</remarks>
public class GravatarImageSource : StreamImageSource, IUriImageSource
{
	/// <summary>The backing store for the <see cref="CacheValidity" /> bindable property.</summary>
	public static readonly BindableProperty CacheValidityProperty =
		BindableProperty.Create(nameof(CacheValidity), typeof(TimeSpan), typeof(UriImageSource), TimeSpan.FromDays(1));

	/// <summary>The backing store for the <see cref="CachingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty CachingEnabledProperty =
		BindableProperty.Create(nameof(CachingEnabled), typeof(bool), typeof(UriImageSource), true);

	/// <summary>The backing store for the <see cref="Email" /> bindable property.</summary>
	public static readonly BindableProperty EmailProperty =
		BindableProperty.Create(nameof(Email), typeof(string), typeof(GravatarImageSource), defaultValue: null, propertyChanged: OnEmailPropertyChanged);

	/// <summary>The backing store for the <see cref="Image" /> bindable property.</summary>
	public static readonly BindableProperty ImageProperty =
		BindableProperty.Create(nameof(Image), typeof(DefaultImage), typeof(GravatarImageSource), defaultValue: GravatarImageSourceDefaults.Defaultimage, propertyChanged: OnDefaultImagePropertyChanged);

	int gravatarSize;

	/// <summary>Initializes a new instance of the <see cref="GravatarImageSource"/> class.</summary>
	public GravatarImageSource()
	{
		Stream = new Func<CancellationToken, Task<Stream>>(c => DownloadStreamAsync(Uri, c));
		Uri = new Uri("https://www.gravatar.com/avatar/");
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
	public string Email
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
		return $"Uri: {Uri}";
	}

	/// <summary>On parent set.</summary>
	protected override void OnParentSet()
	{
		object element = Parent;
		base.OnParentSet();
		if (element is Microsoft.Maui.Controls.Image parentImage)
		{
			var height = parentImage.Height >= 0 ? parentImage.Height : Math.Max(parentImage.HeightRequest, GravatarImageSourceDefaults.DefaultSize);
			var width = parentImage.Width >= 0 ? parentImage.Width : Math.Max(parentImage.WidthRequest, GravatarImageSourceDefaults.DefaultSize);
			var sizeFromParent = (int)Math.Max(width, height);
			if (GravatarSize != sizeFromParent)
			{
				GravatarSize = sizeFromParent;
				HandleSizeChanged(sizeFromParent);
			}
		}
	}

	static string DefaultGravatarName(DefaultImage defaultGravatar)
			=> defaultGravatar switch
			{
				DefaultImage.FileNotFound => "404",
				DefaultImage.MysteryPerson => "mp",
				_ => $"{defaultGravatar}".ToLower(),
			};

	static string GetMd5Hash(string str)
	{
		if (string.IsNullOrWhiteSpace(str))
		{
			str = "MCT";
		}

		using var md5 = MD5.Create();
		Span<byte> hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
		return BitConverter.ToString(hash.ToArray(), 0, hash.Length).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
	}

	static void OnDefaultImagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		gravatarImageSource.HandleDefaultImageChanged((DefaultImage)newValue);
	}

	static void OnEmailPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		GravatarImageSource gravatarImageSource = (GravatarImageSource)bindable;
		gravatarImageSource.HandleEmailChanged((string)newValue);
	}

	void HandleDefaultImageChanged(DefaultImage newValue)
	{
		Uri = new Uri($"https://www.gravatar.com/avatar/{GetMd5Hash(Email)}?s={GravatarSize}&d={DefaultGravatarName(newValue)}");
		OnUriChanged();
	}

	void HandleEmailChanged(string newValue)
	{
		Uri = new Uri($"https://www.gravatar.com/avatar/{GetMd5Hash(newValue)}?s={GravatarSize}&d={DefaultGravatarName(Image)}");
		OnUriChanged();
	}

	void HandleSizeChanged(int newValue)
	{
		Uri = new Uri($"https://www.gravatar.com/avatar/{GetMd5Hash(Email)}?s={newValue}&d={DefaultGravatarName(Image)}");
		OnUriChanged();
	}

	void OnUriChanged()
	{
		OnSourceChanged();
	}

	async Task<Stream> DownloadStreamAsync(Uri? uri, CancellationToken cancellationToken)
	{
		try
		{
			using var client = new HttpClient();
			// Do not remove this await otherwise the client will dispose before the stream even starts
			return await StreamWrapper.GetStreamAsync(uri, cancellationToken, client).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Application.Current?.FindMauiContext()?.CreateLogger<GravatarImageSource>()?.LogWarning(ex, "Error getting stream for {Uri}", Uri);
			return System.IO.Stream.Null;
		}
	}
}
