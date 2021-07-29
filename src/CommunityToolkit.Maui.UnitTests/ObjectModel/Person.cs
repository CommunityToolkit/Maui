using CommunityToolkit.Maui.ObjectModel;
using System;

namespace CommunityToolkit.Maui.UnitTests.ObjectModel
{
    class Person : ObservableObject
	{
		string firstName = string.Empty;
		string lastName = string.Empty;

		public Action? Changed { get; set; }

		public Action? Changing { get; set; }

		public Func<string, string, bool>? Validate { get; set; }

		public string FirstName
		{
			get => firstName;
			set => SetProperty(ref firstName, value, onChanged: Changed, onChanging: Changing, validateValue: Validate);
		}

		public string LastName
		{
			get => lastName;
			set => SetProperty(ref lastName, value, onChanged: Changed, onChanging: Changing, validateValue: Validate);
		}

		public string Group => FirstName[0].ToString().ToUpperInvariant();
	}
}
