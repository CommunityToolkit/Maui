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
</ContentPage>
```

## Control Choices

* Prefer `Grid` over other layouts to keep the visual tree flatter
* Use `VerticalStackLayout` or `HorizontalStackLayout`, not `StackLayout`
* Use `CollectionView` or a `BindableLayout`, not `ListView` or `TableView`
* Use `Border`, not `Frame`
* Declare `ColumnDefinitions` and `RowDefinitions` in-line like `<Grid RowDefinitions="*,*,40">`

## Common Code Styles and Implementation Templates Specific to .NET MAUI

### Using `ICommand` for Commanding
- **Good Practice**: Use `ICommand` for handling user interactions in MVVM pattern.
- **Bad Practice**: Handling user interactions directly in the code-behind.

```csharp
// Good Practice
public class MainPageViewModel
{
    public ICommand ButtonCommand { get; }

    public MainPageViewModel()
    {
        ButtonCommand = new Command(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        // Handle button click
    }
}

// Bad Practice
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Button.Clicked += OnButtonClicked;
    }

    private void OnButtonClicked(object sender, EventArgs e)
    {
        // Handle button click
    }
}
```

### Using `ObservableCollection` for Data Binding
- **Good Practice**: Use `ObservableCollection` for collections that need to notify the UI of changes.
- **Bad Practice**: Using `List` for collections that need to notify the UI of changes.

```csharp
// Good Practice
public class MainPageViewModel
{
    public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();

    public MainPageViewModel()
    {
        Items.Add("Item 1");
        Items.Add("Item 2");
    }
}

// Bad Practice
public class MainPageViewModel
{
    public List<string> Items { get; } = new List<string>();

    public MainPageViewModel()
    {
        Items.Add("Item 1");
        Items.Add("Item 2");
    }
}
```

### Using `DataTemplate` for Customizing List Items
- **Good Practice**: Use `DataTemplate` to define the appearance of items in a collection view.
- **Bad Practice**: Defining item appearance directly in the collection view.

```csharp
// Good Practice
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <StackLayout>
                <Label Text="{Binding}" />
            </StackLayout>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>

// Bad Practice
<CollectionView ItemsSource="{Binding Items}">
    <StackLayout>
        <Label Text="{Binding}" />
    </StackLayout>
</CollectionView>
```

### Using `OnAppearing` and `OnDisappearing` for Page Lifecycle Events
- **Good Practice**: Override `OnAppearing` and `OnDisappearing` to handle page lifecycle events.
- **Bad Practice**: Using event handlers for page lifecycle events.

```csharp
// Good Practice
public class MainPage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Handle appearing
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Handle disappearing
    }
}

// Bad Practice
public class MainPage : ContentPage
{
    public MainPage()
    {
        Appearing += OnPageAppearing;
        Disappearing += OnPageDisappearing;
    }

    private void OnPageAppearing(object sender, EventArgs e)
    {
        // Handle appearing
    }

    private void OnPageDisappearing(object sender, EventArgs e)
    {
        // Handle disappearing
    }
}
```

### Using Resource Dictionaries
- **Good Practice**: Use resource dictionaries to manage styles and resources.
- **Bad Practice**: Defining styles and resources directly in XAML files without using resource dictionaries.

```csharp
// Good Practice
<Application.Resources>
    <ResourceDictionary>
        <Style x:Key="ButtonStyle" TargetType="Button">
            <Setter Property="BackgroundColor" Value="Blue" />
            <Setter Property="TextColor" Value="White" />
        </Style>
    </ResourceDictionary>
</Application.Resources>

// Bad Practice
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.MainPage">
    <ContentPage.Resources>
        <Style x:Key="ButtonStyle" TargetType="Button">
            <Setter Property="BackgroundColor" Value="Blue" />
            <Setter Property="TextColor" Value="White" />
        </Style>
    </ContentPage.Resources>
</ContentPage>
```

### Using `ResourceDictionary` for Theming
- **Good Practice**: Use `ResourceDictionary` to manage themes and styles.
- **Bad Practice**: Defining themes and styles directly in XAML files without using `ResourceDictionary`.

```csharp
// Good Practice
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="LightTheme.xaml" />
            <ResourceDictionary Source="DarkTheme.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>

// Bad Practice
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.MainPage">
    <ContentPage.Resources>
        <Style x:Key="ButtonStyle" TargetType="Button">
            <Setter Property="BackgroundColor" Value="Blue" />
            <Setter Property="TextColor" Value="White" />
        </Style>
    </ContentPage.Resources>
</ContentPage>
```

### Using `Shell` for Navigation
- **Good Practice**: Use `Shell` for navigation to simplify and standardize navigation patterns.
- **Bad Practice**: Using `NavigationPage` and `TabbedPage` directly for complex navigation scenarios.

```csharp
// Good Practice
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(DetailPage), typeof(DetailPage));
    }
}

// Bad Practice
public class App : Application
{
    public App()
    {
        MainPage = new NavigationPage(new MainPage());
    }
}
```

### Using Data Binding
- **Good Practice**: Use data binding to bind UI elements to view models.
- **Bad Practice**: Directly manipulating UI elements in code-behind.

```csharp
// Good Practice
public class MainPageViewModel : INotifyPropertyChanged
{
    private string _text;
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Bad Practice
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        var entry = new Entry();
        entry.TextChanged += (s, e) => { entry.Text = "New Text"; };
    }
}
```

## LINQ Usage
- **Good Practice**: Use LINQ for concise and readable data manipulation.
- **Bad Practice**: Using loops for operations that can be done with LINQ.

```csharp
// Good Practice
var activeUsers = users.Where(u => u.IsActive).ToList();

// Bad Practice
var activeUsers = new List<User>();
foreach (var user in users)
{
    if (user.IsActive)
    {
        activeUsers.Add(user);
    }
}
```

## Using DependencyService
- **Good Practice**: Use `DependencyService` to resolve platform-specific implementations.
- **Bad Practice**: Directly referencing platform-specific code in shared code.

**Example:**
```csharp
// Good Practice
public interface IPlatformService
{
    void PerformPlatformSpecificOperation();
}

public class MainPage : ContentPage
{
    public MainPage()
    {
        var platformService = DependencyService.Get<IPlatformService>();
        platformService?.PerformPlatformSpecificOperation();
    }
}

// Bad Practice
public class MainPage : ContentPage
{
#if ANDROID
    public MainPage()
    {
        // Android specific code
    }
#elif IOS
    public MainPage()
    {
        // iOS specific code
    }
#endif
}
```
