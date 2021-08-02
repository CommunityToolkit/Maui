using MCT.Sample.Pages;
using Application = Microsoft.Maui.Controls.Application;

namespace MCT.Sample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new BaseNavigationPage(new MainPage());
        }
    }
}
