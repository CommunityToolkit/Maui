using CommunityToolkit.Maui.Exceptions;
using CommunityToolkit.Maui.Helpers;
using NUnit.Framework;
using System;

namespace CommunityToolkit.Maui.UnitTests.Helpers.WeakEventManagerTests
{
	public class WeakEventManager_EventHandlerT_Tests : BaseWeakEventManagerTests
	{
		[Test]
		public void WeakEventManagerTEventArgs_HandleEvent_ValidImplementation()
		{
			// Arrange
			TestStringEvent += HandleTestEvent;

			const string stringEventArg = "Test";
			var didEventFire = false;

			void HandleTestEvent(object? sender, string? e)
			{
				if (sender == null || e == null)
					throw new ArgumentNullException(nameof(sender));

				Assert.IsNotNull(sender);
				Assert.AreEqual(GetType(), sender.GetType());

				Assert.IsNotNull(e);
				Assert.AreEqual(stringEventArg, e);

				didEventFire = true;
				TestStringEvent -= HandleTestEvent;
			}

			// Act
			TestStringWeakEventManager.RaiseEvent(this, stringEventArg, nameof(TestStringEvent));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManageTEventArgs_HandleEvent_NullSender()
		{
			// Arrange
			TestStringEvent += HandleTestEvent;

			const string stringEventArg = "Test";

			var didEventFire = false;

			void HandleTestEvent(object? sender, string e)
			{
				Assert.Null(sender);

				Assert.IsNotNull(e);
				Assert.AreEqual(stringEventArg, e);

				didEventFire = true;
				TestStringEvent -= HandleTestEvent;
			}

            // Act
            TestStringWeakEventManager.RaiseEvent(null, stringEventArg, nameof(TestStringEvent));

            // Assert
            Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManagerTEventArgs_HandleEvent_NullEventArgs()
		{
			// Arrange
			TestStringEvent += HandleTestEvent;
			var didEventFire = false;

			void HandleTestEvent(object? sender, string e)
			{
				if (sender == null)
					throw new ArgumentNullException(nameof(sender));

				Assert.IsNotNull(sender);
				Assert.AreEqual(GetType(), sender.GetType());

				Assert.Null(e);

				didEventFire = true;
				TestStringEvent -= HandleTestEvent;
			}

			// Act
			TestStringWeakEventManager.RaiseEvent(this, null!, nameof(TestStringEvent));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManagerTEventArgs_HandleEvent_InvalidHandleEvent()
		{
			// Arrange
			TestStringEvent += HandleTestEvent;

			var didEventFire = false;

			void HandleTestEvent(object? sender, string e) => didEventFire = true;

			// Act
			TestStringWeakEventManager.RaiseEvent(this, "Test", nameof(TestEvent));

			// Assert
			Assert.False(didEventFire);
			TestStringEvent -= HandleTestEvent;
		}

		[Test]
		public void WeakEventManager_NullEventManager()
		{
			// Arrange
			WeakEventManager<string>? unassignedEventManager = null;

			// Act

			// Assert
#pragma warning disable CS8602 //Dereference of a possible null reference
			Assert.Throws<NullReferenceException>(() => unassignedEventManager.RaiseEvent(this, string.Empty, nameof(TestEvent)));
#pragma warning restore CS8602
		}

		[Test]
		public void WeakEventManagerTEventArgs_UnassignedEventManager()
		{
			// Arrange
			var unassignedEventManager = new WeakEventManager<string>();
			var didEventFire = false;

			TestStringEvent += HandleTestEvent;
			void HandleTestEvent(object? sender, string e) => didEventFire = true;

			// Act
			unassignedEventManager.RaiseEvent(null, null!, nameof(TestStringEvent));

			// Assert
			Assert.False(didEventFire);
			TestStringEvent -= HandleTestEvent;
		}

		[Test]
		public void WeakEventManagerTEventArgs_UnassignedEvent()
		{
			// Arrange
			var didEventFire = false;

			TestStringEvent += HandleTestEvent;
			TestStringEvent -= HandleTestEvent;
			void HandleTestEvent(object? sender, string e) => didEventFire = true;

			// Act
			TestStringWeakEventManager.RaiseEvent(this, "Test", nameof(TestStringEvent));

			// Assert
			Assert.False(didEventFire);
		}

		[Test]
		public void WeakEventManagerT_AddEventHandler_NullHandler()
		{
			// Arrange

			// Act

			// Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.AddEventHandler((EventHandler<string>?)null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		}

		[Test]
		public void WeakEventManagerT_AddEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.AddEventHandler(s => { var temp = s; }, null!));
		}

		[Test]
		public void WeakEventManagerT_AddEventHandler_EmptyEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.AddEventHandler(s => { var temp = s; }, string.Empty));
		}

		[Test]
		public void WeakEventManagerT_AddEventHandler_WhiteSpaceEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.AddEventHandler(s => { var temp = s; }, " "));
		}

		[Test]
		public void WeakEventManagerT_RemoveEventHandler_NullHandler()
		{
			// Arrange

			// Act

			// Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.RemoveEventHandler((EventHandler<string>?)null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		}

		[Test]
		public void WeakEventManagerT_RemoveEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.AddEventHandler(s => { var temp = s; }, null!));
		}

		[Test]
		public void WeakEventManagerT_RemoveEventHandler_EmptyEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.AddEventHandler(s => { var temp = s; }, string.Empty));
		}

		[Test]
		public void WeakEventManagerT_RemoveEventHandler_WhiteSpaceEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestStringWeakEventManager.AddEventHandler(s => { var temp = s; }, string.Empty));
		}

		[Test]
		public void WeakEventManagerT_HandleEvent_InvalidHandleEvent()
		{
			// Arrange
			TestStringEvent += HandleTestStringEvent;
			var didEventFire = false;

			void HandleTestStringEvent(object? sender, string e) => didEventFire = true;

			// Act

			// Assert
			Assert.Throws<InvalidHandleEventException>(() => TestStringWeakEventManager.RaiseEvent(string.Empty, nameof(TestStringEvent)));
			Assert.False(didEventFire);
			TestStringEvent -= HandleTestStringEvent;
		}
	}
}
