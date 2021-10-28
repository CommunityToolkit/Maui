using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Essentials;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
	protected bool SetProperty<T>(ref T backingStore, T value, Action? onChanged = null, [CallerMemberName] string propertyName = "")
	{
		if (EqualityComparer<T>.Default.Equals(backingStore, value))
			return false;

		backingStore = value;

		onChanged?.Invoke();

		MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(propertyName));

		return true;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}