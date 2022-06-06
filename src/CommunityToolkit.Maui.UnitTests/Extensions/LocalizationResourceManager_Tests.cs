using System.Globalization;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

[TestCaseOrderer(" CommunityToolkit.Maui.UnitTests.PriorityOrderer", "CommunityToolkit.Maui.UnitTests")]
public class LocalizationResourceManager_Tests
{
	[Fact, TestPriority(0)]
	void GetResource_ResourceManagerNotInitialized_ThrowException()
	{
		FluentActions.Invoking(() => LocalizationResourceManager.Current["Resource"])
			.Should()
			.Throw<NullReferenceException>()
			.WithMessage("Call Init method first");
	}

	[Fact, TestPriority(1)]
	void GetResource_ResourceDoesNotExist_GetEmptyArray()
	{
		LocalizationResourceManager.Current.Init(new MockResourceManager());
		LocalizationResourceManager.Current[""].Should().Be(Array.Empty<byte>());
	}

	[Fact, TestPriority(2)]
	void GetResource_ResourceExists_GetValue()
	{
		LocalizationResourceManager.Current.Init(new MockResourceManager(), new CultureInfo("en-US"));
		LocalizationResourceManager.Current["Resource"].Should().Be("English (United States)");
	}

	[Fact, TestPriority(3)]
	void GetResource_ResourceExistsAndSetCulture_GetValue()
	{
		LocalizationResourceManager.Current.Init(new MockResourceManager());
		LocalizationResourceManager.Current["Resource"].Should().Be("English (United States)");
		LocalizationResourceManager.Current.CurrentCulture = new CultureInfo("uk-UA");
		LocalizationResourceManager.Current.GetValue("Resource").Should().Be("Ukrainian (Ukraine)");
	}

	[Fact, TestPriority(4)]
	void GetResourceWithPredefinedCulture_ResourceExists_GetValue()
	{
		LocalizationResourceManager.Current.Init(new MockResourceManager(), new CultureInfo("uk-UA"));
		LocalizationResourceManager.Current["Resource"].Should().Be("Ukrainian (Ukraine)");
	}
}