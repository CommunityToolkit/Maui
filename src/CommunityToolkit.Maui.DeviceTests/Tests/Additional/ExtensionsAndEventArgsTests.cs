using System.Reflection;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Additional;

#region MauiDrawingLineCompletedEventArgs Tests

public class MauiDrawingLineCompletedEventArgsTests
{
	[Fact]
	public void MauiDrawingLineCompletedEventArgs_CarriesLine()
	{
		var line = new MauiDrawingLine();
		var args = new MauiDrawingLineCompletedEventArgs(line);

		Assert.Same(line, args.Line);
	}

	[Fact]
	public void MauiDrawingLineCompletedEventArgs_IsEventArgs()
	{
		var line = new MauiDrawingLine();
		var args = new MauiDrawingLineCompletedEventArgs(line);

		Assert.IsAssignableFrom<EventArgs>(args);
	}
}

#endregion

#region NavigationEventArgsExtensions Tests

public class NavigationEventArgsExtensionsTests
{
	[Fact]
	public void NavigationEventArgsExtensions_MethodsExist()
	{
		var type = typeof(NavigationEventArgsExtensions);
		var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

		Assert.Contains(methods, m => m.Name == "IsDestinationPageACommunityToolkitPopupPage");
		Assert.Contains(methods, m => m.Name == "WasPreviousPageACommunityToolkitPopupPage");
	}

	[Fact]
	public void NavigationEventArgsExtensions_IsDestinationPageACommunityToolkitPopupPage_HasNavigatedFromOverload()
	{
		var method = typeof(NavigationEventArgsExtensions).GetMethod("IsDestinationPageACommunityToolkitPopupPage", [typeof(NavigatedFromEventArgs)]);
		Assert.NotNull(method);
		Assert.Equal(typeof(bool), method.ReturnType);
	}

	[Fact]
	public void NavigationEventArgsExtensions_WasPreviousPageACommunityToolkitPopupPage_HasNavigatedToOverload()
	{
		var method = typeof(NavigationEventArgsExtensions).GetMethod("WasPreviousPageACommunityToolkitPopupPage", [typeof(NavigatedToEventArgs)]);
		Assert.NotNull(method);
		Assert.Equal(typeof(bool), method.ReturnType);
	}

	[Fact]
	public void NavigationEventArgsExtensions_IsDestinationPageACommunityToolkitPopupPage_HasNavigatingFromOverload()
	{
		var method = typeof(NavigationEventArgsExtensions).GetMethod("IsDestinationPageACommunityToolkitPopupPage", [typeof(NavigatingFromEventArgs)]);
		Assert.NotNull(method);
		Assert.Equal(typeof(bool), method.ReturnType);
	}
}

#endregion

#region ServiceCollectionExtensions Tests

public partial class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddTransientPopup_RegistersPopup()
	{
		var services = new ServiceCollection();
		var result = services.AddTransientPopup<Popup>();

		Assert.Same(services, result);
		Assert.Contains(services, sd => sd.ServiceType == typeof(Popup));
	}

	[Fact]
	public void AddSingletonPopup_RegistersPopup()
	{
		var services = new ServiceCollection();
		var result = services.AddSingletonPopup<Popup>();

		Assert.Same(services, result);
		Assert.Contains(services, sd => sd.ServiceType == typeof(Popup));
	}

	[Fact]
	public void AddTransientPopup_WithViewModel_RegistersBoth()
	{
		var services = new ServiceCollection();
		var result = services.AddTransientPopup<Popup, TestPopupViewModel>();

		Assert.Same(services, result);
		Assert.Contains(services, sd => sd.ServiceType == typeof(Popup));
		Assert.Contains(services, sd => sd.ServiceType == typeof(TestPopupViewModel));
	}

	[Fact]
	public void AddSingletonPopup_WithViewModel_RegistersBoth()
	{
		var services = new ServiceCollection();
		var result = services.AddSingletonPopup<Popup, TestPopupViewModel>();

		Assert.Same(services, result);
		Assert.Contains(services, sd => sd.ServiceType == typeof(Popup));
		Assert.Contains(services, sd => sd.ServiceType == typeof(TestPopupViewModel));
	}

	partial class TestPopupViewModel : System.ComponentModel.INotifyPropertyChanged
	{
#pragma warning disable CS0067 // Event is required by INotifyPropertyChanged but never raised in this test stub
		public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
	}
}

#endregion

#region AppThemeObjectExtensions Tests

public class AppThemeObjectExtensionsTests
{
	[Fact]
	public void SetAppThemeColor_MethodExists()
	{
		var method = typeof(AppThemeObjectExtensions).GetMethod("SetAppThemeColor", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		Assert.NotNull(method);

		var parameters = method.GetParameters();
		Assert.Equal(3, parameters.Length);
		Assert.Equal(typeof(BindableObject), parameters[0].ParameterType);
		Assert.Equal(typeof(BindableProperty), parameters[1].ParameterType);
		Assert.Equal(typeof(AppThemeColor), parameters[2].ParameterType);
	}

	[Fact]
	public void SetAppTheme_MethodExists()
	{
		var method = typeof(AppThemeObjectExtensions).GetMethod("SetAppTheme", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		Assert.NotNull(method);
		Assert.True(method.IsGenericMethod);
	}

	[Fact]
	public void SetAppThemeColor_DoesNotThrow()
	{
		var label = new Label();
		var appThemeColor = new AppThemeColor
		{
			Light = Colors.White,
			Dark = Colors.Black,
		};

		// Should not throw
		label.SetAppThemeColor(Label.TextColorProperty, appThemeColor);
		Assert.True(true);
	}
}

#endregion

#region MultiValueConverterExtension Tests

public partial class MultiValueConverterExtensionTests
{
	class TestMultiConverter : MultiValueConverterExtension, ICommunityToolkitMultiValueConverter
	{
		public object? Convert(object?[]? values, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
		{
			return values?.Length > 0 ? values[0] : null;
		}

		public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo? culture)
		{
			return value is null ? null : [value];
		}
	}

	[Fact]
	public void MultiValueConverterExtension_ProvideValue_ReturnsSelf()
	{
		var converter = new TestMultiConverter();
		var result = converter.ProvideValue(new TestServiceProvider());

		Assert.Same(converter, result);
	}

	[Fact]
	public void MultiValueConverterExtension_IsIMarkupExtension()
	{
		var converter = new TestMultiConverter();
		Assert.IsAssignableFrom<IMarkupExtension<ICommunityToolkitMultiValueConverter>>(converter);
	}

	partial class TestServiceProvider : IServiceProvider
	{
		public object? GetService(Type serviceType) => null;
	}
}

#endregion

#region NullableExtensions Tests (internal, via reflection)

public class NullableExtensionsTests
{
	// NullableExtensions lives in CommunityToolkit.Maui.Core, not CommunityToolkit.Maui
	static readonly Type nullableExtensionsType = typeof(CommunityToolkit.Maui.Core.Extensions.ColorConversionExtensions).Assembly
		.GetType("CommunityToolkit.Maui.Core.Extensions.NullableExtensions")
		?? throw new InvalidOperationException("NullableExtensions type not found");

	static readonly MethodInfo isNullableMethod = nullableExtensionsType
		.GetMethod("IsNullable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException("IsNullable method not found");

	[Fact]
	public void IsNullable_ReferenceType_ReturnsTrue()
	{
		var result = isNullableMethod.Invoke(null, [typeof(string)]);
		Assert.Equal(true, result);
	}

	[Fact]
	public void IsNullable_NullableValueType_ReturnsTrue()
	{
		var result = isNullableMethod.Invoke(null, [typeof(int?)]);
		Assert.Equal(true, result);
	}

	[Fact]
	public void IsNullable_ValueType_ReturnsFalse()
	{
		var result = isNullableMethod.Invoke(null, [typeof(int)]);
		Assert.Equal(false, result);
	}

	[Fact]
	public void IsNullable_Bool_ReturnsFalse()
	{
		var result = isNullableMethod.Invoke(null, [typeof(bool)]);
		Assert.Equal(false, result);
	}

	[Fact]
	public void IsNullable_NullableBool_ReturnsTrue()
	{
		var result = isNullableMethod.Invoke(null, [typeof(bool?)]);
		Assert.Equal(true, result);
	}

	[Fact]
	public void IsNullable_Object_ReturnsTrue()
	{
		var result = isNullableMethod.Invoke(null, [typeof(object)]);
		Assert.Equal(true, result);
	}
}

#endregion

#region PropertyChangedEventArgsExtensions Tests

public class PropertyChangedEventArgsExtensionsTests
{
	[Fact]
	public void IsOneOf_MatchingProperty_ReturnsTrue()
	{
		var result = "Text".IsOneOf(Label.TextProperty);
		Assert.True(result);
	}

	[Fact]
	public void IsOneOf_NonMatchingProperty_ReturnsFalse()
	{
		var result = "TextColor".IsOneOf(Label.TextProperty);
		Assert.False(result);
	}

	[Fact]
	public void IsOneOf_MultipleProperties_MatchesOne_ReturnsTrue()
	{
		var result = "FontSize".IsOneOf(Label.TextProperty, Label.FontSizeProperty);
		Assert.True(result);
	}

	[Fact]
	public void IsOneOf_EmptyProperties_ReturnsFalse()
	{
		var result = "Text".IsOneOf();
		Assert.False(result);
	}
}

#endregion

#region PopupExtensions Tests

public class PopupExtensionsTests
{
	[Fact]
	public void PopupExtensions_ShowPopup_MethodExists()
	{
		var type = typeof(PopupExtensions);
		var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

		Assert.Contains(methods, m => m.Name == "ShowPopup");
	}

	[Fact]
	public void PopupExtensions_ShowPopupAsync_MethodExists()
	{
		var type = typeof(PopupExtensions);
		var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

		Assert.Contains(methods, m => m.Name == "ShowPopupAsync");
	}

	[Fact]
	public void PopupExtensions_ClosePopupAsync_MethodExists()
	{
		var type = typeof(PopupExtensions);
		var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

		// The public API is ClosePopupAsync, not ClosePopup
		Assert.Contains(methods, m => m.Name == "ClosePopupAsync");
	}
}

#endregion

#region ColorAnimationExtensions Tests

public class ColorAnimationExtensionsTests
{
	[Fact]
	public void BackgroundColorTo_MethodExists()
	{
		var method = typeof(ColorAnimationExtensions).GetMethod("BackgroundColorTo", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(method);
	}

	[Fact]
	public void BackgroundColorTo_HasCorrectParameters()
	{
		var method = typeof(ColorAnimationExtensions).GetMethod("BackgroundColorTo", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(method);

		var parameters = method.GetParameters();
		Assert.Equal(6, parameters.Length);
		Assert.Equal(typeof(VisualElement), parameters[0].ParameterType);
		Assert.Equal(typeof(Color), parameters[1].ParameterType);
		Assert.Equal(typeof(uint), parameters[2].ParameterType);
		Assert.Equal(typeof(uint), parameters[3].ParameterType);
		Assert.Equal(typeof(Easing), parameters[4].ParameterType);
		Assert.Equal(typeof(CancellationToken), parameters[5].ParameterType);
	}

	[Fact]
	public void BackgroundColorTo_DefaultParameters_AreCorrect()
	{
		var method = typeof(ColorAnimationExtensions).GetMethod("BackgroundColorTo", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(method);

		var parameters = method.GetParameters();
		Assert.Equal(16u, parameters[2].DefaultValue); // rate
		Assert.Equal(250u, parameters[3].DefaultValue); // length
		Assert.Null(parameters[4].DefaultValue); // easing
	}
}

#endregion

#region AppThemeResourceExtension Tests

public class AppThemeResourceExtensionTests
{
	[Fact]
	public void AppThemeResourceExtension_CanBeCreated()
	{
		var extension = new AppThemeResourceExtension();
		Assert.NotNull(extension);
	}

	[Fact]
	public void AppThemeResourceExtension_Key_DefaultIsNull()
	{
		var extension = new AppThemeResourceExtension();
		Assert.Null(extension.Key);
	}

	[Fact]
	public void AppThemeResourceExtension_SetKey_UpdatesValue()
	{
		var extension = new AppThemeResourceExtension
		{
			Key = "TestKey",
		};

		Assert.Equal("TestKey", extension.Key);
	}

	[Fact]
	public void AppThemeResourceExtension_IsIMarkupExtension()
	{
		var extension = new AppThemeResourceExtension();
		Assert.IsAssignableFrom<IMarkupExtension<BindingBase>>(extension);
	}
}

#endregion
