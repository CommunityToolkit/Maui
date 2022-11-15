using System.ComponentModel;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using Button = Android.Widget.Button;
using Color = Android.Graphics.Color;
using Environment = Android.OS.Environment;
using File = Java.IO.File;
using IOException = Java.IO.IOException;
using View = Android.Views.View;

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

sealed class FileFolderDialog : IDisposable
{
	readonly Context context;
	readonly bool isRootSelectionMode;
	readonly string? sdCardDirectory;
	readonly FileSelectionMode fileSelectionMode;

	AutoResetEvent? autoResetEvent;
	AlertDialog? directoriesDialog;
	EditText? inputText;

	string directory = string.Empty;
	ArrayAdapter<string>? listAdapter;
	TextView? titleView;
	TextView? titleView1;
	string selectedFileName;

	public FileFolderDialog(Context context, FileSelectionMode mode, string fileName = "default", string fileExtension = ".png")
	{
		if (!Enum.IsDefined(typeof(FileSelectionMode), mode))
		{
			throw new InvalidEnumArgumentException($"{mode} is not valid for {nameof(FileSelectionMode)}.");
		}

		fileSelectionMode = mode;
		selectedFileName = fileName + fileExtension;
		isRootSelectionMode = mode is FileSelectionMode.FileOpenRoot
									or FileSelectionMode.FileSaveRoot
									or FileSelectionMode.FolderChooseRoot;

		this.context = context;
		sdCardDirectory = Environment.RootDirectory.AbsolutePath;

		try
		{
			sdCardDirectory = new File(sdCardDirectory).CanonicalPath;
		}
		catch (IOException)
		{
		}
	}

	public void Dispose()
	{
		autoResetEvent?.Dispose();
		inputText?.Dispose();
		titleView?.Dispose();
		titleView1?.Dispose();
	}

	public async Task<string> GetFileOrDirectoryAsync(string directory, string defaultFileName = "default", CancellationToken cancellationToken = default)
	{
		string? result = null;

		var directoryFile = new File(directory);
		while (!directoryFile.Exists() || !directoryFile.IsDirectory)
		{
			if (directoryFile.Parent is null)
			{
				throw new FileNotFoundException($"Could not locate {directoryFile}");
			}

			directoryFile = new File(directory);
		}

		directory = new File(directory).CanonicalPath;

		this.directory = directory;
		var subDirectories = GetDirectories(directory);

		var dialogBuilder = CreateDirectoryChooserDialog(directory, subDirectories, (sender, args) =>
		{
			var parentDirectory = this.directory;
			var selection = ((AlertDialog?)sender)?.ListView?.Adapter?.GetItem(args.Which)?.ToString() ?? string.Empty;
			if (selection[^1] is '/')
			{
				selection = selection[..^1];
			}

			// Navigate into the sub-directory
			if (selection.Equals("..", StringComparison.Ordinal))
			{
				this.directory = this.directory[..this.directory.LastIndexOf("/", StringComparison.Ordinal)];
				if (this.directory.Equals(string.Empty, StringComparison.Ordinal))
				{
					this.directory = "/";
				}
			}
			else
			{
				this.directory += "/" + selection;
			}

			selectedFileName = defaultFileName;

			if (new File(this.directory).IsFile) // If the selection is a regular file
			{
				this.directory = parentDirectory;
				selectedFileName = selection;
			}

			UpdateDirectory(subDirectories);
		});

		dialogBuilder.SetPositiveButton("OK", (_, _) =>
		{
			// Current directory chosen
			// Call registered listener supplied with the chosen directory
			if (fileSelectionMode is FileSelectionMode.FileOpen
										or FileSelectionMode.FileOpenRoot
										or FileSelectionMode.FileSave
										or FileSelectionMode.FileSaveRoot)
			{
				selectedFileName = inputText?.Text + "";
				result = this.directory + '/' + selectedFileName;
				autoResetEvent?.Set();
			}
			else
			{
				result = this.directory;
				autoResetEvent?.Set();
			}
		});

		dialogBuilder.SetNegativeButton("Cancel", (_, _) => { });
		directoriesDialog = dialogBuilder.Create();

		if (directoriesDialog is not null)
		{
			directoriesDialog.CancelEvent += (_, _) => { autoResetEvent?.Set(); };
			directoriesDialog.DismissEvent += (_, _) => { autoResetEvent?.Set(); };
			autoResetEvent = new AutoResetEvent(false);
			directoriesDialog.Show();
		}

		await Task.Run(() => autoResetEvent?.WaitOne(), cancellationToken);

		return result ?? throw new FileNotFoundException($"Could not locate {directoryFile}");
	}


	static bool CreateSubDirectory(string newDirectory)
	{
		var newDirectoryFile = new File(newDirectory);
		return !newDirectoryFile.Exists() && newDirectoryFile.Mkdir();
	}

	List<string> GetDirectories(string dir)
	{
		var directories = new List<string>();
		var directoryFile = new File(dir);

		// if directory is not the base sd card directory add ".." for going up one directory
		if ((isRootSelectionMode || !directory.Equals(sdCardDirectory, StringComparison.Ordinal)) &&
			!"/".Equals(directory, StringComparison.Ordinal))
		{
			directories.Add("..");
		}

		if (!directoryFile.Exists() || !directoryFile.IsDirectory)
		{
			return directories;
		}

		foreach (var file in directoryFile.ListFiles() ?? Array.Empty<File>())
		{
			if (file.IsDirectory)
			{
				// Add "/" to directory names to identify them in the list
				directories.Add(file.Name + "/");
			}
			else if (fileSelectionMode is FileSelectionMode.FileOpen
											or FileSelectionMode.FileOpenRoot
											or FileSelectionMode.FileSave
											or FileSelectionMode.FileSaveRoot)
			{
				// Add file names to the list if we are doing a file save or file open operation
				directories.Add(file.Name);
			}
		}

		directories.Sort();
		return directories;
	}

	// START DIALOG DEFINITION
	AlertDialog.Builder CreateDirectoryChooserDialog(string title,
		List<string> listItems,
		EventHandler<DialogClickEventArgs> onClickListener,
		string defaultFileName = "default")
	{
		var dialogBuilder = new AlertDialog.Builder(context);

		// Create title text showing file select type 
		titleView1 = new TextView(context)
		{
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
			Text = fileSelectionMode switch
			{
				FileSelectionMode.FileOpen or FileSelectionMode.FileOpenRoot => "Open",
				FileSelectionMode.FileSave or FileSelectionMode.FileSaveRoot => "Save As",
				FileSelectionMode.FolderChoose or FileSelectionMode.FolderChooseRoot => "Select Folder",
				_ => throw new NotSupportedException($"{fileSelectionMode} is not yet supported")
			},

			Gravity = GravityFlags.CenterVertical
		};

		titleView1.SetTextColor(Color.Black);
		titleView1.SetTextSize(ComplexUnitType.Dip, 18);
		titleView1.SetTypeface(null, TypefaceStyle.Bold);

		// Create custom view for AlertDialog title
		var titleLayout1 = new LinearLayout(context)
		{
			Orientation = Orientation.Vertical
		};
		titleLayout1.AddView(titleView1);

		if (fileSelectionMode is FileSelectionMode.FileSave)
		{
			// Create New Folder Button
			var newDirButton = new Button(context)
			{
				LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
				Text = "New Folder"
			};

			newDirButton.Click += (_, _) =>
			{
				var input = new EditText(context);
				new AlertDialog.Builder(context).SetTitle("New Folder Name")
					?.SetView(input)
					?.SetPositiveButton("OK", (_, _) =>
					{
						var newDirName = input.Text;
						// Create new directory
						if (CreateSubDirectory(directory + "/" + newDirName))
						{
							// Navigate into the new directory
							directory += "/" + newDirName;
							UpdateDirectory(listItems);
						}
						else
						{
							Toast.MakeText(context,
									"Failed to create '" + newDirName + "' folder",
									ToastLength.Short)
								?.Show();
						}
					})
					?.SetNegativeButton("Cancel", (_, _) => { })
					?.Show();
			};
			titleLayout1.AddView(newDirButton);
		}

		// Create View with folder path and entry text box
		var titleLayout = new LinearLayout(context)
		{
			Orientation = Orientation.Vertical
		};


		var currentSelection = new TextView(context)
		{
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
			Gravity = GravityFlags.CenterVertical,
			Text = "Current selection:"
		};
		currentSelection.SetTextColor(Color.Black);
		currentSelection.SetTextSize(ComplexUnitType.Dip, 12);
		currentSelection.SetTypeface(null, TypefaceStyle.Bold);

		titleLayout.AddView(currentSelection);

		titleView = new TextView(context)
		{
			Gravity = GravityFlags.CenterVertical,
			Text = title,
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
		};
		titleView.SetTextColor(Color.Black);
		titleView.SetTextSize(ComplexUnitType.Dip, 10);
		titleView.SetTypeface(null, TypefaceStyle.Normal);

		titleLayout.AddView(titleView);

		if (fileSelectionMode is FileSelectionMode.FileOpen
									or FileSelectionMode.FileOpenRoot
									or FileSelectionMode.FileSave
									or FileSelectionMode.FileSaveRoot)
		{
			inputText = new EditText(context)
			{
				Text = defaultFileName
			};

			titleLayout.AddView(inputText);
		}

		// Set Views and Finish Dialog builder
		dialogBuilder.SetView(titleLayout);
		dialogBuilder.SetCustomTitle(titleLayout1);
		listAdapter = CreateListAdapter(listItems);
		dialogBuilder.SetSingleChoiceItems(listAdapter, -1, onClickListener);
		dialogBuilder.SetCancelable(true);

		return dialogBuilder;
	}

	void UpdateDirectory(in List<string> subDirectories)
	{
		subDirectories.Clear();
		subDirectories.AddRange(GetDirectories(directory));

		if (titleView is not null)
		{
			titleView.Text = directory;
		}

		if (directoriesDialog?.ListView is not null)
		{
			directoriesDialog.ListView.Adapter = null;
			directoriesDialog.ListView.Adapter = CreateListAdapter(subDirectories);
		}

		if (inputText is not null
			&& fileSelectionMode is FileSelectionMode.FileOpen
									or FileSelectionMode.FileOpenRoot
									or FileSelectionMode.FileSave
									or FileSelectionMode.FileSaveRoot)
		{
			inputText.Text = selectedFileName;
		}
	}

	ArrayAdapter<string> CreateListAdapter(List<string>? items)
	{
		var adapter = new SimpleArrayAdapter(context ?? throw new InvalidOperationException(),
			Android.Resource.Layout.SelectDialogItem,
			Android.Resource.Id.Text1, items ?? Array.Empty<string>().ToList());
		return adapter;
	}

	class SimpleArrayAdapter : ArrayAdapter<string>
	{
		public SimpleArrayAdapter(Context context, int resource, int textViewResourceId, IList<string> objects) : base(
			context, resource, textViewResourceId, objects)
		{
		}

		public override View GetView(int position, View? convertView, ViewGroup parent)
		{
			var v = base.GetView(position, convertView, parent);
			if (v is TextView textView)
			{
				// Enable list item (directory) text wrapping
				if (textView.LayoutParameters != null)
				{
					textView.LayoutParameters.Height = ViewGroup.LayoutParams.WrapContent;
				}

				textView.Ellipsize = null;
			}

			return v;
		}
	}
}