namespace CommunityToolkit.Maui.MediaElement;

public class StreamMediaSource : MediaSource, IStreamImageSource
{
	readonly object synchandle = new();
	CancellationTokenSource? cancellationTokenSource;

	TaskCompletionSource<bool>? completionSource;

	public static readonly BindableProperty StreamProperty
		= BindableProperty.Create(nameof(Stream), typeof(Func<CancellationToken, Task<Stream>>), typeof(StreamMediaSource));

	protected CancellationTokenSource? CancellationTokenSource
	{
		get => cancellationTokenSource;
		private set
		{
			if (cancellationTokenSource == value)
			{
				return;
			}

			if (cancellationTokenSource != null)
			{
				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
			}

			cancellationTokenSource = value;
		}
	}

	bool IsLoading => cancellationTokenSource != null;

	public virtual Func<CancellationToken, Task<Stream>>? Stream
	{
		get => (Func<CancellationToken, Task<Stream>>?)GetValue(StreamProperty);
		set => SetValue(StreamProperty, value);
	}

	// TODO check length as well?
	public bool IsEmpty => Stream != null;

	protected override void OnPropertyChanged(string propertyName)
	{
		if (propertyName == StreamProperty.PropertyName)
		{
			OnSourceChanged();
		}

		base.OnPropertyChanged(propertyName);
	}

	// TODO
#pragma warning disable CS8616 // Nullability of reference types in return type doesn't match implemented member.
	async Task<Stream?> IStreamImageSource.GetStreamAsync(CancellationToken cancellationToken)
#pragma warning restore CS8616 // Nullability of reference types in return type doesn't match implemented member.
	{
		if (Stream is null)
		{
			return null;
		}

		OnLoadingStarted();

		if (CancellationTokenSource is null)
		{
			throw new InvalidOperationException($"{nameof(OnLoadingStarted)} not called");
		}

		cancellationToken.Register(CancellationTokenSource.Cancel);

		try
		{
			var stream = await Stream(CancellationTokenSource.Token);
			OnLoadingCompleted(false);
			return stream;
		}
		catch (OperationCanceledException)
		{
			OnLoadingCompleted(true);
			throw;
		}
	}

	protected void OnLoadingCompleted(bool cancelled)
	{
		if (!IsLoading || completionSource == null)
		{
			return;
		}

		var tcs = Interlocked.Exchange<TaskCompletionSource<bool>?>(ref completionSource, null);

		if (tcs is not null)
		{
			tcs.SetResult(cancelled);
		}

		lock (synchandle)
		{
			CancellationTokenSource = null;
		}
	}

	protected void OnLoadingStarted()
	{
		lock (synchandle)
		{
			CancellationTokenSource = new CancellationTokenSource();
		}
	}

	public virtual Task<bool> Cancel()
	{
		if (!IsLoading)
		{
			return Task.FromResult(false);
		}

		var tcs = new TaskCompletionSource<bool>();
		var original = Interlocked.CompareExchange(ref completionSource, tcs, null);

		if (original is null)
		{
			CancellationTokenSource = null;
		}
		else
		{
			tcs = original;
		}

		return tcs.Task;
	}
}
