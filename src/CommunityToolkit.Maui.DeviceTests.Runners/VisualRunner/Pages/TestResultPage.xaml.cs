// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/Pages/TestResultPage.xaml.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner.Pages
{
	partial class TestResultPage : ContentPage
	{
		public TestResultPage()
		{
			InitializeComponent();

			CopyMessage.Clicked += CopyMessageClicked;
			CopyTrace.Clicked += CopyTraceClicked;
		}

		async void CopyMessageClicked(object? sender, System.EventArgs e)
		{
			await Clipboard.Default.SetTextAsync(ErrorMessage.Text);
		}

		async void CopyTraceClicked(object? sender, System.EventArgs e)
		{
			await Clipboard.Default.SetTextAsync(ErrorTrace.Text);
		}
	}
}