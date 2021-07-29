#nullable enable
using CommunityToolkit.Maui.Exceptions;
using CommunityToolkit.Maui.Helpers;
using NUnit.Framework;
using System;

namespace CommunityToolkit.Maui.UnitTests.Helpers.WeakEventManagerTests
{
	public class WeakEventManager_ActionT_Tests : BaseWeakEventManagerTests
	{
		readonly WeakEventManager<string> actionEventManager = new();

		public event Action<string> ActionEvent
		{
			add => actionEventManager.AddEventHandler(value);
			remove => actionEventManager.RemoveEventHandler(value);
		}

		[Test]
		public void WeakEventManagerActionT_HandleEvent_ValidImplementation()
		{
			// Arrange
			ActionEvent += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(string message)
			{
				Assert.IsNotNull(message);
				Assert.IsNotEmpty(message);

				didEventFire = true;
				ActionEvent -= HandleDelegateTest;
			}

			// Act
			actionEventManager.RaiseEvent("Test", nameof(ActionEvent));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManagerActionT_HandleEvent_InvalidHandleEventEventName()
		{
			// Arrange
			ActionEvent += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(string message)
			{
				Assert.IsNotNull(message);
				Assert.IsNotEmpty(message);

				didEventFire = true;
			}

			// Act
			actionEventManager.RaiseEvent("Test", nameof(TestEvent));

			// Assert
			Assert.False(didEventFire);
			ActionEvent -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerActionT_UnassignedEvent()
		{
			// Arrange
			var didEventFire = false;

			ActionEvent += HandleDelegateTest;
			ActionEvent -= HandleDelegateTest;
			void HandleDelegateTest(string message)
			{
				Assert.IsNotNull(message);
				Assert.IsNotEmpty(message);

				didEventFire = true;
			}

			// Act
			actionEventManager.RaiseEvent("Test", nameof(ActionEvent));

			// Assert
			Assert.False(didEventFire);
		}

		[Test]
		public void WeakEventManagerActionT_UnassignedEventManager()
		{
			// Arrange
			var unassignedEventManager = new WeakEventManager<string>();
			var didEventFire = false;

			ActionEvent += HandleDelegateTest;
			void HandleDelegateTest(string message)
			{
				Assert.IsNotNull(message);
				Assert.IsNotEmpty(message);

				didEventFire = true;
			}

			// Act
			unassignedEventManager.RaiseEvent(string.Empty, nameof(ActionEvent));

			// Assert
			Assert.False(didEventFire);
			ActionEvent -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerActionT_HandleEvent_InvalidHandleEvent()
		{
			// Arrange
			ActionEvent += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest(string message)
			{
				Assert.IsNotNull(message);
				Assert.IsNotEmpty(message);

				didEventFire = true;
			}

			// Act

			// Assert
			Assert.Throws<InvalidHandleEventException>(() => actionEventManager.RaiseEvent(this, "Test", nameof(ActionEvent)));
			Assert.False(didEventFire);
			ActionEvent -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerActionT_AddEventHandler_NullHandler()
		{
			// Arrange
			Action<string>? nullAction = null;

			// Act

			// Assert
#pragma warning disable CS8604 // Possible null reference argument.
			Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(nullAction, nameof(ActionEvent)));
#pragma warning restore CS8604 // Possible null reference argument.

		}

		[Test]
		public void WeakEventManagerActionT_AddEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(s => { var temp = s; }, null!));
		}

		[Test]
		public void WeakEventManagerActionT_AddEventHandler_EmptyEventName()
		{
			// Arrange
			Action<string>? nullAction = null;

			// Act

			// Assert
#pragma warning disable CS8604 // Possible null reference argument.
			Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(nullAction, string.Empty));
#pragma warning restore CS8604 // Possible null reference argument.
		}

		[Test]
		public void WeakEventManagerActionT_AddEventHandler_WhitespaceEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(s => { var temp = s; }, " "));
		}

		[Test]
		public void WeakEventManagerActionT_RemoveEventHandler_NullHandler()
		{
			// Arrange
			Action<string>? nullAction = null;

			// Act

			// Assert
#pragma warning disable CS8604 // Possible null reference argument.
			Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(nullAction));
#pragma warning restore CS8604 // Possible null reference argument.
		}

		[Test]
		public void WeakEventManagerActionT_RemoveEventHandler_NullEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(s => { var temp = s; }, null!));
        }

        [Test]
		public void WeakEventManagerActionT_RemoveEventHandler_EmptyEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(s => { var temp = s; }, string.Empty));
		}

		[Test]
		public void WeakEventManagerActionT_RemoveEventHandler_WhiteSpaceEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(s => { var temp = s; }, " "));
		}
	}
}
