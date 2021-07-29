#nullable enable
using CommunityToolkit.Maui.Exceptions;
using CommunityToolkit.Maui.Helpers;
using NUnit.Framework;
using System;
using System.ComponentModel;

namespace CommunityToolkit.Maui.UnitTests.Helpers.WeakEventManagerTests
{
    public class WeakEventManager_Delegate_Tests : BaseWeakEventManagerTests, INotifyPropertyChanged
	{
		readonly DelegateWeakEventManager propertyChangedWeakEventManager = new();

		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => propertyChangedWeakEventManager.AddEventHandler(value);
			remove => propertyChangedWeakEventManager.RemoveEventHandler(value);
		}

		[Test]
		public void WeakEventManagerDelegate_HandleEvent_ValidImplementation()
		{
			// Arrange
			PropertyChanged += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e)
			{
				Assert.IsNotNull(sender);
				Assert.AreEqual(GetType(), sender?.GetType());

				Assert.IsNotNull(e);

				didEventFire = true;
				PropertyChanged -= HandleDelegateTest;
			}

			// Act
			propertyChangedWeakEventManager.RaiseEvent(this, new PropertyChangedEventArgs("Test"), nameof(PropertyChanged));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManagerDelegate_HandleEvent_NullSender()
		{
			// Arrange
			PropertyChanged += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e)
			{
				Assert.Null(sender);
				Assert.IsNotNull(e);

				didEventFire = true;
				PropertyChanged -= HandleDelegateTest;
			}

            // Act
            propertyChangedWeakEventManager.RaiseEvent(null, new PropertyChangedEventArgs("Test"), nameof(PropertyChanged));

            // Assert
            Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManagerDelegate_HandleEvent_InvalidEventArgs()
		{
			// Arrange
			PropertyChanged += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e) => didEventFire = true;

			// Act

			// Assert
			Assert.Throws<ArgumentException>(() => propertyChangedWeakEventManager.RaiseEvent(this, EventArgs.Empty, nameof(PropertyChanged)));
			Assert.False(didEventFire);
			PropertyChanged -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerDelegate_HandleEvent_NullEventArgs()
		{
			// Arrange
			PropertyChanged += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e)
			{
				Assert.IsNotNull(sender);
				Assert.AreEqual(GetType(), sender?.GetType());

				Assert.Null(e);

				didEventFire = true;
				PropertyChanged -= HandleDelegateTest;
			}

			// Act
			propertyChangedWeakEventManager.RaiseEvent(this, null!, nameof(PropertyChanged));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManagerDelegate_HandleEvent_InvalidHandleEventEventName()
		{
			// Arrange
			PropertyChanged += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e) => didEventFire = true;

			// Act
			propertyChangedWeakEventManager.RaiseEvent(this, new PropertyChangedEventArgs("Test"), nameof(TestStringEvent));

			// Assert
			Assert.False(didEventFire);
			PropertyChanged -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerDelegate_HandleEvent_DynamicMethod_ValidImplementation()
		{
			// Arrange
			var dynamicMethod = new System.Reflection.Emit.DynamicMethod(string.Empty, typeof(void), new[] { typeof(object), typeof(PropertyChangedEventArgs) });
			var ilGenerator = dynamicMethod.GetILGenerator();
			ilGenerator.Emit(System.Reflection.Emit.OpCodes.Ret);

			var handler = (PropertyChangedEventHandler)dynamicMethod.CreateDelegate(typeof(PropertyChangedEventHandler));
			PropertyChanged += handler;

			// Act

			// Assert
			propertyChangedWeakEventManager.RaiseEvent(this, new PropertyChangedEventArgs("Test"), nameof(PropertyChanged));
			PropertyChanged -= handler;
		}

		[Test]
		public void WeakEventManagerDelegate_UnassignedEvent()
		{
			// Arrange
			var didEventFire = false;

			PropertyChanged += HandleDelegateTest;
			PropertyChanged -= HandleDelegateTest;
			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e) => didEventFire = true;

			// Act
			propertyChangedWeakEventManager.RaiseEvent(null, null!, nameof(PropertyChanged));

			// Assert
			Assert.False(didEventFire);
		}

		[Test]
		public void WeakEventManagerDelegate_UnassignedEventManager()
		{
			// Arrange
			var unassignedEventManager = new DelegateWeakEventManager();
			var didEventFire = false;

			PropertyChanged += HandleDelegateTest;
			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e) => didEventFire = true;

			// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			unassignedEventManager.RaiseEvent(null, null, nameof(PropertyChanged));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

			// Assert
			Assert.False(didEventFire);
			PropertyChanged -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerDelegate_HandleEvent_InvalidHandleEvent()
		{
			// Arrange
			PropertyChanged += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(object? sender, PropertyChangedEventArgs e) => didEventFire = true;

			// Act

			// Assert
			Assert.Throws<InvalidHandleEventException>(() => propertyChangedWeakEventManager.RaiseEvent(nameof(PropertyChanged)));
			Assert.False(didEventFire);
			PropertyChanged -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerDelegate_AddEventHandler_NullHandler()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.AddEventHandler(null));
		}

		[Test]
		public void WeakEventManagerDelegate_AddEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.AddEventHandler(null, null!));
		}

		[Test]
		public void WeakEventManagerDelegate_AddEventHandler_EmptyEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.AddEventHandler(null, string.Empty));
		}

		[Test]
		public void WeakEventManagerDelegate_AddEventHandler_WhitespaceEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.AddEventHandler(null, " "));
		}

		[Test]
		public void WeakEventManagerDelegate_RemoveEventHandler_NullHandler()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.RemoveEventHandler(null));
		}

		[Test]
		public void WeakEventManagerDelegate_RemoveEventHandler_NullEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.RemoveEventHandler(null, null!));
        }

        [Test]
		public void WeakEventManagerDelegate_RemoveEventHandler_EmptyEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.RemoveEventHandler(null, string.Empty));
		}

		[Test]
		public void WeakEventManagerDelegate_RemoveEventHandler_WhiteSpaceEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => propertyChangedWeakEventManager.RemoveEventHandler(null, " "));
		}
	}
}
