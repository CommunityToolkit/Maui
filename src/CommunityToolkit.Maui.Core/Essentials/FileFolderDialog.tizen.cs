using Microsoft.Maui.Platform;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.UIExtensions.NUI;
using Path = System.IO.Path;
using Button = Tizen.UIExtensions.NUI.Button;
using Entry = Tizen.UIExtensions.NUI.Entry;
using Label = Tizen.UIExtensions.NUI.Label;
using TColor = Tizen.UIExtensions.Common.Color;
using ScrollView = Tizen.UIExtensions.NUI.ScrollView;
using Shadow = Tizen.NUI.Shadow;
using HorizontalAlignment = Tizen.NUI.HorizontalAlignment;
using VerticalAlignment = Tizen.NUI.VerticalAlignment;
using View = Tizen.NUI.BaseComponents.View;
using Window = Tizen.NUI.Window;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// FileFolderDialog Localization Strings.
/// </summary>
public static class FileFolderDialogLocalization
{
	/// <summary>
	/// Gets or sets the action label.
	/// </summary>
	public static string FileSaveAs { get; set; } = "Save As";
	
	/// <summary>
	/// Gets or sets the action button text.
	/// </summary>
	public static string SaveButton { get; set; } = "Save";
	
	/// <summary>
	/// Gets or sets the action button text.
	/// </summary>
	public static string OkButton { get; set; } = "OK";
	
	/// <summary>
	/// Gets or sets the cancel button text.
	/// </summary>
	public static string CancelButton { get; set; } = "Cancel";

	/// <summary>
	/// Gets or sets the action button text.
	/// </summary>
	public static string SelectFolder { get; set; } = "Select Folder";

	/// <summary>
	/// Gets or sets the NewFolder action button text.
	/// </summary>
	public static string NewFolderButton { get; set; } = "New Folder";
	
	/// <summary>
	/// Gets or sets the NewFolder prompt title.
	/// </summary>
	public static string NewFolderNameTitle { get; set; } = "New Folder Name";
	
	/// <summary>
	/// Gets or sets the Create folder error message.
	/// </summary>
	public static string CreateFolderErrorMessage { get; set; } = "Failed to create new folder";
}

sealed class FileFolderDialog : Popup<string>
{

	const string previousDirectorySymbol = "..";
	const string slashSymbol = "/";

	readonly bool isFileSelectionMode;
	readonly string initialFileFolderPath;

	ScrollView? directoryScrollView;
	Entry? fileNameEntry;
	List<View> directoryViews;
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
		directoryViews = new List<View>();
		isFileSelectionMode = isFileSelection;
	}

	public static string TryGetExternalDirectory()
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

	public bool IsDirectory(string path)
	{
		return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
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

		var content = new View
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
			var newFolderButton = new Button
			{
				Focusable = true,
				Text = FileFolderDialogLocalization.NewFolderButton,
				TextColor = TColor.Black,
				BackgroundColor = TColor.Transparent.ToNative(),
				Margin = new Extents(margin1, margin1, 0, 0),
				WidthSpecification = LayoutParamPolicies.MatchParent
			};
			newFolderButton.Clicked += async (_, _) =>
			{
				try
				{
					var newDirectoryName = await new PromptPopup(FileFolderDialogLocalization.NewFolderNameTitle, "").Open();
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
			};
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
			PropertyMap underline = new PropertyMap();
			underline.Add("enable", new PropertyValue("True"));
			fileNameEntry.Underline = underline;
			fileNameEntry.PixelSize = 15d.ToPixel();
			content.Add(fileNameEntry);
		}

		var hlayout = new View
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
		content.Add(hlayout);

		var cancelButton = new Button
		{
			Focusable = true,
			Text = FileFolderDialogLocalization.CancelButton,
			TextColor = TColor.Black,
			BackgroundColor = TColor.Transparent.ToNative(),
		};
		cancelButton.TextLabel.PixelSize = 15d.ToPixel();
		cancelButton.SizeWidth = cancelButton.TextLabel.NaturalSize.Width + 15d.ToPixel() * 2;
		cancelButton.Clicked += (_, _) => SendCancel();
		hlayout.Add(cancelButton);

		var okButton = new Button
		{
			Focusable = true,
			Text = isFileSelectionMode ? FileFolderDialogLocalization.SaveButton : FileFolderDialogLocalization.OkButton,
			TextColor = TColor.Black,
			BackgroundColor = TColor.Transparent.ToNative(),
		};
		okButton.TextLabel.PixelSize = 15d.ToPixel();
		okButton.SizeWidth = okButton.TextLabel.NaturalSize.Width + 15d.ToPixel() * 2;
		okButton.Clicked += (_, _) =>
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
		};
		hlayout.Add(okButton);

		Relayout += (_, _) =>
		{
			content.SizeWidth = Window.Instance.WindowSize.Width * (IsHorizontal() ? 0.5f : 0.8f);
		};

		return content;
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

	List<string> GetDirectories(string path)
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

		var allFilesAndDirectories = Directory.GetDirectories(directoryPath);
		allFilesAndDirectories = allFilesAndDirectories.Concat(Directory.GetFiles(directoryPath)).ToArray();
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