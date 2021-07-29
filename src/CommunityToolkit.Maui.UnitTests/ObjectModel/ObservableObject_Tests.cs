using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.UnitTests.ObjectModel
{
	public sealed class ObservableObject_Tests
	{
		[Test]
		public void OnPropertyChanged()
		{
			var person = new Person
			{
				FirstName = "James",
				LastName = "Montemagno"
			};

			PropertyChangedEventArgs? updated = null;
			person.PropertyChanged += (sender, args) =>
			{
				updated = args;
			};

			person.FirstName = "Motz";

			Assert.IsNotNull(updated);
			Assert.AreEqual(nameof(person.FirstName), updated?.PropertyName);
		}

		[Test]
		public void OnDidntChange()
		{
			var person = new Person
			{
				FirstName = "James",
				LastName = "Montemagno"
			};

			PropertyChangedEventArgs? updated = null;
			person.PropertyChanged += (sender, args) =>
			{
				updated = args;
			};

			person.FirstName = "James";

			Assert.Null(updated);
		}

		[Test]
		public void OnChangedEvent()
		{
			var person = new Person
			{
				FirstName = "James",
				LastName = "Montemagno"
			};

			var triggered = false;
			person.Changed = () =>
			{
				triggered = true;
			};

			person.FirstName = "Motz";

			Assert.IsTrue(triggered, "OnChanged didn't raise");
		}

		[Test]
		public void OnChangingEvent()
		{
			var person = new Person
			{
				FirstName = "James",
				LastName = "Montemagno"
			};

			var triggered = false;
			person.Changing = () =>
			{
				triggered = true;
			};

			person.FirstName = "Motz";

			Assert.IsTrue(triggered, "OnChanging didn't raise");
		}

		[Test]
		public void ValidateEvent()
		{
			var person = new Person
			{
				FirstName = "James",
				LastName = "Montemagno"
			};

			var contol = "Motz";
			var triggered = false;
			person.Validate = (oldValue, newValue) =>
			{
				triggered = true;
				return oldValue != newValue;
			};

			person.FirstName = contol;

			Assert.IsTrue(triggered, "ValidateValue didn't raise");
			Assert.AreEqual(person.FirstName, contol);
		}

		[Test]
		public void NotValidateEvent()
		{
			var person = new Person
			{
				FirstName = "James",
				LastName = "Montemagno"
			};

			var contol = person.FirstName;
			var triggered = false;
			person.Validate = (oldValue, newValue) =>
			{
				triggered = true;
				return false;
			};

			person.FirstName = "Motz";

			Assert.IsTrue(triggered, "ValidateValue didn't raise");
			Assert.AreEqual(person.FirstName, contol);
		}

		[Test]
		public void ValidateEventException()
		{
			var person = new Person
			{
				FirstName = "James",
				LastName = "Montemagno"
			};

			person.Validate = (oldValue, newValue) =>
			{
				throw new ArgumentOutOfRangeException();
			};

			var result = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
			{
				person.FirstName = "Motz";
				return Task.CompletedTask;
			});

			Assert.IsNotNull(result);
		}
	}
}
