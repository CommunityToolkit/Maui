using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class FolderPickerTests : BaseTest
{
	[Fact]
	public void FolderPickerSetDefaultUpdatesInstance()
	{
		var folderPickerMock = new FolderPickerImplementationMock();
		FolderPicker.SetDefault(folderPickerMock);
		var folderPickerInstance = FolderPicker.Default;
		folderPickerInstance.Should().BeSameAs(folderPickerMock);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PickAsyncFailsOnNet()
	{
		FolderPicker.SetDefault(new FolderPickerImplementation());
		var result = await FolderPicker.PickAsync(TestContext.Current.CancellationToken);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.Folder.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PickAsyncWithInitialPathFailsOnNet()
	{
		FolderPicker.SetDefault(new FolderPickerImplementation());
		var result = await FolderPicker.PickAsync("initial path", TestContext.Current.CancellationToken);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.Folder.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PickAsyncWithNullInitialPathReturnsArgumentNullException()
	{
		FolderPicker.SetDefault(new FolderPickerImplementation());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var result = await FolderPicker.PickAsync(null, TestContext.Current.CancellationToken);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<ArgumentNullException>()
			.Which.ParamName.Should().Be("initialPath");
		result.Folder.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		result.IsCancelled.Should().BeFalse();
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ImplementationPickAsyncWithNullInitialPathReturnsArgumentNullException()
	{
		// The implementation itself, as an app calls it when it gets an IFolderPicker from its services
		IFolderPicker folderPicker = new FolderPickerImplementation();
		var result = await folderPicker.PickAsync(null!, TestContext.Current.CancellationToken);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<ArgumentNullException>()
			.Which.ParamName.Should().Be("initialPath");
		result.Folder.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PickAsyncWithNullInitialPathAndCanceledTokenReturnsCanceledResult()
	{
		// As for FileSaver, a canceled token is reported before the initial path is looked at
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();
		var result = await new FolderPickerImplementation().PickAsync(null!, cancellationTokenSource.Token);
		result.Exception.Should().BeAssignableTo<OperationCanceledException>();
		result.Folder.Should().BeNull();
		result.IsCancelled.Should().BeTrue();
		result.IsSuccessful.Should().BeFalse();
	}

	// Only null is rejected: no initial path is the overload without one, and an empty path is left to the platform (Apple and Windows
	// take it for no initial path)
	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData("")]
	[InlineData(" ")]
	public async Task PickAsyncWithEmptyInitialPathIsNotRejected(string initialPath)
	{
		FolderPicker.SetDefault(new FolderPickerImplementation());
		var result = await FolderPicker.PickAsync(initialPath, TestContext.Current.CancellationToken);
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.Folder.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}
}