// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/Converters/RunStatusToColorConverter.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: ConvertBack throws NotSupportedException (the converter is intentionally one-way) instead of NotImplementedException.
#nullable enable
using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	class RunStatusToColorConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not RunStatus status || Application.Current == null)
				return Colors.Red;

			return status switch
			{
				RunStatus.Ok => Application.Current.Resources["VisualRunnerSuccessfulTestsColor"],
				RunStatus.Failed => Application.Current.Resources["VisualRunnerFailedTestsColor"],
				RunStatus.NoTests => Application.Current.Resources["VisualRunnerNoTestsColor"],
				RunStatus.NotRun => Application.Current.Resources["VisualRunnerNotRunTestsColor"],
				RunStatus.Skipped => Application.Current.Resources["VisualRunnerSkippedTestsColor"],
				_ => throw new ArgumentOutOfRangeException(nameof(value)),
			};
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			throw new NotSupportedException($"{nameof(RunStatusToColorConverter)} is one-way.");
	}
}