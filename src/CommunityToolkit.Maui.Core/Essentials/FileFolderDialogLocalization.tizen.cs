using System;

namespace CommunityToolkit.Maui.Storage;

/// <summary>
/// FileFolderDialog Localization Strings.
/// </summary>
public static class FileFolderDialogLocalization
{
	static string fileSaveAs = "Save As";
	static string saveButton = "Save";
	static string okButton = "OK";
	static string cancelButton = "Cancel";
	static string selectFolder = "Select Folder";
	static string newFolderButton = "New Folder";
	static string newFolderNameTitle = "New Folder Name";
	static string createFolderErrorMessage = "Failed to create new folder";

	/// <summary>
	/// Gets or sets the action label.
	/// </summary>
	public static string FileSaveAs
	{
		get => fileSaveAs;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			fileSaveAs = value;
		}
	}

	/// <summary>
	/// Gets or sets the action button text.
	/// </summary>
	public static string SaveButton
	{
		get => saveButton;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			saveButton = value;
		}
	}

	/// <summary>
	/// Gets or sets the action button text.
	/// </summary>
	public static string OkButton
	{
		get => okButton;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			okButton = value;
		}
	}

	/// <summary>
	/// Gets or sets the cancel button text.
	/// </summary>
	public static string CancelButton
	{
		get => cancelButton;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			cancelButton = value;
		}
	}

	/// <summary>
	/// Gets or sets the action button text.
	/// </summary>
	public static string SelectFolder
	{
		get => selectFolder;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			selectFolder = value;
		}
	}

	/// <summary>
	/// Gets or sets the NewFolder action button text.
	/// </summary>
	public static string NewFolderButton
	{
		get => newFolderButton;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			newFolderButton = value;
		}
	}

	/// <summary>
	/// Gets or sets the NewFolder prompt title.
	/// </summary>
	public static string NewFolderNameTitle
	{
		get => newFolderNameTitle;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			newFolderNameTitle = value;
		}
	}

	/// <summary>
	/// Gets or sets the Create folder error message.
	/// </summary>
	public static string CreateFolderErrorMessage
	{
		get => createFolderErrorMessage;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			createFolderErrorMessage = value;
		}
	}
}