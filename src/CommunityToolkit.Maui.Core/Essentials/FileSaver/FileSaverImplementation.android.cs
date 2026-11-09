using System.Buffers;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Webkit;
using CommunityToolkit.Maui.Core.Essentials;
using CommunityToolkit.Maui.Core.Extensions;
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

		var intent = new Intent(Intent.ActionCreateDocument);

		intent.AddCategory(Intent.CategoryOpenable);
		// Not MimeTypeMap.GetFileExtensionFromUrl: it finds no extension in a file name with a space or a non-ASCII letter.
		intent.SetType(MimeTypeMap.Singleton?.GetMimeTypeFromExtension(Path.GetExtension(fileName).TrimStart('.')) ?? "*/*");
		intent.PutExtra(Intent.ExtraTitle, fileName);

		// No storage volume holds a folder of the app's private storage: the picker then starts where it chooses.
		if (AndroidPathExtensions.ToExternalStorageDocumentUri(initialPath) is AndroidUri initialFolderUri)
		{
			intent.PutExtra(DocumentsContract.ExtraInitialUri, initialFolderUri);
		}

		await ActivityResultManager.StartAsync(intent, AndroidRequestCode.RequestCodeSaveFilePicker, OnResult, cancellationToken).ConfigureAwait(false);

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

		using var parcelFileDescriptor = Application.Context.ContentResolver?.OpenFileDescriptor(uri, "wt")
			?? throw new FileSaveException($"Unable to open the file at URI '{uri}' for writing.");
		using var fileOutputStream = new ParcelFileDescriptor.AutoCloseOutputStream(parcelFileDescriptor);
		var buffer = ArrayPool<byte>.Shared.Rent(4096);

		try
		{
			int bytesRead;
			long totalRead = 0;
			while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
			{
				// A stream that ignores the token still returns the block that it read while the token was canceled
				cancellationToken.ThrowIfCancellationRequested();

				// Awaited to the end: a write that is no longer awaited would go on with the output stream that is disposed below
				await fileOutputStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
				totalRead += bytesRead;

				// The block that reaches the end of the stream is not reported: the save is not complete before the document is truncated,
				// closed and its path resolved, so 100 is only ever the final report
				if (stream.CanSeek && totalRead < stream.Length)
				{
					progress?.Report(100d * totalRead / stream.Length);
				}
			}

			if (fileOutputStream.Channel is not null)
			{
				await fileOutputStream.Channel.TruncateAsync(totalRead).ConfigureAwait(false);
			}

			// A cancellation that came after the check of the last block, while the end of the data was read for example, still cancels the save
			cancellationToken.ThrowIfCancellationRequested();
		}
		catch (Exception e)
		{
			// Closing the descriptor with an error tells the provider that the document was not written, so that a provider that commits a
			// document when it is closed (a cloud provider, for example) does not take the part that was written for the whole document
			try
			{
				parcelFileDescriptor.CloseWithError(e.Message);
			}
			catch (Java.IO.IOException)
			{
				// The save already failed, and its own exception says why
			}

			throw;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}

		// Disposing the .NET peers of the stream and of the descriptor does not close them: closing the stream closes the descriptor,
		// which tells the provider that the document is written
		fileOutputStream.Close();

		// A written document whose file has no path that the provider tells (a document of a cloud provider, a download of another app) is
		// reported by its content URI, which the app can open with the ContentResolver, never as a save that failed
		var path = uri.ToPhysicalPath()
			?? GetPathOfDocumentCreatedInDownloads(uri)
			?? uri.ToString()
			?? throw new FileSaveException($"Unable to resolve absolute path where the file was saved '{uri}'.");

		progress?.Report(100);

		return path;
	}

	// The Downloads provider names a document that it creates in its root by the number of its download, which holds no path: the path of
	// its file follows from the display name and the summary that the provider reports for the document
	static string? GetPathOfDocumentCreatedInDownloads(AndroidUri uri)
	{
		if (!uri.IsDownloadManagerDocument())
		{
			return null;
		}

		try
		{
			using var cursor = Application.Context.ContentResolver?.Query(uri, [DocumentsContract.Document.ColumnDisplayName, DocumentsContract.Document.ColumnSummary], null, null, null);
			return cursor?.MoveToFirst() is true
				? uri.ToPathOfDocumentCreatedInDownloads(cursor.GetString(0), cursor.GetString(1))
				: null;
		}
		catch (Java.Lang.Exception e)
		{
			// The document is written: a provider that cannot describe it leaves only its path unknown
			Trace.WriteLine($"Unable to query the name of the document '{uri}': {e.Message}");
			return null;
		}
	}
}