using CommunityToolkit.Maui.Sample.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System.Diagnostics;
using System.Windows.Input;
using System;

namespace CommunityToolkit.Maui.Sample.Pages
{
    public class BasePage : ContentPage
    {
        public BasePage()
        {
            NavigateCommand = new Command(async (parameter) =>
            {
                if (parameter != null)
                {
                    await Navigation.PushAsync(PreparePage(parameter));
                }
            });
        }

        protected override void OnAppearing()
        {
            Debug.WriteLine($"OnAppearing: {this}");
        }

        protected override void OnDisappearing()
        {
            Debug.WriteLine($"OnDisappearing: {this}");
        }

        public ICommand NavigateCommand { get; }

        Page PreparePage(object parameter)
        {
            SectionModel model = parameter as SectionModel;

            if (model == null)
                return null;

            var page = (Page)Activator.CreateInstance(model.Type);
            page.Title = model.Title;

            return page;
        }
    }
}
