using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.UnitTests.ObjectModel
{
    public class Person : ObservableObject
	{
		string firstName = string.Empty;
		string lastName = string.Empty;
		int id = 0;

		public int Id
		{
			get => id;
			set => SetProperty(ref id, value);
		}

		public string FirstName
		{
			get => firstName;
			set => SetProperty(ref firstName, value);
		}

		public string LastName
		{
			get => lastName;
			set => SetProperty(ref lastName, value);
		}

		public string Group => FirstName[0].ToString().ToUpperInvariant();
	}
    
}

