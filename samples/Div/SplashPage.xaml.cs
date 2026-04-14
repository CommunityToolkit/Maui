namespace DivStart;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
        Loaded += async (object? sender, EventArgs e) =>
        {
            InfoBox.Text = "";
            await Task.Delay(1000);
            for (int i = 0; i < 5; i++)
            {
                InfoBox.Text += Environment.NewLine + $"Line {i}";
                await Task.Delay(500);
            }
            Application.SetStatusBar();
            await Shell.Current.GoToAsync(@"//FunctionalPage");
        };
    }
}