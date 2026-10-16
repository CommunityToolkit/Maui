using Android.Content;
using Android.Database;
using Android.OS;
using AndroidApplication = Android.App.Application;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// A document provider that the test app owns, standing in for the one that the user picks a destination from. It serves the file
/// descriptors that FileSaver opens with <c>ContentResolver.OpenFileDescriptor(uri, "wt")</c>, from files under the app's cache
/// folder. Its URIs have the shape of the external storage provider's
/// (<c>content://authority/document/primary%3Apath%2Fto%2Ffile</c>), so the toolkit's <c>ToPhysicalPath()</c> resolves them.
/// Every descriptor it hands out reports its closing to the provider (<c>ParcelFileDescriptor.Open</c> with an
/// <c>OnCloseListener</c>), which is how a test sees whether its client closed it, as a provider that commits on close would.
/// </summary>
[ContentProvider([FilesTestContentProvider.Authority], Name = FilesTestContentProvider.JavaName, Exported = false, GrantUriPermissions = false)]
public sealed class FilesTestContentProvider : ContentProvider
{
	public const string Authority = "com.communitytoolkit.maui.devicetests.files";
	public const string JavaName = "com.communitytoolkit.maui.devicetests.files.FilesTestContentProvider";

	/// <summary>A document id with this prefix opens as a null descriptor (a provider that has no file for the document).</summary>
	public const string NullDescriptorPrefix = "null-descriptor:";

	/// <summary>
	/// A document id with this prefix opens read-only whatever mode is asked for (a provider whose document cannot be written): its
	/// backing file must exist, and writing to the descriptor fails.
	/// </summary>
	public const string ReadOnlyDescriptorPrefix = "read-only-descriptor:";

	static readonly Lock gate = new();
	static readonly List<string> openModes = [];
	static readonly List<string> closeErrors = [];
	static int generation;
	static int cleanCloses;
	static int failedCloses;

	/// <summary>When true the provider opens "wt" as "rw": a provider that ignores the truncate flag.</summary>
	public static bool IgnoreTruncateFlag { get; set; }

	/// <summary>The modes that <c>OpenFile</c> was called with since <see cref="Reset"/>.</summary>
	public static IReadOnlyList<string> OpenModes
	{
		get
		{
			lock (gate)
			{
				return [.. openModes];
			}
		}
	}

	static string RootFolder => Path.Combine(AndroidApplication.Context.CacheDir?.AbsolutePath ?? throw new InvalidOperationException("The app has no cache folder."), "files-tests");

	/// <summary>
	/// How many descriptors handed out since <see cref="Reset"/> their client closed (the close listener reported no error). A
	/// descriptor that is never closed and is finalized by the garbage collector counts in <see cref="FailedCloseCount"/> instead.
	/// </summary>
	public static int CleanCloseCount => Volatile.Read(ref cleanCloses);

	/// <summary>
	/// How many descriptors handed out since <see cref="Reset"/> were reported closed with an error: closed by their client with
	/// <c>ParcelFileDescriptor.CloseWithError</c>, or finalized without being closed (leaked).
	/// </summary>
	public static int FailedCloseCount => Volatile.Read(ref failedCloses);

	/// <summary>
	/// The message of each error that a close reported since <see cref="Reset"/>. The one of a client's <c>CloseWithError(message)</c>
	/// holds that message, which tells it apart from a descriptor that was leaked.
	/// </summary>
	public static IReadOnlyList<string> CloseErrors
	{
		get
		{
			lock (gate)
			{
				return [.. closeErrors];
			}
		}
	}

	public static void Reset()
	{
		lock (gate)
		{
			openModes.Clear();
			closeErrors.Clear();
			generation++;
			cleanCloses = 0;
			failedCloses = 0;
		}

		IgnoreTruncateFlag = false;
		if (Directory.Exists(RootFolder))
		{
			Directory.Delete(RootFolder, recursive: true);
		}
	}

	/// <summary>The URI of a document of this provider: <c>content://authority/document/{documentId}</c> (percent-encoded).</summary>
	public static AndroidUri BuildDocumentUri(string documentId) =>
		AndroidUri.Parse($"content://{Authority}/document/{AndroidUri.Encode(documentId)}") ?? throw new InvalidOperationException("Uri.Parse returned null.");

	/// <summary>The file that backs the document with this id.</summary>
	public static string GetBackingFile(string documentId) => Path.Combine(RootFolder, documentId.Replace(':', '/'));

	public override bool OnCreate() => true;

	public override ParcelFileDescriptor? OpenFile(AndroidUri uri, string mode)
	{
		var segments = uri.PathSegments;
		if (segments is null || segments.Count < 2 || segments[1] is not string documentId)
		{
			throw new Java.IO.FileNotFoundException($"Not a document URI of this provider: {uri}");
		}

		lock (gate)
		{
			openModes.Add(mode);
		}

		if (documentId.StartsWith(NullDescriptorPrefix, StringComparison.Ordinal))
		{
			return null;
		}

		var file = GetBackingFile(documentId);
		Directory.CreateDirectory(Path.GetDirectoryName(file)!);

		var effectiveMode = documentId.StartsWith(ReadOnlyDescriptorPrefix, StringComparison.Ordinal)
			? "r"
			: IgnoreTruncateFlag && mode.Contains('t', StringComparison.Ordinal) ? "rw" : mode;
		int openedIn;
		lock (gate)
		{
			openedIn = generation;
		}

		return ParcelFileDescriptor.Open(new Java.IO.File(file), ParcelFileDescriptor.ParseMode(effectiveMode), new Handler(Looper.MainLooper!), new CloseListener(openedIn));
	}

	public override string? GetType(AndroidUri uri) => "application/octet-stream";

	public override ICursor? Query(AndroidUri uri, string[]? projection, string? selection, string[]? selectionArgs, string? sortOrder) => null;

	public override AndroidUri? Insert(AndroidUri uri, ContentValues? values) => null;

	public override int Delete(AndroidUri uri, string? selection, string[]? selectionArgs) => 0;

	public override int Update(AndroidUri uri, ContentValues? values, string? selection, string[]? selectionArgs) => 0;

	// Called on the main thread when the client closes the descriptor (no error), closes it with an error, or when it is finalized without
	// being closed (error).
	// Only closes of descriptors opened since the last Reset count, so a late finalization from an earlier test changes nothing.
	sealed class CloseListener(int openedIn) : Java.Lang.Object, ParcelFileDescriptor.IOnCloseListener
	{
		public void OnClose(Java.IO.IOException? e)
		{
			lock (gate)
			{
				if (openedIn != generation)
				{
					return;
				}

				if (e is null)
				{
					cleanCloses++;
				}
				else
				{
					failedCloses++;
					closeErrors.Add(e.Message ?? e.ToString());
				}
			}
		}
	}
}