// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original paths: src/Core/tests/DeviceTests.Shared/HandlerTests/HandlerTestBase.cs and the public-API subset of
//                 src/Controls/tests/DeviceTests/ControlsHandlerTestBase.cs (CreateHandler(Async), GetValueAsync, OnLoadedAsync,
//                 OnUnloadedAsync).
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace CommunityToolkit.Maui.DeviceTests. Controls handlers are registered with the public
//                 UseMauiApp<TestApplicationStub>() instead of the internal RemapForControls()/ConfigureTestBuilder().
//                 CreateHandlerAndAddToWindow (internal window/fragment/modal plumbing) is not ported: host views with AttachAndRun.
#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.DeviceTests
{
	public class HandlerTestBase : HandlerTestBasement
	{
		protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
			base.ConfigureBuilder(mauiAppBuilder)
				// Registers every Controls handler and the Controls mapper remappings. TestApplicationStub is already registered,
				// so UseMauiApp's TryAdd keeps it and Application.Current stays the runner's application.
				.UseMauiApp<TestApplicationStub>();

		protected THandler CreateHandler<THandler>(IElement view)
			where THandler : IElementHandler, new()
		{
			return CreateHandler<THandler>(view, MauiContext);
		}

		protected async Task<THandler> CreateHandlerAsync<THandler>(IElement view)
			where THandler : IElementHandler, new() =>
			await InvokeOnMainThreadAsync(() => CreateHandler<THandler>(view));

		protected IElementHandler CreateHandler(IElement view)
		{
			var handler = view.ToHandler(MauiContext);
			InitializeViewHandler(view, handler, MauiContext);
			return handler;
		}

		protected async Task<IElementHandler> CreateHandlerAsync(IElement view) =>
			await InvokeOnMainThreadAsync(() => CreateHandler(view));

		protected Task<TValue> GetValueAsync<TValue, THandler>(IElement view, Func<THandler, TValue> func)
			 where THandler : IElementHandler, new()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var handler = CreateHandler<THandler>(view);
				return func(handler);
			});
		}

		protected Task<TValue> GetValueAsync<TValue>(IElement view, Func<IPlatformViewHandler, TValue> func)
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var handler = (IPlatformViewHandler)view.ToHandler(MauiContext);
				return func(handler);
			});
		}

		protected Task<TValue> GetValueAsync<TValue>(IElement view, Func<IPlatformViewHandler, Task<TValue>> func)
		{
			return InvokeOnMainThreadAsync(async () =>
			{
				var handler = (IPlatformViewHandler)view.ToHandler(MauiContext);
				return await func(handler);
			});
		}

		protected Task OnLoadedAsync(VisualElement frameworkElement, TimeSpan? timeOut = null) =>
			frameworkElement.OnLoadedAsync(timeOut);

		protected Task OnUnloadedAsync(VisualElement frameworkElement, TimeSpan? timeOut = null) =>
			frameworkElement.OnUnloadedAsync(timeOut);
	}
}
