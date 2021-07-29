#nullable enable
using CommunityToolkit.Maui.Helpers;
using System;

namespace CommunityToolkit.Maui.UnitTests.Helpers.WeakEventManagerTests
{
    public class BaseWeakEventManagerTests
	{
		protected event EventHandler TestEvent
		{
			add => TestWeakEventManager.AddEventHandler(value);
			remove => TestWeakEventManager.RemoveEventHandler(value);
		}

		protected event EventHandler<string> TestStringEvent
		{
			add => TestStringWeakEventManager.AddEventHandler(value);
			remove => TestStringWeakEventManager.RemoveEventHandler(value);
		}

		protected DelegateWeakEventManager TestWeakEventManager { get; } = new DelegateWeakEventManager();

		protected WeakEventManager<string> TestStringWeakEventManager { get; } = new WeakEventManager<string>();
	}
}
