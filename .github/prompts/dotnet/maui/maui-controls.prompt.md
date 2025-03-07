# Page Life-cycle

Use `EventToCommandBehavior` from CommunityToolkit.Maui to handle page life-cycle events when using XAML.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:pageModels="clr-namespace:CommunityTemplate.PageModels"             
             xmlns:models="clr-namespace:CommunityTemplate.Models"
             xmlns:controls="clr-namespace:CommunityTemplate.Pages.Controls"
             xmlns:pullToRefresh="clr-namespace:Syncfusion.Maui.Toolkit.PullToRefresh;assembly=Syncfusion.Maui.Toolkit"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             x:Class="CommunityTemplate.Pages.MainPage"
             x:DataType="pageModels:MainPageModel"
             Title="{Binding Today}">

    <ContentPage.Behaviors>
        <toolkit:EventToCommandBehavior
                EventName="NavigatedTo"
                Command="{Binding NavigatedToCommand}" />
        <toolkit:EventToCommandBehavior
                EventName="NavigatedFrom"
                Command="{Binding NavigatedFromCommand}" />
        <toolkit:EventToCommandBehavior
                EventName="Appearing"                
                Command="{Binding AppearingCommand}" />
    </ContentPage.Behaviors>
```

## Control Choices

* Prefer `Grid` over other layouts to keep the visual tree flatter
* Use `VerticalStackLayout` or `HorizontalStackLayout`, not `StackLayout`
* Use `CollectionView` or a `BindableLayout`, not `ListView` or `TableView`
* Use `Border`, not `Frame`
* Declare `ColumnDefinitions` and `RowDefinitions` in-line like `<Grid RowDefinitions="*,*,40">`