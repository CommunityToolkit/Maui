using CommunityToolkit.Maui.Exceptions;
using CommunityToolkit.Maui.Helpers;
using NUnit.Framework;
using System;

namespace CommunityToolkit.Maui.UnitTests.Helpers.WeakEventManagerTests
{
    public class DelegateWeakEventManager_Action_Tests : BaseWeakEventManagerTests
	{
		readonly DelegateWeakEventManager actionEventManager = new();

		public event Action ActionEvent
		{
			add => actionEventManager.AddEventHandler(value);
			remove => actionEventManager.RemoveEventHandler(value);
		}

		[Test]
		public void WeakEventManagerAction_HandleEvent_ValidImplementation()
		{
			// Arrange
			ActionEvent += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest()
			{
				didEventFire = true;
				ActionEvent -= HandleDelegateTest;
			}

			// Act
			actionEventManager.RaiseEvent(nameof(ActionEvent));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManagerAction_HandleEvent_InvalidHandleEventEventName()
		{
			// Arrange
			ActionEvent += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest() => didEventFire = true;

			// Act
			actionEventManager.RaiseEvent(nameof(TestStringEvent));

			// Assert
			Assert.False(didEventFire);
			ActionEvent -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerAction_UnassignedEvent()
		{
			// Arrange
			var didEventFire = false;

			ActionEvent += HandleDelegateTest;
			ActionEvent -= HandleDelegateTest;
			void HandleDelegateTest() => didEventFire = true;

			// Act
			actionEventManager.RaiseEvent(nameof(ActionEvent));

			// Assert
			Assert.False(didEventFire);
		}

		[Test]
		public void WeakEventManagerAction_UnassignedEventManager()
		{
			// Arrange
			var unassignedEventManager = new DelegateWeakEventManager();
			var didEventFire = false;

			ActionEvent += HandleDelegateTest;
			void HandleDelegateTest() => didEventFire = true;

			// Act
			unassignedEventManager.RaiseEvent(nameof(ActionEvent));

			// Assert
			Assert.False(didEventFire);
			ActionEvent -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerAction_HandleEvent_InvalidHandleEvent()
		{
			// Arrange
			ActionEvent += HandleDelegateTest;
			var didEventFire = false;

			void HandleDelegateTest() => didEventFire = true;

			// Act

			// Assert
			Assert.Throws<InvalidHandleEventException>(() => actionEventManager.RaiseEvent(this, EventArgs.Empty, nameof(ActionEvent)));
			Assert.False(didEventFire);
			ActionEvent -= HandleDelegateTest;
		}

		[Test]
		public void WeakEventManagerAction_AddEventHandler_NullHandler()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(null));
        }

        [Test]
		public void WeakEventManagerAction_AddEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(null, null!));
		}

		[Test]
		public void WeakEventManagerAction_AddEventHandler_EmptyEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(null, string.Empty));
        }

        [Test]
		public void WeakEventManagerAction_AddEventHandler_WhitespaceEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => actionEventManager.AddEventHandler(null, " "));
        }

        [Test]
		public void WeakEventManagerAction_RemoveEventHandler_NullHandler()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(null));
        }

        [Test]
		public void WeakEventManagerAction_RemoveEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(null, null!));
		}

		[Test]
		public void WeakEventManagerAction_RemoveEventHandler_EmptyEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(null, string.Empty));
        }

        [Test]
		public void WeakEventManagerAction_RemoveEventHandler_WhiteSpaceEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => actionEventManager.RemoveEventHandler(null, " "));
        }
    }
}
