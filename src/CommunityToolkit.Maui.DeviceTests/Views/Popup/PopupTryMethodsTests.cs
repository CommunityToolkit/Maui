namespace CommunityToolkit.Maui.DeviceTests;

// The platform files show the popups in a Window of their own and navigate with .NET MAUI's own navigation.
[Category(TestCategory.Popup)]
[Collection(TestCollections.DisableParallelization)]
public partial class PopupTryMethodsTests : PopupWindowHostTestBase;