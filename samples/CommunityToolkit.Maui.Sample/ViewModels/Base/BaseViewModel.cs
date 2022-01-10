using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
	readonly WeakEventManager propertyChangedEventManager = new();

	event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
	{
		add => propertyChangedEventManager.AddEventHandler(value);
		remove => propertyChangedEventManager.RemoveEventHandler(value);
	}

	protected bool SetProperty<T>(ref T backingStore, T value, Action? onChanged = null, [CallerMemberName] string propertyName = "")
	{
		if (EqualityComparer<T>.Default.Equals(backingStore, value))
		{
			return false;
		}

		backingStore = value;

		onChanged?.Invoke();

		MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(propertyName));

		return true;
	}

	protected void OnPropertyChanged([CallerMemberName] string propertyName = "") => propertyChangedEventManager.HandleEvent(this, new PropertyChangedEventArgs(propertyName), nameof(INotifyPropertyChanged.PropertyChanged));
}