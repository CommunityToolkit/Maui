using System.Runtime.InteropServices;

namespace CommunityToolkit.Maui.DeviceTests;

// The file is opened for writing, so only the write itself can fail (a full disk, an I/O error, a file size limit); the tests make a write
// exceed a file size limit of the process.
public partial class FileSaverWriteStreamTests
{
	[Fact]
	public async Task WriteStreamThrowsTheExceptionThatWritingTheFileThrew()
	{
		var path = NewFilePath();
		var content = Enumerable.Range(0, 20_000).Select(i => (byte)(i % 251)).ToArray();
		var stream = new FilesScriptedStream(content);
		using var limit = new FilesFileSizeLimit();
		// The report of the first block limits the size of the files that this process may write to that one block, so the write of the
		// second block fails (EFBIG; a full disk gives ENOSPC). The limit is lifted as soon as WriteStream and the calibration are done.
		var progress = new FilesRecordingProgress { OnReport = _ => limit.Apply(4096) };

		var thrown = await Record.ExceptionAsync(() => WriteStream(stream, path, progress));
		// What FileStream itself throws for a write past the limit (.NET reports EFBIG as an ArgumentOutOfRangeException), while the
		// limit still applies: WriteStream is expected to pass the failure of its write on unchanged.
		var calibration = await Record.ExceptionAsync(async () =>
		{
			await using var other = new FileStream(NewFilePath(), FileMode.CreateNew);
			await other.WriteAsync(new byte[8192]);
		});
		limit.Dispose();

		Assert.True(limit.WasApplied, "The file size limit was never applied.");
		Assert.NotNull(calibration);
		Assert.NotNull(thrown);
		Assert.IsType(calibration.GetType(), thrown);
		Assert.Equal(calibration.Message, thrown.Message);
		Assert.Equal(2, stream.ReadsWithData);
		Assert.Equal(content[..4096], await File.ReadAllBytesAsync(path));
	}

	[Fact]
	public async Task WriteStreamDoesNotReportACompleteCopyWhenWritingTheLastBlockFails()
	{
		var path = NewFilePath();
		var stream = new FilesScriptedStream(sample);
		using var limit = new FilesFileSizeLimit();
		// 10000 bytes: two blocks of 4096 bytes, which the file stream writes at once, then 1808 bytes, which it buffers and only writes when
		// it is disposed. The report of the second block limits the size of the files that this process may write to those two blocks, so
		// that last write fails after the whole stream was read.
		var reports = 0;
		var progress = new FilesRecordingProgress
		{
			OnReport = _ =>
			{
				if (++reports is 2)
				{
					limit.Apply(8192);
				}
			}
		};

		var thrown = await Record.ExceptionAsync(() => WriteStream(stream, path, progress));
		limit.Dispose();

		Assert.True(limit.WasApplied, "The file size limit was never applied.");
		Assert.NotNull(thrown);
		Assert.Equal(3, stream.ReadsWithData);
		Assert.Equal(sample[..8192], await File.ReadAllBytesAsync(path));
		var values = progress.Values;
		Assert.True(values.Count is 0 || values[^1] < 100, $"A failed copy reported {string.Join(", ", values)}.");
	}

	// SIGXFSZ is ignored while the limit is applied, so that a write past the limit fails with EFBIG instead of ending the process. The limit
	// affects every file of the process, so it is applied for as short a time as possible.
	sealed class FilesFileSizeLimit : IDisposable
	{
		const string libSystem = "/usr/lib/libSystem.dylib";
		const int fileSizeResource = 1; // RLIMIT_FSIZE
		const int fileSizeSignal = 25; // SIGXFSZ
		static readonly IntPtr ignore = 1; // SIG_IGN

		readonly Lock gate = new();
		ResourceLimit previousLimit;
		IntPtr previousHandler;
		bool applied;

		public bool WasApplied { get; private set; }

		public void Apply(ulong bytes)
		{
			lock (gate)
			{
				if (applied || WasApplied)
				{
					return;
				}

				if (getrlimit(fileSizeResource, out previousLimit) != 0)
				{
					throw new InvalidOperationException($"getrlimit failed: {Marshal.GetLastPInvokeError()}");
				}

				previousHandler = signal(fileSizeSignal, ignore);
				var limit = previousLimit with { Current = bytes };
				if (setrlimit(fileSizeResource, ref limit) != 0)
				{
					signal(fileSizeSignal, previousHandler);
					throw new InvalidOperationException($"setrlimit failed: {Marshal.GetLastPInvokeError()}");
				}

				applied = true;
				WasApplied = true;
			}
		}

		public void Dispose()
		{
			lock (gate)
			{
				if (!applied)
				{
					return;
				}

				var limit = previousLimit;
				setrlimit(fileSizeResource, ref limit);
				signal(fileSizeSignal, previousHandler);
				applied = false;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		record struct ResourceLimit(ulong Current, ulong Maximum);

		[DllImport(libSystem, SetLastError = true)]
		static extern int getrlimit(int resource, out ResourceLimit limit);

		[DllImport(libSystem, SetLastError = true)]
		static extern int setrlimit(int resource, ref ResourceLimit limit);

		[DllImport(libSystem)]
		static extern IntPtr signal(int signalNumber, IntPtr handler);
	}
}