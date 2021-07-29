#nullable enable
using CommunityToolkit.Maui.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.ObjectModel
{
    /// <summary>
    /// Observable object with INotifyPropertyChanged implemented using WeakEventManager
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
	{
		readonly DelegateWeakEventManager weakEventManager = new();

		/// <summary>
		/// Occurs when property changed.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => weakEventManager.AddEventHandler(value);
			remove => weakEventManager.RemoveEventHandler(value);
		}

		/// <summary>
		/// Sets the property.
		/// </summary>
		/// <returns><c>true</c>, if property was set, <c>false</c> otherwise.</returns>
		/// <param name="backingStore">Backing store.</param>
		/// <param name="value">Value.</param>
		/// <param name="validateValue">Validates value.</param>
		/// <param name="propertyName">Property name.</param>
		/// <param name="onChanged">On changed.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		protected virtual bool SetProperty<T>(
			ref T backingStore,
			T value,
			[CallerMemberName] string? propertyName = "",
			Action? onChanging = null,
			Action? onChanged = null,
			Func<T, T, bool>? validateValue = null)
		{
			// if value didn't change
			if (EqualityComparer<T>.Default.Equals(backingStore, value))
				return false;

			// if value changed but didn't validate
			if (validateValue != null && !validateValue(backingStore, value))
				return false;

			onChanging?.Invoke();
			backingStore = value;
			onChanged?.Invoke();
			OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Raises the property changed event.
		/// </summary>
		/// <param name="propertyName">Property name.</param>
		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = "") =>
			weakEventManager.RaiseEvent(this, new PropertyChangedEventArgs(propertyName), nameof(PropertyChanged));
	}
}
