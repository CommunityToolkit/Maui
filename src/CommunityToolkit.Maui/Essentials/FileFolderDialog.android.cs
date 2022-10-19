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

namespace CommunityToolkit.Maui.Essentials;

class FileFolderDialog : IDisposable
{
	public enum FileSelectionMode
	{
		FileOpen,
		FileSave,
		FolderChoose,
		FileOpenRoot,
		FileSaveRoot,
		FolderChooseRoot
	}

	const int fileOpen = 0;
	const int fileSave = 1;
	const int folderChoose = 2;
	readonly Context? context;
	readonly bool goToUpper;
	readonly string? sdCardDirectory;
	readonly int selectType;
	AutoResetEvent? autoResetEvent;
	AlertDialog? dirsDialog;
	EditText? inputText;

	string mDir = "";
	ArrayAdapter<string>? listAdapter;
	List<string>? subDirectories;
	TextView? titleView;
	TextView? titleView1;
	string? result;
	string selectedFileName;

	//////////////////////////////////////////////////////
	// Callback interface for selected directory
	//////////////////////////////////////////////////////

	public FileFolderDialog(Context? context, FileSelectionMode mode, string? fileName = "default", string? fileExtension = ".png")
	{
		selectedFileName = fileName + fileExtension;
		switch (mode)
		{
			case FileSelectionMode.FileOpen:
				selectType = fileOpen;
				break;
			case FileSelectionMode.FileSave:
				selectType = fileSave;
				break;
			case FileSelectionMode.FolderChoose:
				selectType = folderChoose;
				break;
			case FileSelectionMode.FileOpenRoot:
				selectType = fileOpen;
				goToUpper = true;
				break;
			case FileSelectionMode.FileSaveRoot:
				selectType = fileSave;
				goToUpper = true;
				break;
			case FileSelectionMode.FolderChooseRoot:
				selectType = folderChoose;
				goToUpper = true;
				break;
			default:
				selectType = fileOpen;
				break;
		}
		
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

	public async Task<string?> GetFileOrDirectoryAsync(string? dir, string defaultFileName = "default")
	{
		if (dir is null)
		{
			return null;
		}

		var dirFile = new File(dir);
		while (!dirFile.Exists() || !dirFile.IsDirectory)
		{
			dir = dirFile.Parent;
			if (dir == null)
			{
				return null;
			}

			dirFile = new File(dir);
			Log.Debug("~~~~~", "dir=" + dir);
		}

		Log.Debug("~~~~~", "dir=" + dir);
		try
		{
			dir = new File(dir).CanonicalPath;
		}
		catch (IOException)
		{
			return result;
		}

		mDir = dir;
		subDirectories = GetDirectories(dir);

		var dialogBuilder = CreateDirectoryChooserDialog(dir, subDirectories, (sender, args) =>
		{
			var mDirOld = mDir;
			var sel = "" + ((AlertDialog?)sender)?.ListView?.Adapter?.GetItem(args.Which);
			if (sel[^1] == '/')
			{
				sel = sel.Substring(0, sel.Length - 1);
			}

			// Navigate into the sub-directory
			if (sel.Equals("..", StringComparison.Ordinal))
			{
				mDir = mDir.Substring(0, mDir.LastIndexOf("/", StringComparison.Ordinal));
				if ("".Equals(mDir, StringComparison.Ordinal))
				{
					mDir = "/";
				}
			}
			else
			{
				mDir += "/" + sel;
			}

			selectedFileName = defaultFileName;

			if (new File(mDir).IsFile) // If the selection is a regular file
			{
				mDir = mDirOld;
				selectedFileName = sel;
			}

			UpdateDirectory();
		});
		dialogBuilder.SetPositiveButton("OK", (_, _) =>
		{
			// Current directory chosen
			// Call registered listener supplied with the chosen directory

			{
				if (selectType is fileOpen or fileSave)
				{
					selectedFileName = inputText?.Text + "";
					result = mDir + "/" + selectedFileName;
					autoResetEvent?.Set();
				}
				else
				{
					result = mDir;
					autoResetEvent?.Set();
				}
			}
		});
		dialogBuilder.SetNegativeButton("Cancel", (_, _) => { });
		dirsDialog = dialogBuilder.Create();

		if (dirsDialog != null)
		{
			dirsDialog.CancelEvent += (_, _) => { autoResetEvent?.Set(); };
			dirsDialog.DismissEvent += (_, _) => { autoResetEvent?.Set(); };
			// Show directory chooser dialog
			autoResetEvent = new AutoResetEvent(false);
			dirsDialog.Show();
		}

		await Task.Run(() => { autoResetEvent?.WaitOne(); });

		return result;
	}


	static bool CreateSubDir(string newDir)
	{
		var newDirFile = new File(newDir);
		return !newDirFile.Exists() && newDirFile.Mkdir();
	}

	List<string> GetDirectories(string dir)
	{
		var dirs = new List<string>();
		try
		{
			var dirFile = new File(dir);

			// if directory is not the base sd card directory add ".." for going up one directory
			if ((goToUpper || !mDir.Equals(sdCardDirectory, StringComparison.Ordinal)) &&
			    !"/".Equals(mDir, StringComparison.Ordinal))
			{
				dirs.Add("..");
			}

			Log.Debug("~~~~", "m_dir=" + mDir);
			if (!dirFile.Exists() || !dirFile.IsDirectory)
			{
				return dirs;
			}

			foreach (var file in dirFile.ListFiles() ?? Array.Empty<File>())
			{
				if (file.IsDirectory)
				{
					// Add "/" to directory names to identify them in the list
					dirs.Add(file.Name + "/");
				}
				else if (selectType is fileSave or fileOpen)
				{
					// Add file names to the list if we are doing a file save or file open operation
					dirs.Add(file.Name);
				}
			}
		}
		catch (Exception)
		{
			// ignored
		}

		dirs.Sort();
		return dirs;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////                                   START DIALOG DEFINITION                                    //////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	AlertDialog.Builder CreateDirectoryChooserDialog(string title,
		List<string> listItems,
		EventHandler<DialogClickEventArgs> onClickListener, 
		string defaultFileName = "default")
	{
		var dialogBuilder = new AlertDialog.Builder(context);
		////////////////////////////////////////////////
		// Create title text showing file select type // 
		////////////////////////////////////////////////
		titleView1 = new TextView(context);
		titleView1.LayoutParameters =
			new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
		//m_titleView1.setTextAppearance(m_context, android.R.style.TextAppearance_Large);
		//m_titleView1.setTextColor( m_context.getResources().getColor(android.R.color.black) );

		if (selectType == fileOpen)
		{
			titleView1.Text = "Open";
		}

		if (selectType == fileSave)
		{
			titleView1.Text = "Save As";
		}

		if (selectType == folderChoose)
		{
			titleView1.Text = "Select folder";
		}

		//need to make this a variable Save as, Open, Select Directory
		titleView1.Gravity = GravityFlags.CenterVertical;
		//_mTitleView1.SetBackgroundColor(Color.DarkGray); // dark gray 	-12303292
		titleView1.SetTextColor(Color.Black);
		titleView1.SetTextSize(ComplexUnitType.Dip, 18);
		titleView1.SetTypeface(null, TypefaceStyle.Bold);
		// Create custom view for AlertDialog title
		var titleLayout1 = new LinearLayout(context);
		titleLayout1.Orientation = Orientation.Vertical;
		titleLayout1.AddView(titleView1);

		if (selectType == fileSave)
		{
			///////////////////////////////
			// Create New Folder Button  //
			///////////////////////////////
			var newDirButton = new Button(context);
			newDirButton.LayoutParameters =
				new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
			newDirButton.Text = "New Folder";
			newDirButton.Click += (_, _) =>
			{
				var input = new EditText(context);
				new AlertDialog.Builder(context).SetTitle("New Folder Name")
					?.SetView(input)
					?.SetPositiveButton("OK", (_, _) =>
					{
						var newDirName = input.Text;
						// Create new directory
						if (CreateSubDir(mDir + "/" + newDirName))
						{
							// Navigate into the new directory
							mDir += "/" + newDirName;
							UpdateDirectory();
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

		/////////////////////////////////////////////////////
		// Create View with folder path and entry text box // 
		/////////////////////////////////////////////////////
		var titleLayout = new LinearLayout(context);
		titleLayout.Orientation = Orientation.Vertical;


		var currentSelection = new TextView(context);
		currentSelection.LayoutParameters =
			new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
		currentSelection.SetTextColor(Color.Black);
		currentSelection.Gravity = GravityFlags.CenterVertical;
		currentSelection.Text = "Current selection:";
		currentSelection.SetTextSize(ComplexUnitType.Dip, 12);
		currentSelection.SetTypeface(null, TypefaceStyle.Bold);

		titleLayout.AddView(currentSelection);

		titleView = new TextView(context);
		titleView.LayoutParameters =
			new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
		titleView.SetTextColor(Color.Black);
		titleView.Gravity = GravityFlags.CenterVertical;
		titleView.Text = title;
		titleView.SetTextSize(ComplexUnitType.Dip, 10);
		titleView.SetTypeface(null, TypefaceStyle.Normal);

		titleLayout.AddView(titleView);

		if (selectType is fileOpen or fileSave)
		{
			inputText = new EditText(context);
			inputText.Text = defaultFileName;
			titleLayout.AddView(inputText);
		}

		//////////////////////////////////////////
		// Set Views and Finish Dialog builder  //
		//////////////////////////////////////////
		dialogBuilder.SetView(titleLayout);
		dialogBuilder.SetCustomTitle(titleLayout1);
		listAdapter = CreateListAdapter(listItems);
		dialogBuilder.SetSingleChoiceItems(listAdapter, -1, onClickListener);
		dialogBuilder.SetCancelable(true);
		return dialogBuilder;
	}


	void UpdateDirectory()
	{
		subDirectories?.Clear();
		subDirectories?.AddRange(GetDirectories(mDir));
		if (titleView != null)
		{
			titleView.Text = mDir;
		}

		if (dirsDialog?.ListView != null)
		{
			dirsDialog.ListView.Adapter = null;
			dirsDialog.ListView.Adapter = CreateListAdapter(subDirectories);
		}

		//#scorch
		if (selectType is fileSave or fileOpen && inputText != null)
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