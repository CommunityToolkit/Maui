using System.ComponentModel;
using Microsoft.Maui.Controls.Compatibility.Platform.Tizen;
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

enum FileSelectionMode
{
	FileOpen,
	FileSave,
	FolderChoose,
	FileOpenRoot,
	FileSaveRoot,
	FolderChooseRoot
}

sealed class FileFolderDialog : Popup<string>, IDisposable
{
	const string previousDirectorySymbol = "..";
	const string slashSymbol = "/";

	readonly bool isRootSelectionMode;
	readonly bool isFileSelectionMode;
	readonly FileSelectionMode fileSelectionMode;
	readonly string initialFileFolderPath;

	ScrollView? directoryScrollView;
	Entry? fileNameEntry;
	List<View> directoyViews;
	string selectedPath;
	string selectedFileName;

	public FileFolderDialog(FileSelectionMode mode, string initialPath, CancellationToken cancellationToken = default, string fileName = "default")
	{
		if (!Enum.IsDefined(typeof(FileSelectionMode), mode))
		{
			throw new InvalidEnumArgumentException($"{mode} is not valid for {nameof(FileSelectionMode)}.");
		}
		if (!File.Exists(initialPath) && !Directory.Exists(initialPath))
		{
			throw new FileNotFoundException($"Could not locate {initialPath}");
		}

		fileSelectionMode = mode;
		selectedFileName = fileName;
		selectedPath = initialFileFolderPath = initialPath;
		directoyViews = new List<View>();
		isRootSelectionMode = mode is FileSelectionMode.FileOpenRoot
									or FileSelectionMode.FileSaveRoot
									or FileSelectionMode.FolderChooseRoot;
		isFileSelectionMode = mode is FileSelectionMode.FileOpen
									or FileSelectionMode.FileOpenRoot
									or FileSelectionMode.FileSave
									or FileSelectionMode.FileSaveRoot;
	}

	protected override View CreateContent()
	{
		Layout = new LinearLayout
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		BackgroundColor = new TColor(0.1f, 0.1f, 0.1f, 0.5f).ToNative();

		
		var isHorizontal = Window.Instance.WindowSize.Width > Window.Instance.WindowSize.Height;
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
			SizeWidth = Window.Instance.WindowSize.Width * (isHorizontal ? 0.5f : 0.8f),
			BackgroundColor = TColor.White.ToNative(),
		};

		content.Add(new Label
		{
			Text = fileSelectionMode switch
			{
				FileSelectionMode.FileOpen or FileSelectionMode.FileOpenRoot => "Open",
				FileSelectionMode.FileSave or FileSelectionMode.FileSaveRoot => "Save As",
				FileSelectionMode.FolderChoose or FileSelectionMode.FolderChooseRoot => "Select Folder",
				_ => throw new NotSupportedException($"{fileSelectionMode} is not yet supported")
			},
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

		if (fileSelectionMode is FileSelectionMode.FileSave)
		{
			var newFolderButton = new Button
			{
				Focusable = true,
				Text = "New Folder",
				TextColor = TColor.Black,
				Margin = new Extents(margin1, margin1, 0, 0),
				WidthSpecification = LayoutParamPolicies.MatchParent
			};
			newFolderButton.Clicked += async (sender, args) =>
			{
				try
				{
					var newDirectoryName = await new PromptPopup("New Folder Name", "").Open();
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
				catch(Exception ex)
				{
					new Tizen.Applications.ToastMessage
					{
						Message = $"Failed to create new folder, {ex.Message}"
					}.Post();
				}
			};
			content.Add(newFolderButton);
		}

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
			fileNameEntry = new Entry()
			{
				Text = selectedFileName,
				Margin = new Extents(margin1, margin1, 0, 0),
				WidthSpecification = LayoutParamPolicies.MatchParent
			};
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

		var okButton = new Button
		{
			Focusable = true,
			Text = "OK",
			TextColor = TColor.Black,
			BackgroundColor = TColor.Transparent.ToNative(),
		};
		okButton.TextLabel.PixelSize = 15d.ToPixel();
		okButton.SizeWidth = okButton.TextLabel.NaturalSize.Width + 15d.ToPixel() * 2;
		okButton.Clicked += (s, e) =>
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

		var cancelButton = new Button
		{
			Focusable = true,
			Text = "Cancel",
			TextColor = TColor.Black,
			BackgroundColor = TColor.Transparent.ToNative(),
		};
		cancelButton.TextLabel.PixelSize = 15d.ToPixel();
		cancelButton.SizeWidth = cancelButton.TextLabel.NaturalSize.Width + 15d.ToPixel() * 2;
		cancelButton.Clicked += (s, e) => SendCancel();
		hlayout.Add(cancelButton);

		Relayout += (s, e) =>
		{
			var isHorizontal = Window.Instance.WindowSize.Width > Window.Instance.WindowSize.Height;
			content.SizeWidth = Window.Instance.WindowSize.Width * (isHorizontal ? 0.5f : 0.8f);
		};

		return content;
	}

	void ProcessSelect(string selectedItem)
	{
		if (selectedItem.Equals(previousDirectorySymbol, StringComparison.Ordinal))
		{
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

		foreach(var view in directoyViews)
		{
			directoryScrollView.ContentContainer.Remove(view);
		}
		directoyViews.Clear();

		var listItems = GetDirectories(path);

		if (listItems != null)
		{
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
				itemLabel.TouchEvent += (s, e) =>
				{
					var state = e.Touch.GetState(0);
					if (state == PointStateType.Up && itemLabel.IsInside(e.Touch.GetLocalPosition(0)))
					{
						ProcessSelect(item);
						return true;
					}
					return false;
				};
				itemLabel.KeyEvent += (s, e) =>
				{
					if (e.Key.IsAcceptKeyEvent())
					{
						ProcessSelect(item);
						return true;
					}
					return false;
				};
				directoryScrollView.ContentContainer.Add(itemLabel);
				directoyViews.Add(itemLabel);
			}
			directoryScrollView.SizeHeight = 30d.ToPixel() * Math.Min(listItems.Count(), 5);
		}
	}

	public bool IsDirectory(string path)
	{
		if (((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory))
		{
			return true;
		}
		return false;
	}

	static bool CreateSubDirectory(string newDirectory)
	{
		Directory.CreateDirectory(newDirectory);
		return Directory.Exists(newDirectory);
	}

	List<string> GetDirectories(string path)
	{
		var directories = new List<string>();
		var directoryPath = IsDirectory(path) ? path : Path.GetDirectoryName(path);

		if (isRootSelectionMode || !slashSymbol.Equals(selectedPath, StringComparison.Ordinal))
		{
			directories.Add(previousDirectorySymbol);
		}

		if (!Directory.Exists(directoryPath) || !IsDirectory(directoryPath))
		{
			return directories;
		}

		string[] allFilesAndDirectories = Directory.GetDirectories(directoryPath);
		allFilesAndDirectories = allFilesAndDirectories.Concat(Directory.GetFiles(directoryPath)).ToArray();
		foreach (var item in allFilesAndDirectories ?? Array.Empty<string>())
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