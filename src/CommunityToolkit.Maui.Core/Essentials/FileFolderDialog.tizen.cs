using System.Collections.Generic;
using Microsoft.Maui.Platform;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Tizen.UIExtensions.NUI;
using Button = Tizen.UIExtensions.NUI.Button;
using Entry = Tizen.UIExtensions.NUI.Entry;
using HorizontalAlignment = Tizen.NUI.HorizontalAlignment;
using Label = Tizen.UIExtensions.NUI.Label;
using Path = System.IO.Path;
using ScrollView = Tizen.UIExtensions.NUI.ScrollView;
using Shadow = Tizen.NUI.Shadow;
using TColor = Tizen.UIExtensions.Common.Color;
using View = Tizen.NUI.BaseComponents.View;
using VerticalAlignment = Tizen.NUI.VerticalAlignment;
using Window = Tizen.NUI.Window;

namespace CommunityToolkit.Maui.Storage;

sealed class FileFolderDialog : Popup<string>
{
	const string previousDirectorySymbol = "..";
	const string slashSymbol = "/";

	readonly bool isFileSelectionMode;
	readonly string initialFileFolderPath;
	readonly List<View> directoryViews = new();

	ScrollView? directoryScrollView;
	View? content;
	Button? okButton, cancelButton, newFolderButton;
	Entry? fileNameEntry;
	string selectedPath;
	string selectedFileName;

	public FileFolderDialog(bool isFileSelection, string initialPath, string fileName = "default")
	{
		if (!File.Exists(initialPath) && !Directory.Exists(initialPath))
		{
			throw new FileNotFoundException($"Could not locate {initialPath}");
		}

		selectedFileName = fileName;
		selectedPath = initialFileFolderPath = initialPath;
		isFileSelectionMode = isFileSelection;
	}

	public static string GetExternalDirectory()
	{
		string? externalDirectory = null;
		foreach (var storage in Tizen.System.StorageManager.Storages)
		{
			if (storage.StorageType == Tizen.System.StorageArea.External)
			{
				externalDirectory = storage.RootDirectory;
				break;
			}

			if (storage.StorageType == Tizen.System.StorageArea.Internal)
			{
				externalDirectory = storage.RootDirectory;
			}
		}

		return externalDirectory ?? throw new InvalidOperationException("Cannot locate external directory.");
	}

	public static bool IsDirectory(string path)
	{
		return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
	}

	protected override void Dispose(bool disposing)
	{
		Relayout -= OnRelayout;
		if (okButton is not null)
		{
			okButton.Clicked -= OnOkButtonClicked;
			okButton.Dispose();
		}

		if (cancelButton is not null)
		{
			cancelButton.Clicked -= OnCancelButtonClicked;
			cancelButton.Dispose();
		}

		if (newFolderButton is not null)
		{
			newFolderButton.Clicked -= OnNewFolderButtonClicked;
			newFolderButton.Dispose();
		}

		fileNameEntry?.Dispose();
		directoryScrollView?.Dispose();
		content?.Dispose();
		base.Dispose(disposing);
	}

	protected override View CreateContent()
	{
		Layout = new LinearLayout
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		BackgroundColor = new TColor(0.1f, 0.1f, 0.1f, 0.5f).ToNative();

		var margin1 = (ushort)20d.ToPixel();
		var margin2 = (ushort)10d.ToPixel();
		var radius = 8d.ToPixel();

		content = new View
		{
			CornerRadius = radius,
			BoxShadow = new Shadow(20d.ToPixel(), TColor.Black.ToNative()),
			Layout = new LinearLayout
			{
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				LinearOrientation = LinearLayout.Orientation.Vertical,
			},
			SizeWidth = Window.Instance.WindowSize.Width * (IsHorizontal() ? 0.5f : 0.8f),
			BackgroundColor = TColor.White.ToNative(),
		};

		content.Add(new Label
		{
			Text = isFileSelectionMode ? FileFolderDialogLocalization.FileSaveAs : FileFolderDialogLocalization.SelectFolder,
			Margin = new Extents(margin1, margin1, margin1, margin2),
			WidthSpecification = LayoutParamPolicies.MatchParent,
			HorizontalTextAlignment = Tizen.UIExtensions.Common.TextAlignment.Start,
			VerticalTextAlignment = Tizen.UIExtensions.Common.TextAlignment.Center,
			FontAttributes = Tizen.UIExtensions.Common.FontAttributes.Bold,
			TextColor = TColor.Black,
			PixelSize = 21d.ToPixel(),
		});

		content.Add(new View
		{
			BackgroundColor = TColor.FromHex("#cccccc").ToNative(),
			SizeHeight = 1.5d.ToPixel(),
			WidthSpecification = LayoutParamPolicies.MatchParent,
		});

		directoryScrollView = new ScrollView
		{
			Margin = new Extents(margin1, margin1, 0, 0),
			VerticalScrollBarVisibility = Tizen.UIExtensions.Common.ScrollBarVisibility.Default,
			WidthSpecification = LayoutParamPolicies.MatchParent,
		};
		directoryScrollView.ContentContainer.Layout = new LinearLayout
		{
			LinearOrientation = LinearLayout.Orientation.Vertical,
		};
		content.Add(directoryScrollView);

		UpdateDirectoryScrollView(initialFileFolderPath);

		content.Add(new View
		{
			BackgroundColor = TColor.FromHex("#cccccc").ToNative(),
			SizeHeight = 1.5d.ToPixel(),
			WidthSpecification = LayoutParamPolicies.MatchParent,
		});

		if (isFileSelectionMode)
		{
			newFolderButton = new Button
			{
				Focusable = true,
				Text = FileFolderDialogLocalization.NewFolderButton,
				TextColor = TColor.Black,
				BackgroundColor = TColor.Transparent.ToNative(),
				Margin = new Extents(margin1, margin1, 0, 0),
				WidthSpecification = LayoutParamPolicies.MatchParent
			};
			newFolderButton.Clicked += OnNewFolderButtonClicked;
			content.Add(newFolderButton);
		}

		if (isFileSelectionMode)
		{
			fileNameEntry = new Entry()
			{
				Text = selectedFileName,
				Margin = new Extents(margin1, margin1, 0, 0),
				WidthSpecification = LayoutParamPolicies.MatchParent
			};
			var underline = new PropertyMap();
			underline.Add("enable", new PropertyValue("True"));
			fileNameEntry.Underline = underline;
			fileNameEntry.PixelSize = 15d.ToPixel();
			content.Add(fileNameEntry);
		}

		var horizontalLayout = new View
		{
			Margin = new Extents(margin1, margin1, margin2, margin1),
			Layout = new LinearLayout
			{
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.End,
				LinearOrientation = LinearLayout.Orientation.Horizontal,
			},
			WidthSpecification = LayoutParamPolicies.MatchParent,
			HeightSpecification = LayoutParamPolicies.WrapContent
		};
		content.Add(horizontalLayout);

		cancelButton = new Button
		{
			Focusable = true,
			Text = FileFolderDialogLocalization.CancelButton,
			TextColor = TColor.Black,
			BackgroundColor = TColor.Transparent.ToNative(),
		};
		cancelButton.TextLabel.PixelSize = 15d.ToPixel();
		cancelButton.SizeWidth = cancelButton.TextLabel.NaturalSize.Width + 15d.ToPixel() * 2;
		cancelButton.Clicked += OnCancelButtonClicked;
		horizontalLayout.Add(cancelButton);

		okButton = new Button
		{
			Focusable = true,
			Text = isFileSelectionMode ? FileFolderDialogLocalization.SaveButton : FileFolderDialogLocalization.OkButton,
			TextColor = TColor.Black,
			BackgroundColor = TColor.Transparent.ToNative(),
		};
		okButton.TextLabel.PixelSize = 15d.ToPixel();
		okButton.SizeWidth = okButton.TextLabel.NaturalSize.Width + 15d.ToPixel() * 2;
		okButton.Clicked += OnOkButtonClicked;
		horizontalLayout.Add(okButton);

		Relayout += OnRelayout;

		return content;
	}

	async void OnNewFolderButtonClicked(object? sender, ClickedEventArgs e)
	{
		try
		{
			var newDirectoryName = await new PromptPopup(FileFolderDialogLocalization.NewFolderNameTitle, string.Empty).Open();
			var newDirectoryPath = IsDirectory(selectedPath) ? selectedPath : Path.GetDirectoryName(selectedPath);
			if (string.IsNullOrEmpty(newDirectoryPath) || string.IsNullOrEmpty(newDirectoryName))
			{
				return;
			}
			newDirectoryPath = newDirectoryPath.EndsWith(slashSymbol) ? newDirectoryPath : newDirectoryPath + slashSymbol;
			if (CreateSubDirectory(newDirectoryPath + newDirectoryName))
			{
				UpdateDirectoryScrollView(selectedPath);
			}
		}
		catch (Exception ex)
		{
			new Tizen.Applications.ToastMessage
			{
				Message = $"{FileFolderDialogLocalization.CreateFolderErrorMessage}, {ex.Message}"
			}.Post();
		}
	}

	void OnCancelButtonClicked(object? sender, ClickedEventArgs e)
	{
		SendCancel();
	}

	void OnOkButtonClicked(object? sender, ClickedEventArgs e)
	{
		if (isFileSelectionMode)
		{
			if (IsDirectory(selectedPath))
			{
				selectedFileName = fileNameEntry is null ? selectedFileName : fileNameEntry.Text;
				SendSubmit(selectedPath + selectedFileName);
			}
			else
			{
				SendSubmit(selectedPath);
			}
		}
		else
		{
			SendSubmit(selectedPath);
		}
	}

	void OnRelayout(object? sender, EventArgs e)
	{
		if (content is not null)
		{
			content.SizeWidth = Window.Instance.WindowSize.Width * (IsHorizontal() ? 0.5f : 0.8f);
		}
	}

	static bool IsHorizontal()
	{
		return Window.Instance.WindowSize.Width > Window.Instance.WindowSize.Height;
	}

	static bool CreateSubDirectory(string newDirectory)
	{
		Directory.CreateDirectory(newDirectory);
		return Directory.Exists(newDirectory);
	}

	void ProcessSelect(string selectedItem)
	{
		if (selectedItem.Equals(previousDirectorySymbol, StringComparison.Ordinal))
		{
			if (selectedPath.Equals(initialFileFolderPath, StringComparison.Ordinal)
				|| (selectedPath + slashSymbol).Equals(initialFileFolderPath, StringComparison.Ordinal))
			{
				return;
			}

			selectedPath = Path.GetDirectoryName(selectedPath) ?? initialFileFolderPath;
			selectedPath = selectedPath[..selectedPath.LastIndexOf(slashSymbol, StringComparison.Ordinal)];
		}
		else
		{
			if (!IsDirectory(selectedPath))
			{
				selectedPath = Path.GetDirectoryName(selectedPath) ?? selectedPath[..selectedPath.LastIndexOf(slashSymbol, StringComparison.Ordinal)];
			}
			selectedPath = selectedPath.EndsWith(slashSymbol) ? selectedPath : selectedPath + slashSymbol;
			selectedPath += selectedItem;
		}

		if (IsDirectory(selectedPath))
		{
			UpdateDirectoryScrollView(selectedPath);
		}
		else if (isFileSelectionMode)
		{
			fileNameEntry!.Text = selectedItem;
		}
	}

	void UpdateDirectoryScrollView(string path)
	{
		if (directoryScrollView is null)
		{
			return;
		}

		foreach (var view in directoryViews)
		{
			directoryScrollView.ContentContainer.Remove(view);
		}

		directoryViews.Clear();

		var listItems = GetDirectories(path);

		foreach (var item in listItems)
		{
			var itemLabel = new Label
			{
				Text = item,
				Focusable = true,
				HorizontalTextAlignment = Tizen.UIExtensions.Common.TextAlignment.Start,
				PixelSize = 16d.ToPixel(),
				WidthSpecification = LayoutParamPolicies.MatchParent,
				HeightSpecification = LayoutParamPolicies.WrapContent,
				Margin = new Extents(0, 0, (ushort)5d.ToPixel(), (ushort)5d.ToPixel()),
			};
			itemLabel.TouchEvent += (_, e) =>
			{
				var state = e.Touch.GetState(0);
				if (state == PointStateType.Up && itemLabel.IsInside(e.Touch.GetLocalPosition(0)))
				{
					ProcessSelect(item);
					return true;
				}

				return false;
			};
			itemLabel.KeyEvent += (_, e) =>
			{
				if (e.Key.IsAcceptKeyEvent())
				{
					ProcessSelect(item);
					return true;
				}

				return false;
			};
			directoryScrollView.ContentContainer.Add(itemLabel);
			directoryViews.Add(itemLabel);
		}
		directoryScrollView.SizeHeight = 30d.ToPixel() * Math.Min(listItems.Count, 5);
	}

	IReadOnlyList<string> GetDirectories(string path)
	{
		var directories = new List<string>();
		var directoryPath = IsDirectory(path) ? path : Path.GetDirectoryName(path);

		if (!slashSymbol.Equals(selectedPath, StringComparison.Ordinal))
		{
			directories.Add(previousDirectorySymbol);
		}

		if (!Directory.Exists(directoryPath) || !IsDirectory(directoryPath))
		{
			return directories;
		}

		var allFilesAndDirectories = Directory.GetDirectories(directoryPath).Concat(Directory.GetFiles(directoryPath));
		foreach (var item in allFilesAndDirectories)
		{
			var fileName = Path.GetFileName(item);
			if (IsDirectory(item))
			{
				directories.Add(fileName + slashSymbol);
			}
			else if (isFileSelectionMode)
			{
				directories.Add(fileName);
			}
		}

		directories.Sort();
		return directories;
	}
}