using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

// A DynamicallyAccessedMembers annotation on a public generic parameter makes every consumer whose type argument lacks the annotation
// get IL2091 warnings, so the public converter surface must stay free of them.
public class ConverterTrimAnnotationTests
{
	public static TheoryData<Type> ConverterTypesWithoutAnnotatedTypeParameters => new()
	{
		typeof(BaseConverter<,>),
		typeof(BaseConverter<,,>),
		typeof(BaseConverterOneWay<,>),
		typeof(BaseConverterOneWay<,,>),
		typeof(BoolToObjectConverter<>),
		typeof(CompareConverter<,>)
	};

	[Theory]
	[MemberData(nameof(ConverterTypesWithoutAnnotatedTypeParameters))]
	public void ConverterTypeParameters_CarryNoDynamicallyAccessedMembers(Type genericTypeDefinition)
	{
		foreach (var typeParameter in genericTypeDefinition.GetGenericArguments())
		{
			Assert.Null(typeParameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
		}
	}

	[Fact]
	public void IsInRangeConverterTypeParameters_RequireOnlyAPublicParameterlessConstructor()
	{
		var typeParameters = typeof(IsInRangeConverter<,>).GetGenericArguments();

		Assert.Equal(2, typeParameters.Length);
		foreach (var typeParameter in typeParameters)
		{
			var annotation = typeParameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
			Assert.NotNull(annotation);
			Assert.Equal(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, annotation.MemberTypes);
		}
	}

	[Fact]
	public void CommunityToolkitValueConverterTargetType_CarriesNoDynamicallyAccessedMembers()
	{
		var interfaceType = typeof(ICommunityToolkitValueConverter);

		foreach (var method in interfaceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
		{
			Assert.Null(method.ReturnParameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
			Assert.Null(method.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
			foreach (var parameter in method.GetParameters())
			{
				Assert.Null(parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
			}
		}

		foreach (var propertyName in new[] { nameof(ICommunityToolkitValueConverter.FromType), nameof(ICommunityToolkitValueConverter.ToType) })
		{
			var property = interfaceType.GetProperty(propertyName) ?? throw new InvalidOperationException($"{propertyName} is missing");
			Assert.Null(property.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
			Assert.Null(property.GetMethod?.ReturnParameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
		}
	}
}