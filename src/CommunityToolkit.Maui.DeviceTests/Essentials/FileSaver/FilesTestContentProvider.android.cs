using Android.Content;
using Android.Database;
using Android.OS;
using AndroidApplication = Android.App.Application;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.DeviceTests;

// The document URIs have the shape of the external storage provider's (content://authority/document/primary%3Apath%2Fto%2Ffile), so the
// toolkit's ToPhysicalPath() resolves them. Every descriptor the provider hands out reports its closing back to it, which is how a test sees
// whether its client closed it, as a provider that commits on close would.
[ContentProvider([FilesTestContentProvider.Authority], Name = FilesTestContentProvider.JavaName, Exported = false, GrantUriPermissions = false)]
public sealed class FilesTestContentProvider : ContentProvider
{
	public const string Authority = "com.communitytoolkit.maui.devicetests.files";
	public const string JavaName = "com.communitytoolkit.maui.devicetests.files.FilesTestContentProvider";

	public const string NullDescriptorPrefix = "null-descriptor:";

	// Models a provider whose document cannot be written: it opens read-only whatever mode is asked for, so the backing file must exist.
	public const string ReadOnlyDescriptorPrefix = "read-only-descriptor:";

	static readonly Lock gate = new();
	static readonly List<string> openModes = [];
	static readonly List<string> closeErrors = [];
	static int generation;
	static int cleanCloses;
	static int failedCloses;

	public static bool IgnoreTruncateFlag { get; set; }

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

	public static int CleanCloseCount => Volatile.Read(ref cleanCloses);

	// Includes a descriptor that was finalized without being closed (leaked), which the close listener reports as an error too.
	public static int FailedCloseCount => Volatile.Read(ref failedCloses);

	// A client's CloseWithError(message) reports that message, which tells its close apart from a descriptor that was leaked.
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

	public static AndroidUri BuildDocumentUri(string documentId) =>
		AndroidUri.Parse($"content://{Authority}/document/{AndroidUri.Encode(documentId)}") ?? throw new InvalidOperationException("Uri.Parse returned null.");

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