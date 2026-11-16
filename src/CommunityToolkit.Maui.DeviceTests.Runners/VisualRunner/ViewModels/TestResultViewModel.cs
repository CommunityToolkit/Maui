// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/ViewModels/TestResultViewModel.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Controls;
using Xunit.Abstractions;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	public class TestResultViewModel : ViewModelBase
	{
		TimeSpan _duration;
		string? _errorMessage;
		string? _errorStackTrace;
		ImageSource? _errorImage;

		public TestResultViewModel(TestCaseViewModel testCase, ITestResultMessage? testResult)
		{
			TestCase = testCase ?? throw new ArgumentNullException(nameof(testCase));
			TestResultMessage = testResult;

			if (testResult != null)
				testCase.UpdateTestState(this);
		}

		public TestCaseViewModel TestCase { get; }

		public ITestResultMessage? TestResultMessage { get; }

		public TimeSpan Duration
		{
			get => _duration;
			set => Set(ref _duration, value, () => TestCase?.UpdateTestState(this));
		}

		public string? ErrorMessage
		{
			get => _errorMessage;
			set
			{
				if (Set(ref _errorMessage, value))
				{
					ExtractErrorMessage(value);
				}
			}
		}

		public string? ErrorStackTrace
		{
			get => _errorStackTrace;
			set => Set(ref _errorStackTrace, value);
		}

		public ImageSource? ErrorImage
		{
			get => _errorImage;
			set => Set(ref _errorImage, value);
		}

		public string? Output => TestResultMessage?.Output;

		public bool HasOutput => !string.IsNullOrWhiteSpace(Output);

		void ExtractErrorMessage(string? message)
		{
			if (message == null)
			{
				ErrorImage = null;
				return;
			}

			const string openTag = "<img>";
			const string closeTag = "</img>";
			var openTagIndex = message.IndexOf("<img>");
			var closeTagIndex = message.IndexOf("</img>");

			if (openTagIndex >= 0 && closeTagIndex > openTagIndex)
			{
				var imgString = message.Substring(openTagIndex + openTag.Length, closeTagIndex - openTagIndex - openTag.Length);
				var messageBefore = message.Substring(0, openTagIndex);
				var messageAfter = message.Substring(closeTagIndex + closeTag.Length);
				var imgBytes = Convert.FromBase64String(imgString);
				var stream = new MemoryStream(imgBytes);
				ErrorImage = ImageSource.FromStream(() => stream);
			}
		}
	}
}