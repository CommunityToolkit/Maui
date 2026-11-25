using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class FileSaverTests : BaseTest
{
	[Fact]
	public void FileSaverTestsSetDefaultUpdatesInstance()
	{
		var fileSaverImplementationMock = new FileSaverImplementationMock();
		FileSaver.SetDefault(fileSaverImplementationMock);
		var fileSaver = FileSaver.Default;
		fileSaver.Should().BeSameAs(fileSaverImplementationMock);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveAsync("fileName", Stream.Null, TestContext.Current.CancellationToken);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncWithInitialPathFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveAsync("initial path", "fileName", Stream.Null, TestContext.Current.CancellationToken);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncWithNullInitialPathReturnsArgumentNullException()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var result = await FileSaver.SaveAsync(null, "fileName", Stream.Null, TestContext.Current.CancellationToken);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<ArgumentNullException>()
			.Which.ParamName.Should().Be("initialPath");
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncProgressFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveAsync("fileName", Stream.Null, new Progress<double>(), TestContext.Current.CancellationToken);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncProgressWithInitialPathFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveAsync("initial path", "fileName", Stream.Null, new Progress<double>(), TestContext.Current.CancellationToken);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncProgressWithNullInitialPathReturnsArgumentNullException()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var result = await FileSaver.SaveAsync(null, "fileName", Stream.Null, new Progress<double>(), TestContext.Current.CancellationToken);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<ArgumentNullException>()
			.Which.ParamName.Should().Be("initialPath");
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
	}

	// A null file name, which an app passes on when it asks for the name with a prompt that the user cancels, is an ArgumentNullException
	// in every overload, never the exception of the platform code that would have used it
	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public async Task SaveAsyncWithNullFileNameReturnsArgumentNullException(int overload)
	{
		FileSaver.SetDefault(new FileSaverImplementation());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var result = await SaveAsync(overload, null, Stream.Null, TestContext.Current.CancellationToken);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<ArgumentNullException>()
			.Which.ParamName.Should().Be("fileName");
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public async Task SaveAsyncWithNullStreamReturnsArgumentNullException(int overload)
	{
		FileSaver.SetDefault(new FileSaverImplementation());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var result = await SaveAsync(overload, "fileName", null, TestContext.Current.CancellationToken);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<ArgumentNullException>()
			.Which.ParamName.Should().Be("stream");
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<ArgumentNullException>(result.EnsureSuccess);
	}

	static Task<FileSaverResult> SaveAsync(int overload, string fileName, Stream stream, CancellationToken token) => overload switch
	{
		0 => FileSaver.SaveAsync("initial path", fileName, stream, token),
		1 => FileSaver.SaveAsync(fileName, stream, token),
		2 => FileSaver.SaveAsync("initial path", fileName, stream, new Progress<double>(), token),
		3 => FileSaver.SaveAsync(fileName, stream, new Progress<double>(), token),
		_ => throw new ArgumentOutOfRangeException(nameof(overload), overload, "There are four overloads.")
	};
}