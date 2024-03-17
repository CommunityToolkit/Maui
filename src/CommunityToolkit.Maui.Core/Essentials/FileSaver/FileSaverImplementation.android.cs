using System.Buffers;
using System.Web;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Webkit;
using CommunityToolkit.Maui.Core.Essentials;
using CommunityToolkit.Maui.Core.Extensions;
using Java.IO;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;
using Trace = System.Diagnostics.Trace;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FileSaverImplementation : IFileSaver
{
	static async Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26) && !string.IsNullOrEmpty(initialPath))
		{
			Trace.WriteLine("Specifying an initial path is only supported on Android 26 and later.");
		}

		AndroidUri? filePath = null;

		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			var status = await Permissions.RequestAsync<Permissions.StorageWrite>().WaitAsync(cancellationToken).ConfigureAwait(false);
			if (status is not PermissionStatus.Granted)
			{
				throw new PermissionException("Storage permission is not granted.");
			}
		}

		if (Android.OS.Environment.ExternalStorageDirectory is not null)
		{
			initialPath = initialPath.Replace(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, string.Empty, StringComparison.InvariantCulture);
		}

		var initialFolderUri = AndroidUri.Parse(AndroidStorageConstants.ExternalStorageBaseUrl + HttpUtility.UrlEncode(initialPath));
		var intent = new Intent(Intent.ActionCreateDocument);

		intent.AddCategory(Intent.CategoryOpenable);
		intent.SetType(MimeTypeMap.Singleton?.GetMimeTypeFromExtension(MimeTypeMap.GetFileExtensionFromUrl(fileName)) ?? "*/*");
		intent.PutExtra(Intent.ExtraTitle, fileName);
		intent.PutExtra(DocumentsContract.ExtraInitialUri, initialFolderUri);

		await IntermediateActivity.StartAsync(intent, (int)AndroidRequestCode.RequestCodeSaveFilePicker, onResult: OnResult).WaitAsync(cancellationToken).ConfigureAwait(false);

		if (filePath is null)
		{
			throw new FileSaveException("Path doesn't exist.");
		}

		return await SaveDocument(filePath, stream, progress, cancellationToken).ConfigureAwait(false);

		void OnResult(Intent resultIntent)
		{
			filePath = EnsurePhysicalPath(resultIntent.Data);
		}
	}

	static Task<string> InternalSaveAsync(string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		return InternalSaveAsync(AndroidPathExtensions.GetExternalDirectory(), fileName, stream, progress, cancellationToken);
	}

	static AndroidUri EnsurePhysicalPath(AndroidUri? uri)
	{
		if (uri is null)
		{
			throw new FileSaveException("Path is not selected.");
		}

		const string uriSchemeFolder = "content";
		if (uri.Scheme is not null && uri.Scheme.Equals(uriSchemeFolder, StringComparison.OrdinalIgnoreCase))
		{
			return uri;
		}

		throw new FileSaveException($"Unable to resolve absolute path or retrieve contents of URI '{uri}'.");
	}

	static async Task<string> SaveDocument(AndroidUri uri, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		if (stream.CanSeek)
		{
			stream.Seek(0, SeekOrigin.Begin);
		}

		using var parcelFileDescriptor = Application.Context.ContentResolver?.OpenFileDescriptor(uri, "wt");
		using var fileOutputStream = new ParcelFileDescriptor.AutoCloseOutputStream(parcelFileDescriptor);
		var buffer = ArrayPool<byte>.Shared.Rent(4096);

		try
		{
			int bytesRead;
			long totalRead = 0;
			while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
			{
				await fileOutputStream.WriteAsync(buffer, 0, bytesRead).WaitAsync(cancellationToken).ConfigureAwait(false);
				totalRead += bytesRead;
				progress?.Report(totalRead / stream.Length);
			}

			if (fileOutputStream.Channel is not null)
			{
				await fileOutputStream.Channel.TruncateAsync(totalRead).WaitAsync(cancellationToken).ConfigureAwait(false);
			}
		}
		finally
		{
			progress?.Report(100);
			ArrayPool<byte>.Shared.Return(buffer);
		}

		return uri.ToPhysicalPath() ?? throw new FileSaveException($"Unable to resolve absolute path where the file was saved '{uri}'.");
	}
}