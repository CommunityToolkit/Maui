using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
//using CommunityToolkit.Maui.Behaviors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestX;

namespace CommunityToolkit.Maui.DeviceTests;
public class App : RunnerApp
{
}



[TestClass]
public class Tests
{
	[TestMethod]
	public async Task SelectAllTextBehaviorTest()
	{
		var entry = new Entry
		{
			Text = "Bla"
		};

		var handler = CreateHandler<EntryHandler>(entry);
		bool hasSelection = false;

		if (entry.Handler is null)
		{

			throw new Exception("Handler is still null?!");
			
//			entry.HandlerChanged += (s, e) =>
//			{
//#if ANDROID
//				if (entry.Handler!.PlatformView is not Android.Widget.EditText edit)
//				{
//					throw new Exception("PlatformView isn't a EditText");
//				}

//				hasSelection = edit.HasSelection;
//#endif
//				Assert.IsTrue(hasSelection);
//			};

//			await Task.Delay(-1);
		}

#if ANDROID
		if (entry.Handler.PlatformView is not Android.Widget.EditText edit)
		{
			throw new Exception("PlatformView isn't a EditText");
		}

		entry.Behaviors.Add(new Behaviors.SelectAllTextBehavior());

		entry.Dispatcher.Dispatch(() => entry.Focus());
		if (!entry.IsFocused)
		{
			edit.Focus(new FocusRequest(true));
		}
		
		hasSelection = edit.HasSelection;
#endif

		Assert.IsTrue(hasSelection);
	}


	protected THandler CreateHandler<THandler>(IView view)
			where THandler : IViewHandler
	{
		var handler = Activator.CreateInstance<THandler>();
		handler.SetMauiContext(App.Current!.Handler!.MauiContext!);

		handler.SetVirtualView(view);
		view.Handler = handler;

		var size = view.Measure(double.PositiveInfinity, double.PositiveInfinity);
		view.Arrange(new Rect(0, 0, size.Width, size.Height));
		handler.PlatformArrange(view.Frame);

		return handler;
	}
}