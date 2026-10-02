using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui;

/// <summary>
/// Represents a resource that is aware of the operating system theme.
/// </summary>
public sealed class AppThemeObject : AppThemeObject<object>
{
}

/// <summary>
/// Represents an object that is aware of the operating system theme.
/// </summary>
/// <remarks>
/// Values created by <see cref="Microsoft.Maui.Controls.Xaml.DynamicResourceExtension"/> cannot be preserved by this
/// public-API-only implementation because MAUI does not expose the resource key from the provided dynamic resource value.
/// </remarks>
public abstract partial class AppThemeObject<T>
{
	/// <summary>
	/// The <see cref="object"/> that is used when the operating system uses light theme.
	/// </summary>
	public T? Light { get; set; }

	/// <summary>
	/// The <see cref="object"/> that is used when the operating system uses dark theme.
	/// </summary>
	public T? Dark { get; set; }

	/// <summary>
	/// The <see cref="object"/> that is used when the current theme is unspecified or
	/// when a value is not provided for <see cref="Light"/> or <see cref="Dark"/>.
	/// </summary>
	public T? Default { get; set; }

	/// <summary>
	/// Gets a bindable object which holds the different values for each operating system theme. 
	/// </summary>
	/// <remarks>
	/// This overload cannot preserve values created by <see cref="Microsoft.Maui.Controls.Xaml.DynamicResourceExtension"/>
	/// because MAUI does not expose the resource key from the provided dynamic resource value.
	/// </remarks>
	/// <returns>A <see cref="BindingBase"/> instance with the respective theme values.</returns>
	public virtual BindingBase GetBinding()
	{
		return new Binding(
			nameof(AppThemeSource.RequestedTheme),
			converter: new AppThemeObjectConverter(Light, Dark, Default),
			source: AppThemeSource.Instance);
	}

	internal BindingBase GetBinding(BindableProperty targetProperty)
	{
		return new MultiBinding
		{
			Converter = new AppThemeObjectConverter(Light, Dark, Default),
			ConverterParameter = targetProperty,
			Bindings =
			{
				new Binding(Binding.SelfPath, source: RelativeBindingSource.Self),
				new Binding(nameof(AppThemeSource.RequestedTheme), source: AppThemeSource.Instance)
			}
		};
	}

	sealed class AppThemeObjectConverter(T? light, T? dark, T? defaultValue) : IValueConverter, IMultiValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var requestedTheme = value is AppTheme theme ? theme : AppInfo.RequestedTheme;

			return GetValue(requestedTheme);
		}

		public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var target = values.Length > 0 ? values[0] : null;
			var requestedTheme = values.Length > 1 && values[1] is AppTheme theme
				? theme
				: AppInfo.RequestedTheme;

			var value = GetValue(requestedTheme);

			if (target is Element targetElement && parameter is BindableProperty targetProperty)
			{
				targetElement.RemoveDynamicResource(targetProperty);
			}

			return value;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			throw new NotSupportedException($"{nameof(AppThemeObject<T>)} only supports one-way bindings.");

		public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
			throw new NotSupportedException($"{nameof(AppThemeObject<T>)} only supports one-way bindings.");

		object? GetValue(AppTheme requestedTheme) =>
			requestedTheme switch
			{
				AppTheme.Dark => dark ?? defaultValue,
				_ => light ?? defaultValue
			};
	}

	sealed partial class AppThemeSource : INotifyPropertyChanged
	{
		Application? subscribedApplication;

		public event PropertyChangedEventHandler? PropertyChanged;

		public static AppThemeSource Instance { get; } = new();

		public AppTheme RequestedTheme
		{
			get
			{
				Subscribe(Application.Current);

				return Application.Current?.RequestedTheme ?? AppInfo.RequestedTheme;
			}
		}

		void Subscribe(Application? application)
		{
			if (ReferenceEquals(subscribedApplication, application))
			{
				return;
			}

			if (subscribedApplication is not null)
			{
				subscribedApplication.RequestedThemeChanged -= OnRequestedThemeChanged;
			}

			subscribedApplication = application;

			if (subscribedApplication is not null)
			{
				subscribedApplication.RequestedThemeChanged += OnRequestedThemeChanged;
			}
		}

		void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RequestedTheme)));
	}
}