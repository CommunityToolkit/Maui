using System.Reflection;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockApplication : Application, IPlatformApplication
{
	public new Application? Current = null;
	public IServiceProvider Services { get; }
	public IApplication Application => this;

#pragma warning disable CS0612 // Type or member is obsolete
	public MockApplication(IServiceProvider serviceProvider)
#pragma warning restore CS0612 // Type or member is obsolete
	{
		Services = serviceProvider;

		InitializeSystemResources(new MockResourcesProvider());
	}
#pragma warning disable CS0612 // Type or member is obsolete
	void InitializeSystemResources(ISystemResourcesProvider resourcesProvider)
#pragma warning restore CS0612 // Type or member is obsolete
	{
		const string privateFieldName = "_systemResources";

		if (typeof(Application).GetField(privateFieldName, BindingFlags.NonPublic | BindingFlags.Instance) is not FieldInfo systemResourcesFieldInfo)
		{
			throw new InvalidOperationException($"Unable to access {privateFieldName}");
		}

		// .NET MAUI's SystemResources initialization: https://github.com/dotnet/maui/blob/79695fbb7ba6517a334c795ecf0a1d6358ef309a/src/Controls/src/Core/Application/Application.cs#L42-L49
		systemResourcesFieldInfo.SetValue(this, new Lazy<IResourceDictionary>(() =>
		{
			var systemResources = resourcesProvider.GetSystemResources();
			systemResources.ValuesChanged += OnParentResourcesChanged;
			return systemResources;
		}));

	}
}

// Inspired by https://github.com/dotnet/maui/blob/main/src/Controls/tests/Core.UnitTests/TestClasses/ApplicationHandlerStub.cs
class ApplicationHandlerStub() : ElementHandler<IApplication, object>(Mapper)
{
	public static IPropertyMapper<IApplication, ApplicationHandlerStub> Mapper = new PropertyMapper<IApplication, ApplicationHandlerStub>(ElementMapper);

	protected override object CreatePlatformElement()
	{
		return new object();
	}
}