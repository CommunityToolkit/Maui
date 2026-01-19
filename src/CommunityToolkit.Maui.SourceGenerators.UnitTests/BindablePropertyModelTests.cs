using CommunityToolkit.Maui.SourceGenerators.Models;
using CommunityToolkit.Maui.SourceGenerators.UnitTests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class BindablePropertyModelTests : BaseTest
{
	[Fact]
	public void BindablePropertyName_ReturnsCorrectPropertyName()
	{
		// Arrange
		var compilation = CreateCompilation("public class TestClass { public string TestProperty { get; set; } }");
		var typeSymbol = compilation.GetTypeByMetadataName("TestClass")!;
		var propertySymbol = typeSymbol.GetMembers("TestProperty").OfType<IPropertySymbol>().First();

		var model = new BindablePropertyModel(
			"TestProperty",
			propertySymbol.Type,
			typeSymbol,
			"Microsoft.Maui.Controls.BindingMode.OneWay",
			"null",
			"null",
			"null",
			"null",
			"null",
			string.Empty,
			true, // IsReadOnlyBindableProperty
			string.Empty, // SetterAccessibility
			false,
			"null"
		);

		// Act
		var bindablePropertyName = model.BindablePropertyName;

		// Assert
		Assert.Equal("TestPropertyProperty", bindablePropertyName);
	}

	[Fact]
	public void BindablePropertyModel_WithAllParameters_StoresCorrectValues()
	{
		// Arrange
		var compilation = CreateCompilation("public class TestClass { public string TestProperty { get; set; } }");
		var typeSymbol = compilation.GetTypeByMetadataName("TestClass")!;
		var propertySymbol = typeSymbol.GetMembers("TestProperty").OfType<IPropertySymbol>().First();

		const string propertyName = "TestProperty";
		const string defaultBindingMode = "Microsoft.Maui.Controls.BindingMode.TwoWay";
		const string validateValueMethodName = "ValidateValue";
		const string propertyChangedMethodName = "OnPropertyChanged";
		const string propertyChangingMethodName = "OnPropertyChanging";
		const string coerceValueMethodName = "CoerceValue";
		const string defaultValueCreatorMethodName = "CreateDefaultValue";
		const string newKeywordText = "new ";
		const bool hasInitializer = false;
		const string propertyAccessibility = "public";

		// Act
		var model = new BindablePropertyModel(
			propertyName,
			propertySymbol.Type,
			typeSymbol,
			defaultBindingMode,
			validateValueMethodName,
			propertyChangedMethodName,
			propertyChangingMethodName,
			coerceValueMethodName,
			defaultValueCreatorMethodName,
			newKeywordText,
			true, // IsReadOnlyBindableProperty
			string.Empty, // SetterAccessibility
			hasInitializer,
			propertyAccessibility
		);

		// Assert
		Assert.Equal(propertyName, model.PropertyName);
		Assert.Equal(propertySymbol.Type, model.ReturnType);
		Assert.Equal(typeSymbol, model.DeclaringType);
		Assert.Equal(defaultBindingMode, model.DefaultBindingMode);
		Assert.Equal(validateValueMethodName, model.ValidateValueMethodName);
		Assert.Equal(propertyChangedMethodName, model.PropertyChangedMethodName);
		Assert.Equal(propertyChangingMethodName, model.PropertyChangingMethodName);
		Assert.Equal(coerceValueMethodName, model.CoerceValueMethodName);
		Assert.Equal(defaultValueCreatorMethodName, model.DefaultValueCreatorMethodName);
		Assert.Equal(newKeywordText, model.NewKeywordText);
		Assert.Equal(hasInitializer, model.HasInitializer);
		Assert.Equal("TestPropertyProperty", model.BindablePropertyName);
		Assert.Equal(defaultValueCreatorMethodName, model.EffectiveDefaultValueCreatorMethodName);
		Assert.Equal("IsInitializingTestProperty", model.InitializingPropertyName);
		Assert.Equal(propertyAccessibility, model.PropertyAccessibility);
	}

	[Fact]
	public void ClassInformation_WithAllParameters_StoresCorrectValues()
	{
		// Arrange
		const string className = "TestClass";
		const string declaredAccessibility = "public";
		const string containingNamespace = "TestNamespace";

		// Act
		var classInfo = new ClassInformation(className, declaredAccessibility, containingNamespace);

		// Assert
		Assert.Equal(className, classInfo.ClassName);
		Assert.Equal(declaredAccessibility, classInfo.DeclaredAccessibility);
		Assert.Equal(containingNamespace, classInfo.ContainingNamespace);
	}

	[Fact]
	public void SemanticValues_WithClassInfoAndProperties_StoresCorrectValues()
	{
		// Arrange
		var compilation = CreateCompilation("public class TestClass { public string TestProperty { get; set; } }");
		var typeSymbol = compilation.GetTypeByMetadataName("TestClass")!;
		var propertySymbol = typeSymbol.GetMembers("TestProperty").OfType<IPropertySymbol>().First();

		var classInfo = new ClassInformation("TestClass", "public", "TestNamespace");
		var bindableProperty = new BindablePropertyModel(
			"TestProperty",
			propertySymbol.Type,
			typeSymbol,
			"Microsoft.Maui.Controls.BindingMode.OneWay",
			"null",
			"null",
			"null",
			"null",
			"null",
			string.Empty,
			true, // IsReadOnlyBindableProperty
			string.Empty, // SetterAccessibilityText
			false,
			"public"
		);

		var bindableProperties = new[] { bindableProperty }.ToImmutableArray();

		// Act
		var semanticValues = new BindablePropertySemanticValues(classInfo, bindableProperties);

		// Assert
		Assert.Equal(classInfo, semanticValues.ClassInformation);
		Assert.Equal(bindableProperties, semanticValues.BindableProperties);
		Assert.Single(semanticValues.BindableProperties);
	}

	static Compilation CreateCompilation(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
		};

		return CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	[Fact]
	public void BindablePropertyName_WithInitializer_ReturnsCorrectEffectiveDefaultValueCreatorMethodName()
	{
		// Arrange
		var compilation = CreateCompilation(
			/* language=C#-test */
			//lang=csharp
			"""
			public class TestClass { public string TestProperty { get; set; } = "Initial Value"; }");
			""");
		var typeSymbol = compilation.GetTypeByMetadataName("TestClass")!;
		var propertySymbol = typeSymbol.GetMembers("TestProperty").OfType<IPropertySymbol>().First();

		const string propertyName = "TestProperty";
		const bool hasInitializer = true;

		var model = new BindablePropertyModel(
			propertyName,
			propertySymbol.Type,
			typeSymbol,
			"Microsoft.Maui.Controls.BindingMode.OneWay",
			"null",
			"null",
			"null",
			"null",
			"null",
			string.Empty,
			true,
			string.Empty,
			hasInitializer,
			"public"
		);

		// Act
		var effectiveDefaultValueCreatorMethodName = model.EffectiveDefaultValueCreatorMethodName;

		// Assert
		Assert.Equal("CreateDefault" + propertyName, effectiveDefaultValueCreatorMethodName);
	}

	[Fact]
	public void BindablePropertyName_WithInitializerAndDefaulValueCreator_ReturnsCorrectEffectiveDefaultValueCreatorMethodName()
	{
		// Arrange
		var compilation = CreateCompilation(
			/* language=C#-test */
			//lang=csharp
			"""
			public class TestClass { public string TestProperty { get; set; } = "Initial Value"; }
			""");
		var typeSymbol = compilation.GetTypeByMetadataName("TestClass")!;
		var propertySymbol = typeSymbol.GetMembers("TestProperty").OfType<IPropertySymbol>().First();

		const string propertyName = "TestProperty";
		const bool hasInitializer = true;

		var model = new BindablePropertyModel(
			propertyName,
			propertySymbol.Type,
			typeSymbol,
			"Microsoft.Maui.Controls.BindingMode.OneWay",
			"null",
			"null",
			"null",
			"null",
			"CreateTextDefaultValue",
			string.Empty,
			true,
			string.Empty,
			hasInitializer,
			"public"
		);

		// Act
		var effectiveDefaultValueCreatorMethodName = model.EffectiveDefaultValueCreatorMethodName;

		// Assert
		Assert.Equal("CreateTextDefaultValue", effectiveDefaultValueCreatorMethodName);
	}
}