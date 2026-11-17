using System.Collections;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ConverterTargetTypeValidationTests : BaseViewTest
{
	static readonly Type[] outputTypes =
	[
		typeof(bool), typeof(int), typeof(long), typeof(double), typeof(decimal), typeof(char), typeof(byte),
		typeof(DayOfWeek), typeof(DateTime), typeof(TimeSpan), typeof(Guid), typeof(Thickness), typeof(ValueTypeWithoutConversions),
		typeof(bool?), typeof(int?), typeof(double?), typeof(DayOfWeek?), typeof(DateTime?), typeof(Thickness?), typeof(ValueTypeWithoutConversions?),
		typeof(string), typeof(object), typeof(Uri), typeof(Color), typeof(Brush), typeof(SolidColorBrush), typeof(ImageSource),
		typeof(int[]), typeof(List<int>), typeof(ClassWithParameterlessConstructor), typeof(ClassWithoutParameterlessConstructor),
		typeof(IFormattable), typeof(ISpanFormattable), typeof(IComparable), typeof(IConvertible), typeof(IEnumerable), typeof(IList<int>),
		typeof(IDisposable), typeof(IInterfaceDeclaringToString), typeof(IInterfaceWithoutToString)
	];

	public interface IInterfaceDeclaringToString
	{
		string ToString();
	}

	public interface IInterfaceWithoutToString
	{
		int Value { get; }
	}

	public readonly record struct ValueTypeWithoutConversions(int Value);

	public sealed class ClassWithParameterlessConstructor;

	public sealed class ClassWithoutParameterlessConstructor(int value)
	{
		public int Value { get; } = value;
	}

	sealed class FormattableValue(int value) : IInterfaceDeclaringToString
	{
		public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
	}

	sealed class ValidationProbeConverter<TFrom, TTo> : BaseConverter<TFrom, TTo>
	{
		public override TTo DefaultConvertReturnValue { get; set; } = default!;

		public override TFrom DefaultConvertBackReturnValue { get; set; } = default!;

		public override TTo ConvertFrom(TFrom value, CultureInfo? culture) => default!;

		public override TFrom ConvertBackTo(TTo value, CultureInfo? culture) => default!;
	}

	[Fact]
	public void Convert_InterfaceOutputDeclaringToString_ToStringTarget_PassesValidation()
	{
		SetShouldSuppressExceptionsInConverters(false);

		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<IFormattable> { TrueObject = 42 };

		var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

		result.Should().Be(42);
	}

	[Fact]
	public void Convert_InterfaceOutputDeclaringToString_ToStringTarget_DoesNotReturnFallback()
	{
		SetShouldSuppressExceptionsInConverters(true);

		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<IFormattable> { TrueObject = 42, DefaultConvertReturnValue = 7 };

		var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

		result.Should().Be(42);
	}

	[Fact]
	public void Convert_CustomInterfaceOutputDeclaringToString_ToStringTarget_PassesValidation()
	{
		SetShouldSuppressExceptionsInConverters(false);

		var trueObject = new FormattableValue(5);
		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<IInterfaceDeclaringToString> { TrueObject = trueObject };

		var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

		result.Should().BeSameAs(trueObject);
	}

	[Fact]
	public void Binding_InterfaceOutputDeclaringToString_UpdatesLabelText()
	{
		SetShouldSuppressExceptionsInConverters(false);

		var label = new Label
		{
			BindingContext = true
		};

		label.SetBinding(Label.TextProperty, new Binding(Binding.SelfPath, converter: new BoolToObjectConverter<IFormattable> { TrueObject = 42 }));

		label.Text.Should().Be("42");
	}

	[Theory]
	[InlineData(typeof(int?))]
	[InlineData(typeof(int))]
	[InlineData(typeof(string))]
	public void Convert_NullableValueTypeOutput_CompatibleTarget_ReturnsValue(Type targetType)
	{
		SetShouldSuppressExceptionsInConverters(false);

		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<int?> { TrueObject = 5 };

		var result = converter.Convert(true, targetType, null, CultureInfo.InvariantCulture);

		result.Should().Be(5);
	}

	[Theory]
	[InlineData(typeof(long))]
	[InlineData(typeof(double?))]
	[InlineData(typeof(DayOfWeek))]
	[InlineData(typeof(object))]
	public void Convert_NullableValueTypeOutput_IncompatibleTarget_ThrowsArgumentException(Type targetType)
	{
		SetShouldSuppressExceptionsInConverters(false);

		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<int?> { TrueObject = 5 };

		var exception = Assert.Throws<ArgumentException>(() => converter.Convert(true, targetType, null, CultureInfo.InvariantCulture));

		exception.ParamName.Should().Be("targetType");
	}

	[Theory]
	[InlineData(typeof(long))]
	[InlineData(typeof(double?))]
	[InlineData(typeof(DayOfWeek))]
	[InlineData(typeof(object))]
	public void Convert_NullableValueTypeOutput_IncompatibleTarget_ReturnsFallbackWhenExceptionsAreSuppressed(Type targetType)
	{
		SetShouldSuppressExceptionsInConverters(true);

		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<int?> { TrueObject = 5, DefaultConvertReturnValue = -1 };

		var result = converter.Convert(true, targetType, null, CultureInfo.InvariantCulture);

		result.Should().Be(-1);
	}

	[Theory]
	[InlineData(typeof(int?))]
	[InlineData(typeof(int))]
	public void Convert_NullableValueTypeOutput_NullValue_CompatibleTarget_ReturnsNull(Type targetType)
	{
		SetShouldSuppressExceptionsInConverters(false);

		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<int?> { TrueObject = null };

		var result = converter.Convert(true, targetType, null, CultureInfo.InvariantCulture);

		result.Should().BeNull();
	}

	[Fact]
	public void Convert_ValueTypeOutput_NullableTarget_ReturnsValue()
	{
		SetShouldSuppressExceptionsInConverters(false);

		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<int> { TrueObject = 5 };

		var result = converter.Convert(true, typeof(int?), null, CultureInfo.InvariantCulture);

		result.Should().Be(5);
	}

	[Fact]
	public void Convert_InterfaceOutputWithoutToString_ToStringTarget_PassesValidation()
	{
		SetShouldSuppressExceptionsInConverters(false);

		var trueObject = new MemoryStream();
		ICommunityToolkitValueConverter converter = new BoolToObjectConverter<IDisposable> { TrueObject = trueObject };

		var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

		result.Should().BeSameAs(trueObject);
	}

	// The oracle is a verbatim copy of the target type check on main (1e0c110d), Activator call included, so the matrix pins every behaviour change.
	[Fact]
	public void TargetTypeValidation_RejectsNothingMainAccepted_AndOnlyRelaxesStringTargets()
	{
		SetShouldSuppressExceptionsInConverters(false);

		var newRejections = new List<string>();
		var newAcceptances = new List<string>();

		foreach (var outputType in outputTypes)
		{
			foreach (var targetType in outputTypes)
			{
				Compare("Convert", outputType, targetType,
					InvokeMainBranchOracle(outputType, targetType, shouldAllowNullableValueTypes: true),
					IsTargetTypeAcceptedByConvert(outputType, targetType));

				Compare("ConvertBack", outputType, targetType,
					InvokeMainBranchOracle(outputType, targetType, shouldAllowNullableValueTypes: false),
					IsTargetTypeAcceptedByConvertBack(outputType, targetType));
			}
		}

		newRejections.Should().BeEmpty(string.Join(Environment.NewLine, newRejections));
		newAcceptances.Should().OnlyContain(static pair => pair.EndsWith("targetType=String", StringComparison.Ordinal), string.Join(Environment.NewLine, newAcceptances));

		void Compare(string path, Type outputType, Type targetType, bool isAcceptedOnMain, bool isAcceptedNow)
		{
			var pair = $"{path}: output={Describe(outputType)} targetType={Describe(targetType)}";

			if (isAcceptedOnMain && !isAcceptedNow)
			{
				newRejections.Add(pair);
			}
			else if (!isAcceptedOnMain && isAcceptedNow)
			{
				newAcceptances.Add(pair);
			}
		}
	}

	// Main only reached Activator.CreateInstance when default(TTarget) is null; this pins that it never accepted a target type there,
	// so returning false on that path cannot reject anything main accepted.
	[Fact]
	public void MainBranchActivatorBranch_NeverAcceptsATargetTypeForNullDefaultOutputs()
	{
		var accepted = new List<string>();

		foreach (var outputType in outputTypes.Where(static type => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null))
		{
			foreach (var targetType in outputTypes)
			{
				if (outputType.IsAssignableFrom(targetType))
				{
					continue;
				}

				if (InvokeMainBranchActivatorBranch(outputType, targetType))
				{
					accepted.Add($"TTarget={Describe(outputType)} targetType={Describe(targetType)}");
				}
			}
		}

		accepted.Should().BeEmpty(string.Join(Environment.NewLine, accepted));
	}

	static void SetShouldSuppressExceptionsInConverters(bool shouldSuppress) => new Options().SetShouldSuppressExceptionsInConverters(shouldSuppress);

	static string Describe(Type type) => Nullable.GetUnderlyingType(type) is Type underlyingType ? $"{underlyingType.Name}?" : type.Name;

	static bool IsTargetTypeAcceptedByConvert(Type outputType, Type targetType)
	{
		var converter = CreateProbeConverter(typeof(object), outputType);
		return IsAccepted(() => converter.Convert(new object(), targetType, null, CultureInfo.InvariantCulture));
	}

	static bool IsTargetTypeAcceptedByConvertBack(Type outputType, Type targetType)
	{
		var converter = CreateProbeConverter(outputType, typeof(object));
		return IsAccepted(() => converter.ConvertBack(new object(), targetType, null, CultureInfo.InvariantCulture));
	}

	static ICommunityToolkitValueConverter CreateProbeConverter(Type fromType, Type toType)
	{
		var converterType = typeof(ValidationProbeConverter<,>).MakeGenericType(fromType, toType);
		return (ICommunityToolkitValueConverter)(Activator.CreateInstance(converterType) ?? throw new InvalidOperationException($"Cannot create {converterType}"));
	}

	static bool IsAccepted(Action convert)
	{
		try
		{
			convert();
			return true;
		}
		catch (ArgumentException exception) when (exception.ParamName is "targetType")
		{
			return false;
		}
	}

	static bool InvokeMainBranchOracle(Type outputType, Type targetType, bool shouldAllowNullableValueTypes) =>
		(bool)(typeof(MainBranchTargetTypeValidation).GetMethod(nameof(MainBranchTargetTypeValidation.IsAccepted))!
			.MakeGenericMethod(outputType)
			.Invoke(null, [targetType, shouldAllowNullableValueTypes]) ?? throw new InvalidOperationException());

	static bool InvokeMainBranchActivatorBranch(Type outputType, Type targetType) =>
		(bool)(typeof(MainBranchTargetTypeValidation).GetMethod(nameof(MainBranchTargetTypeValidation.ActivatorBranch))!
			.MakeGenericMethod(outputType)
			.Invoke(null, [targetType]) ?? throw new InvalidOperationException());

	static class MainBranchTargetTypeValidation
	{
		public static bool IsAccepted<TTarget>(Type targetType, bool shouldAllowNullableValueTypes) =>
			typeof(TTarget).IsAssignableFrom(targetType) || IsValidTargetType<TTarget>(targetType, shouldAllowNullableValueTypes);

		public static bool ActivatorBranch<TTarget>(Type targetType)
		{
			try
			{
				var instanceOfT = default(TTarget);
				instanceOfT ??= (TTarget?)Activator.CreateInstance(targetType);

				var result = Convert.ChangeType(instanceOfT, targetType);

				return result is not null;
			}
			catch
			{
				return false;
			}
		}

		static bool IsValidTargetType<TTarget>(in Type targetType, bool shouldAllowNullableValueTypes)
		{
			if (targetType == typeof(string) && typeof(TTarget).GetMethods().Any(x => x.Name is nameof(ToString) && x.ReturnType == typeof(string)))
			{
				return true;
			}

			if (shouldAllowNullableValueTypes && targetType.IsValueType && IsValidNullableValueType(targetType))
			{
				return true;
			}

			return ActivatorBranch<TTarget>(targetType);

			static bool IsValidNullableValueType(Type targetType)
			{
				if (Nullable.GetUnderlyingType(targetType) is not Type underlyingType)
				{
					return false;
				}

				return underlyingType == typeof(TTarget);
			}
		}
	}
}
