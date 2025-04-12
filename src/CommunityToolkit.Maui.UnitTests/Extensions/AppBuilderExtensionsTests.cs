using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

#pragma warning disable CA1416
public class AppBuilderExtensionsTests : BaseTest
{
	[Fact]
	public void ConfirmOptionsDefaultValue()
	{
		// Arrange
		bool isAndroidDialogFragmentServiceInitialized = false;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;

		// Assert
		Assert.True(Core.Options.ShouldUseStatusBarBehaviorOnAndroidModalPage);
		Assert.False(Options.ShouldEnableSnackbarOnWindows);
		Assert.False(Options.ShouldSuppressExceptionsInAnimations);
		Assert.False(Options.ShouldSuppressExceptionsInBehaviors);
		Assert.False(Options.ShouldSuppressExceptionsInConverters);
		Assert.False(isAndroidDialogFragmentServiceInitialized);

		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;

		void HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted(object? sender, EventArgs e)
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;
			isAndroidDialogFragmentServiceInitialized = true;
		}
	}

	[Fact]
	public void ConfirmDefaultValueRemainWhenOptionsNull()
	{
		// Arrange
		var builder = MauiApp.CreateBuilder();
		bool isAndroidDialogFragmentServiceInitialized = false;

		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;

		// Act
		builder.UseMauiCommunityToolkit(null);

		// Assert
		Assert.True(Core.Options.ShouldUseStatusBarBehaviorOnAndroidModalPage);
		Assert.False(Options.ShouldEnableSnackbarOnWindows);
		Assert.False(Options.ShouldSuppressExceptionsInAnimations);
		Assert.False(Options.ShouldSuppressExceptionsInBehaviors);
		Assert.False(Options.ShouldSuppressExceptionsInConverters);
		Assert.True(isAndroidDialogFragmentServiceInitialized);

		void HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted(object? sender, EventArgs e)
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;
			isAndroidDialogFragmentServiceInitialized = true;
		}
	}


	[Fact]
	public void UseMauiCommunityToolkit_ShouldRegisterServices()
	{
		// Arrange
		var builder = MauiApp.CreateBuilder();
		bool isAndroidDialogFragmentServiceInitialized = false;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;

		// Act
		builder.UseMauiCommunityToolkit();

		// Assert
		var serviceProvider = builder.Services.BuildServiceProvider();
		Assert.NotNull(serviceProvider.GetService<IPopupService>());
		Assert.True(isAndroidDialogFragmentServiceInitialized);

		void HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted(object? sender, EventArgs e)
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;
			isAndroidDialogFragmentServiceInitialized = true;
		}
	}

	[Fact]
	public void UseMauiCommunityToolkit_ShouldAssignValues()
	{
		// Arrange
		var builder = MauiApp.CreateBuilder();
		bool isAndroidDialogFragmentServiceInitialized = false;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;

		// Act
		builder.UseMauiCommunityToolkit(options =>
		{
			options.SetShouldEnableSnackbarOnWindows(!Options.ShouldEnableSnackbarOnWindows);
			options.SetShouldSuppressExceptionsInAnimations(!Options.ShouldSuppressExceptionsInAnimations);
			options.SetShouldSuppressExceptionsInBehaviors(!Options.ShouldSuppressExceptionsInBehaviors);
			options.SetShouldSuppressExceptionsInConverters(!Options.ShouldSuppressExceptionsInConverters);
			options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(!Core.Options.ShouldUseStatusBarBehaviorOnAndroidModalPage);
		});

		// Assert
		Assert.False(Core.Options.ShouldUseStatusBarBehaviorOnAndroidModalPage);
		Assert.True(Options.ShouldEnableSnackbarOnWindows);
		Assert.True(Options.ShouldSuppressExceptionsInAnimations);
		Assert.True(Options.ShouldSuppressExceptionsInBehaviors);
		Assert.True(Options.ShouldSuppressExceptionsInConverters);
		Assert.False(isAndroidDialogFragmentServiceInitialized);

		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;

		void HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted(object? sender, EventArgs e)
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= HandleShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted;
			isAndroidDialogFragmentServiceInitialized = true;
		}
	}
}
#pragma warning restore CA1416