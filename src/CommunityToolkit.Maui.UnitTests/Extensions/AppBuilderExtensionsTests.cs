using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class AppBuilderExtensionsTests : BaseTest
{
    [Fact]
    public void ConfirmOptionsDefaultValue()
    {
        // Assert
        Assert.True(Core.Options.ShouldUseStatusBarBehaviorOnAndroidModalPage);
        Assert.False(Options.ShouldEnableSnackbarOnWindows);
        Assert.False(Options.ShouldSuppressExceptionsInAnimations);
        Assert.False(Options.ShouldSuppressExceptionsInBehaviors);
        Assert.False(Options.ShouldSuppressExceptionsInConverters);
    }
    
    [Fact]
    public void UseMauiCommunityToolkit_ShouldRegisterServices()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();

        // Act
#pragma warning disable CA1416
        builder.UseMauiCommunityToolkit();
#pragma warning restore CA1416

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IPopupService>());
    }
    
    [Fact]
    public void UseMauiCommunityToolkit_ShouldAssignValues()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();

        // Act
#pragma warning disable CA1416
        builder.UseMauiCommunityToolkit(options =>
        {
            options.SetShouldEnableSnackbarOnWindows(!Options.ShouldEnableSnackbarOnWindows);
            options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(!Core.Options.ShouldUseStatusBarBehaviorOnAndroidModalPage);
            options.SetShouldSuppressExceptionsInAnimations(!Options.ShouldSuppressExceptionsInAnimations);
            options.SetShouldSuppressExceptionsInBehaviors(!Options.ShouldSuppressExceptionsInBehaviors);
            options.SetShouldSuppressExceptionsInConverters(!Options.ShouldSuppressExceptionsInConverters);
        });
#pragma warning restore CA1416

        // Assert
        Assert.False(Core.Options.ShouldUseStatusBarBehaviorOnAndroidModalPage);
        Assert.True(Options.ShouldEnableSnackbarOnWindows);
        Assert.True(Options.ShouldSuppressExceptionsInAnimations);
        Assert.True(Options.ShouldSuppressExceptionsInBehaviors);
        Assert.True(Options.ShouldSuppressExceptionsInConverters);
    }
}