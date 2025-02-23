using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Views;

sealed partial class PopupContainer<T> : PopupContainer
{
    readonly TaskCompletionSource<PopupResult<T>> taskCompletionSource;

    public PopupContainer(View view, PopupOptions popupOptions, TaskCompletionSource<PopupResult<T>> taskCompletionSource)
        : this(view as Popup<T> ?? CreatePopupFromView<Popup<T>>(view), popupOptions, taskCompletionSource)
    {
    }

    public PopupContainer(Popup<T> popup, PopupOptions popupOptions, TaskCompletionSource<PopupResult<T>> taskCompletionSource)
        : base(popup, popupOptions, null)
    {
        this.taskCompletionSource = taskCompletionSource;

        Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
        On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
    }

    public Task Close(PopupResult<T> result, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        taskCompletionSource.SetResult(result);
        return Navigation.PopModalAsync(false).WaitAsync(token);
    }
}

partial class PopupContainer : ContentPage
{
    readonly Popup popup;
    readonly PopupOptions popupOptions;
    readonly Command tapOutsideOfPopupCommand;
    readonly TaskCompletionSource<PopupResult>? taskCompletionSource;

    public PopupContainer(View view, PopupOptions popupOptions, TaskCompletionSource<PopupResult>? taskCompletionSource)
        : this(view as Popup ?? CreatePopupFromView<Popup>(view), popupOptions, taskCompletionSource)
    {
        // Only set the content if overloaded constructor hasn't set the content already; don't override content if it already exists
        Content ??= new PopupContainerContent(view, popupOptions);
    }

    public PopupContainer(Popup popup, PopupOptions popupOptions, TaskCompletionSource<PopupResult>? taskCompletionSource)
    {
        this.popup = popup;
        this.popupOptions = popupOptions;
        this.taskCompletionSource = taskCompletionSource;

        // Only set the content if overloaded constructor hasn't set the content already; don't override content if it already exists
        Content ??= new PopupContainerContent(popup, popupOptions);
        BackgroundColor = popupOptions.BackgroundColor;

        tapOutsideOfPopupCommand = new Command(async () =>
        {
            popupOptions.OnTappingOutsideOfPopup?.Invoke();
            await Close(new PopupResult(true));
        }, () => popupOptions.CanBeDismissedByTappingOutsideOfPopup);
        
        Content.GestureRecognizers.Add(new TapGestureRecognizer { Command  = tapOutsideOfPopupCommand });
        
        this.popupOptions.PropertyChanged += HandlePopupPropertyChanged;

        this.SetBinding(BindingContextProperty, static (View x) => x.BindingContext, source: Content, mode: BindingMode.OneWay);

        Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
        On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
    }

    public Task Close(PopupResult result, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        taskCompletionSource?.SetResult(result);
        return Navigation.PopModalAsync(false).WaitAsync(token);
    }

    // Prevent the Android Back Button from dismissing the Popup if CanBeDismissedByTappingOutsideOfPopup is true
    protected override bool OnBackButtonPressed()
    {
        if (popupOptions.CanBeDismissedByTappingOutsideOfPopup)
        {
            return base.OnBackButtonPressed();
        }

        return true;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        popup.NotifyPopupIsClosed();
        base.OnNavigatedFrom(args);
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        popup.NotifyPopupIsOpened();
    }
    
    protected static T CreatePopupFromView<T>(in View view) where T : Popup, new()
    {
        return new T
        {
            Content = view
        };
    }
    
    void HandlePopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PopupOptions.CanBeDismissedByTappingOutsideOfPopupProperty))
        {
            tapOutsideOfPopupCommand.ChangeCanExecute();
        }
    }

    private protected sealed class PopupContainerContent : Grid
    {
        public PopupContainerContent(View popupContent, PopupOptions options)
        {
            BackgroundColor = null;

            Children.Add(new Border
            {
                Content = popupContent,
                Background = popupContent.Background,
                BackgroundColor = popupContent.BackgroundColor,
                VerticalOptions = options.VerticalOptions,
                HorizontalOptions = options.HorizontalOptions,
                StrokeShape = options.Shape,
                Margin = options.Margin,
                Padding = options.Padding
            });

            this.SetBinding(BindingContextProperty, static (View x) => x.BindingContext, source: popupContent, mode: BindingMode.OneWay);
        }
    }
}