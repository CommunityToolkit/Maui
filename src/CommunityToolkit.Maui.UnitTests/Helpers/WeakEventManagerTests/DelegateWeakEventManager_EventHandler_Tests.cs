using CommunityToolkit.Maui.Exceptions;
using CommunityToolkit.Maui.Helpers;
using NUnit.Framework;
using System;

namespace CommunityToolkit.Maui.UnitTests.Helpers.WeakEventManagerTests
{
    public class DelegateWeakEventManager_EventHandler_Tests : BaseWeakEventManagerTests
	{
		[Test]
		public void WeakEventManager_HandleEvent_ValidImplementation()
		{
			// Arrange
			TestEvent += HandleTestEvent;
			var didEventFire = false;

			void HandleTestEvent(object? sender, EventArgs e)
			{
				if (sender == null)
					throw new ArgumentNullException(nameof(sender));

				Assert.IsNotNull(sender);
				Assert.AreEqual(GetType(), sender.GetType());

				Assert.IsNotNull(e);

				didEventFire = true;
				TestEvent -= HandleTestEvent;
			}

			// Act
			TestWeakEventManager.RaiseEvent(this, new EventArgs(), nameof(TestEvent));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManager_HandleEvent_NullSender()
		{
			// Arrange
			TestEvent += HandleTestEvent;
			var didEventFire = false;

			void HandleTestEvent(object? sender, EventArgs e)
			{
				Assert.Null(sender);
				Assert.IsNotNull(e);

				didEventFire = true;
				TestEvent -= HandleTestEvent;
			}

            // Act
            TestWeakEventManager.RaiseEvent(null, new EventArgs(), nameof(TestEvent));

            // Assert
            Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManager_HandleEvent_EmptyEventArgs()
		{
			// Arrange
			TestEvent += HandleTestEvent;
			var didEventFire = false;

			void HandleTestEvent(object? sender, EventArgs e)
			{
				if (sender == null)
					throw new ArgumentNullException(nameof(sender));

				Assert.IsNotNull(sender);
				Assert.AreEqual(GetType(), sender.GetType());

				Assert.IsNotNull(e);
				Assert.AreEqual(EventArgs.Empty, e);

				didEventFire = true;
				TestEvent -= HandleTestEvent;
			}

			// Act
			TestWeakEventManager.RaiseEvent(this, EventArgs.Empty, nameof(TestEvent));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManager_HandleEvent_NullEventArgs()
		{
			// Arrange
			TestEvent += HandleTestEvent;
			var didEventFire = false;

			void HandleTestEvent(object? sender, EventArgs e)
			{
				if (sender == null)
					throw new ArgumentNullException(nameof(sender));

				Assert.IsNotNull(sender);
				Assert.AreEqual(GetType(), sender.GetType());

				Assert.Null(e);

				didEventFire = true;
				TestEvent -= HandleTestEvent;
			}

			// Act
			TestWeakEventManager.RaiseEvent(this, null!, nameof(TestEvent));

			// Assert
			Assert.IsTrue(didEventFire);
		}

		[Test]
		public void WeakEventManager_HandleEvent_InvalidHandleEventName()
		{
			// Arrange
			TestEvent += HandleTestEvent;
			var didEventFire = false;

			void HandleTestEvent(object? sender, EventArgs e) => didEventFire = true;

			// Act
			TestWeakEventManager.RaiseEvent(this, new EventArgs(), nameof(TestStringEvent));

			// Assert
			Assert.False(didEventFire);
			TestEvent -= HandleTestEvent;
		}

		[Test]
		public void WeakEventManager_UnassignedEvent()
		{
			// Arrange
			var didEventFire = false;

			TestEvent += HandleTestEvent;
			TestEvent -= HandleTestEvent;
			void HandleTestEvent(object? sender, EventArgs e) => didEventFire = true;

			// Act
			TestWeakEventManager.RaiseEvent(null, null!, nameof(TestEvent));

			// Assert
			Assert.False(didEventFire);
		}

		[Test]
		public void WeakEventManager_UnassignedEventManager()
		{
			// Arrange
			var unassignedEventManager = new DelegateWeakEventManager();
			var didEventFire = false;

			TestEvent += HandleTestEvent;
			void HandleTestEvent(object? sender, EventArgs e) => didEventFire = true;

			// Act
			unassignedEventManager.RaiseEvent(null, null!, nameof(TestEvent));

			// Assert
			Assert.False(didEventFire);
			TestEvent -= HandleTestEvent;
		}

		[Test]
		public void WeakEventManager_AddEventHandler_NullHandler()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.AddEventHandler(null));
        }

        [Test]
		public void WeakEventManager_AddEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.AddEventHandler(null, null!));
		}

		[Test]
		public void WeakEventManager_AddEventHandler_EmptyEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.AddEventHandler(null, string.Empty));
        }

        [Test]
		public void WeakEventManager_AddEventHandler_WhitespaceEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.AddEventHandler(null, " "));
        }

        [Test]
		public void WeakEventManager_RemoveEventHandler_NullHandler()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.RemoveEventHandler(null));
        }

        [Test]
		public void WeakEventManager_RemoveEventHandler_NullEventName()
		{
			// Arrange

			// Act

			// Assert
			Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.RemoveEventHandler(null, null!));
		}

		[Test]
		public void WeakEventManager_RemoveEventHandler_EmptyEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.RemoveEventHandler(null, string.Empty));
        }

        [Test]
		public void WeakEventManager_RemoveEventHandler_WhiteSpaceEventName()
		{
            // Arrange

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => TestWeakEventManager.RemoveEventHandler(null, " "));
        }

        [Test]
		public void WeakEventManager_HandleEvent_InvalidHandleEvent()
		{
			// Arrange
			TestEvent += HandleTestEvent;
			var didEventFire = false;

			void HandleTestEvent(object? sender, EventArgs e) => didEventFire = true;

			// Act

			// Assert
			Assert.Throws<InvalidHandleEventException>(() => TestWeakEventManager.RaiseEvent(nameof(TestEvent)));
			Assert.False(didEventFire);
			TestEvent -= HandleTestEvent;
		}
	}
}
