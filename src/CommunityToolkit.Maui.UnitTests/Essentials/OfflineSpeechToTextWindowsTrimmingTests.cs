using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

// Core builds its Windows target framework only on Windows, so these tests read its Windows source and System.Speech's Windows assembly as
// metadata: a System.Speech update with a new [ComImport] interface fails here instead of crashing fully trimmed Windows apps.
public partial class OfflineSpeechToTextWindowsTrimmingTests
{
	const string systemSpeechPackageFolderKey = "SystemSpeechPackageFolder";
	const string coreProjectFolderKey = "CoreProjectFolder";
	const string engineFactorySignature = "static SpeechRecognitionEngine CreateSpeechRecognitionEngine(";

	static readonly string windowsImplementationPath = Path.Combine(GetMetadataValue(coreProjectFolderKey), "Essentials", "SpeechToText", "OfflineSpeechToTextImplementation.windows.cs");

	[Fact]
	public void EngineFactoryKeepsEveryComImportInterfaceOfSystemSpeechWhole()
	{
		var keptTypeNames = GetTypeNamesKeptByTheEngineFactory();
		var comImportInterfaceNames = GetComImportInterfaceNames(GetSystemSpeechWindowsAssemblyPath());

		Assert.NotEmpty(comImportInterfaceNames);
		Assert.Empty(comImportInterfaceNames.Except(keptTypeNames, StringComparer.Ordinal));
		Assert.Empty(keptTypeNames.Except(comImportInterfaceNames, StringComparer.Ordinal));
	}

	// DynamicallyAccessedMemberTypes.All also keeps the members of base interfaces, so an interface of another assembly that System.Speech
	// implements for native callers (IStream) stays whole when it is a base of a kept interface.
	[Fact]
	public void EveryComInterfaceThatSystemSpeechTakesFromAnotherAssemblyIsABaseOfAComImportInterface()
	{
		using var stream = File.OpenRead(GetSystemSpeechWindowsAssemblyPath());
		using var peReader = new PEReader(stream);
		var metadata = peReader.GetMetadataReader();

		var externalInterfacesOfComImportInterfaces = new HashSet<string>(StringComparer.Ordinal);
		var externalComInterfacesOfClasses = new HashSet<string>(StringComparer.Ordinal);
		foreach (var typeHandle in metadata.TypeDefinitions)
		{
			var type = metadata.GetTypeDefinition(typeHandle);
			foreach (var implementationHandle in type.GetInterfaceImplementations())
			{
				var interfaceHandle = metadata.GetInterfaceImplementation(implementationHandle).Interface;
				if (interfaceHandle.Kind is not HandleKind.TypeReference)
				{
					continue;
				}

				var interfaceName = GetTypeReferenceName(metadata, (TypeReferenceHandle)interfaceHandle);
				if (IsComImportInterface(type))
				{
					externalInterfacesOfComImportInterfaces.Add(interfaceName);
				}
				else if (interfaceName.StartsWith("System.Runtime.InteropServices.ComTypes.", StringComparison.Ordinal))
				{
					externalComInterfacesOfClasses.Add(interfaceName);
				}
			}
		}

		Assert.Contains("System.Runtime.InteropServices.ComTypes.IStream", externalComInterfacesOfClasses);
		Assert.Empty(externalComInterfacesOfClasses.Except(externalInterfacesOfComImportInterfaces, StringComparer.Ordinal));
	}

	// Every engine must come from the factory: the trimmer applies its DynamicDependency attributes only when it keeps the factory.
	[Fact]
	public void CoreCreatesSpeechRecognitionEngineOnlyInTheEngineFactory()
	{
		var coreProjectFolder = GetMetadataValue(coreProjectFolderKey);
		var windowsOnlySources = Directory.EnumerateFiles(coreProjectFolder, "*.cs", SearchOption.AllDirectories)
			.Where(path => IsWindowsOnlySource(Path.GetRelativePath(coreProjectFolder, path)))
			.ToList();

		Assert.Contains(windowsImplementationPath, windowsOnlySources);
		foreach (var path in windowsOnlySources)
		{
			Assert.DoesNotContain("new SpeechRecognitionEngine(", File.ReadAllText(path), StringComparison.Ordinal);
		}

		var source = File.ReadAllText(windowsImplementationPath);
		Assert.Contains(engineFactorySignature + "CultureInfo culture) => new(culture);", source, StringComparison.Ordinal);
		Assert.Contains("offlineSpeechRecognizer = CreateSpeechRecognitionEngine(options.Culture);", source, StringComparison.Ordinal);
	}

	// The Turkish and Azerbaijani rows fail if the match follows the culture's casing, where "WINDOWS" lower-cases to "wındows"
	[Theory]
	[InlineData("en-US", "Essentials/SpeechToText/OfflineSpeechToTextImplementation.windows.cs", true)]
	[InlineData("en-US", "Essentials/windows/SpeechHelpers.cs", true)]
	[InlineData("en-US", "Platform/Windows/SpeechHelpers.shared.cs", true)]
	[InlineData("en-US", "Essentials/SpeechToText/OfflineSpeechToTextImplementation.shared.cs", false)]
	[InlineData("en-US", "Essentials/SpeechToText/OfflineSpeechToTextImplementation.android.cs", false)]
	[InlineData("en-US", "Essentials/windowsSpeech/SpeechHelpers.cs", false)]
	[InlineData("en-US", "obj/Release/net11.0-windows10.0.19041.0/windows/Generated.cs", false)]
	[InlineData("en-US", "bin/Release/Generated.windows.cs", false)]
	[InlineData("tr-TR", "Essentials/WINDOWS/SpeechHelpers.cs", true)]
	[InlineData("tr-TR", "Essentials/SpeechToText/OfflineSpeechToTextImplementation.WINDOWS.CS", true)]
	[InlineData("tr-TR", "OBJ/windows/Generated.cs", false)]
	[InlineData("az-Latn-AZ", "Essentials/WINDOWS/SpeechHelpers.cs", true)]
	[InlineData("az-Latn-AZ", "BIN/Generated.windows.cs", false)]
	public void WindowsOnlySourceMatchesWhatCoreCompilesOnlyForWindows(string cultureName, string relativePath, bool isWindowsOnly)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(cultureName);
			CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

			Assert.Equal(isWindowsOnly, IsWindowsOnlySource(relativePath.Replace('/', Path.DirectorySeparatorChar)));
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[Fact]
	public void InspectedSystemSpeechIsTheVersionCoreReferences()
	{
		var coreProject = XDocument.Load(Path.Combine(GetMetadataValue(coreProjectFolderKey), "CommunityToolkit.Maui.Core.csproj"));
		var systemSpeechReference = Assert.Single(coreProject.Descendants("PackageReference"), reference => string.Equals((string?)reference.Attribute("Include"), "System.Speech", StringComparison.Ordinal));

		// GeneratePathProperty points at <global packages folder>/system.speech/<version>, which NuGet writes in lower case
		var inspectedVersion = Path.GetFileName(GetMetadataValue(systemSpeechPackageFolderKey).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

		Assert.Equal(((string?)systemSpeechReference.Attribute("Version"))?.ToLowerInvariant(), inspectedVersion.ToLowerInvariant());
	}

	[GeneratedRegex("""^\s*\[DynamicDependency\(DynamicallyAccessedMemberTypes\.All, "(?<type>[^"]+)", "System\.Speech"\)\]\s*$""", RegexOptions.CultureInvariant)]
	private static partial Regex SystemSpeechDynamicDependencyRegex();

	static HashSet<string> GetTypeNamesKeptByTheEngineFactory()
	{
		var lines = File.ReadAllLines(windowsImplementationPath);
		var factoryLine = Array.FindIndex(lines, line => line.TrimStart().StartsWith(engineFactorySignature, StringComparison.Ordinal));
		Assert.True(factoryLine > 0, $"{windowsImplementationPath} has no '{engineFactorySignature}'");

		var keptTypeNames = new HashSet<string>(StringComparer.Ordinal);
		for (var line = factoryLine - 1; line >= 0; line--)
		{
			var match = SystemSpeechDynamicDependencyRegex().Match(lines[line]);
			if (!match.Success)
			{
				break;
			}

			Assert.True(keptTypeNames.Add(match.Groups["type"].Value), $"{match.Groups["type"].Value} is named twice");
		}

		return keptTypeNames;
	}

	static HashSet<string> GetComImportInterfaceNames(string assemblyPath)
	{
		using var stream = File.OpenRead(assemblyPath);
		using var peReader = new PEReader(stream);
		var metadata = peReader.GetMetadataReader();

		return metadata.TypeDefinitions
			.Where(handle => IsComImportInterface(metadata.GetTypeDefinition(handle)))
			.Select(handle => GetDocumentationIdTypeName(metadata, handle))
			.ToHashSet(StringComparer.Ordinal);
	}

	static bool IsComImportInterface(TypeDefinition type) =>
		(type.Attributes & TypeAttributes.Interface) is not 0 && (type.Attributes & TypeAttributes.Import) is not 0;

	// DynamicDependency names types in documentation ID format, which separates a nested type from its declaring type with '.'
	static string GetDocumentationIdTypeName(MetadataReader metadata, TypeDefinitionHandle handle)
	{
		var type = metadata.GetTypeDefinition(handle);
		var declaringType = type.GetDeclaringType();
		if (!declaringType.IsNil)
		{
			return $"{GetDocumentationIdTypeName(metadata, declaringType)}.{metadata.GetString(type.Name)}";
		}

		var typeNamespace = metadata.GetString(type.Namespace);
		return typeNamespace.Length is 0 ? metadata.GetString(type.Name) : $"{typeNamespace}.{metadata.GetString(type.Name)}";
	}

	static string GetTypeReferenceName(MetadataReader metadata, TypeReferenceHandle handle)
	{
		var reference = metadata.GetTypeReference(handle);
		return $"{metadata.GetString(reference.Namespace)}.{metadata.GetString(reference.Name)}";
	}

	// NuGet gives a net11.0-windows project the runtimes/win/lib folder of the newest framework that is not newer than its own
	static string GetSystemSpeechWindowsAssemblyPath()
	{
		var targetFramework = new FrameworkName(AppContext.TargetFrameworkName ?? throw new InvalidOperationException("No target framework name")).Version;
		var windowsLibFolder = Path.Combine(GetMetadataValue(systemSpeechPackageFolderKey), "runtimes", "win", "lib");

		var nearestFolder = Directory.EnumerateDirectories(windowsLibFolder, "net*")
			.Select(folder => (Folder: folder, Version: Version.TryParse(Path.GetFileName(folder)[3..], out var version) ? version : null))
			.Where(candidate => candidate.Version is not null && candidate.Version <= targetFramework)
			.OrderByDescending(candidate => candidate.Version)
			.Select(candidate => candidate.Folder)
			.FirstOrDefault() ?? throw new DirectoryNotFoundException($"{windowsLibFolder} has no folder for {targetFramework} or older");

		return Path.Combine(nearestFolder, "System.Speech.dll");
	}

	// Directory.Build.targets compiles *.windows.cs files and every .cs file in a windows folder only for Windows
	static bool IsWindowsOnlySource(string pathRelativeToCore)
	{
		var segments = pathRelativeToCore.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
		if (segments.Any(segment => IsSegment(segment, "bin") || IsSegment(segment, "obj")))
		{
			return false;
		}

		return pathRelativeToCore.EndsWith(".windows.cs", StringComparison.OrdinalIgnoreCase)
			|| segments[..^1].Any(segment => IsSegment(segment, "windows"));

		static bool IsSegment(string segment, string folderName) => string.Equals(segment, folderName, StringComparison.OrdinalIgnoreCase);
	}

	static string GetMetadataValue(string key) =>
		typeof(OfflineSpeechToTextWindowsTrimmingTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
			.SingleOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))?.Value
		?? throw new InvalidOperationException($"CommunityToolkit.Maui.UnitTests.csproj sets no AssemblyMetadata '{key}'");
}