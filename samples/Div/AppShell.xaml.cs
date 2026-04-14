namespace DivStart;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }
     async void OnGo(object? sender, EventArgs e)
    {
        await GoToAsync(@"//SplashPage");
    }
}