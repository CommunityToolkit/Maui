using Android.Content;
using Android.Webkit;
using Java.IO;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;
using Application = Android.App.Application;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc/>
	public async Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.StorageWrite>().WaitAsync(cancellationToken).ConfigureAwait(false);
		if (status is not PermissionStatus.Granted)
		{
			throw new PermissionException("Storage permission is not granted.");
		}

		var intent = new Intent(Intent.ActionCreateDocument);
		intent.AddCategory(Intent.CategoryOpenable);
		intent.SetType(MimeTypeMap.Singleton?.GetMimeTypeFromExtension(GetExtension(fileName)) ?? "*/*");
		intent.PutExtra(Intent.ExtraTitle, fileName);
		var pickerIntent = Intent.CreateChooser(intent, string.Empty) ?? throw new InvalidOperationException("Unable to create intent.");

		AndroidUri? filePath = null;

		await IntermediateActivity.StartAsync(pickerIntent, (int)AndroidRequestCode.RequestCodeSaveFilePicker, onResult: OnResult).WaitAsync(cancellationToken).ConfigureAwait(false);

		if (filePath is null)
		{
			throw new FileSaveException("Path doesn't exist.");
		}

		return await SaveDocument(filePath, stream, cancellationToken).ConfigureAwait(false);

		void OnResult(Intent resultIntent)
		{
			filePath = EnsurePhysicalPath(resultIntent.Data);
		}
	}

	/// <inheritdoc />
	public Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(GetExternalDirectory(), fileName, stream, cancellationToken);
	}

	static string GetExternalDirectory()
	{
		return Android.OS.Environment.ExternalStorageDirectory?.Path ?? "/storage/emulated/0";
	}

	static AndroidUri EnsurePhysicalPath(AndroidUri? uri)
	{
		if (uri is null)
		{
			throw new FolderPickerException("Path is not selected.");
		}

		const string uriSchemeFolder = "content";
		if (uri.Scheme != null && uri.Scheme.Equals(uriSchemeFolder, StringComparison.OrdinalIgnoreCase))
		{
			return uri;
		}

		throw new FolderPickerException($"Unable to resolve absolute path or retrieve contents of URI '{uri}'.");
	}

	static async Task<string> SaveDocument(AndroidUri uri, Stream stream, CancellationToken cancellationToken)
	{
		var parcelFileDescriptor = Application.Context.ContentResolver?.OpenFileDescriptor(uri, "w");
		var fileOutputStream = new FileOutputStream(parcelFileDescriptor?.FileDescriptor);
		await using var memoryStream = new MemoryStream();

		stream.Seek(0, SeekOrigin.Begin);
		await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
		await fileOutputStream.WriteAsync(memoryStream.ToArray()).WaitAsync(cancellationToken).ConfigureAwait(false);

		fileOutputStream.Close();
		parcelFileDescriptor?.Close();
		var split = uri.Path?.Split(":") ?? throw new FolderPickerException("Unable to resolve path.");

		return Android.OS.Environment.ExternalStorageDirectory + "/" + split[1];
	}
}