using CoreFoundation;

namespace CommunityToolkit.Maui.Storage;

// iOS 18 throws NSInternalInconsistencyException ("deallocated host proxy") when the picker's view service, still answering the
// picker leaving the screen, messages a picker that is already deallocated; iOS 26 only logs it.
static class DocumentPickerRelease
{
	static readonly TimeSpan checkInterval = TimeSpan.FromMilliseconds(500);
	// A margin over the sub-second gaps measured between the picker leaving the screen and the view service's last message
	static readonly TimeSpan viewServiceSettleTime = TimeSpan.FromSeconds(3);

	public static void DisposeOnceTheViewServiceIsDone(UIDocumentPickerViewController picker) => CheckLater(picker, TimeSpan.Zero);

	static void CheckLater(UIDocumentPickerViewController picker, TimeSpan offScreenFor) =>
		DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, checkInterval), () =>
		{
			var stillOffScreenFor = picker.PresentingViewController is null ? offScreenFor + checkInterval : TimeSpan.Zero;
			if (stillOffScreenFor >= viewServiceSettleTime)
			{
				picker.Dispose();
			}
			else
			{
				CheckLater(picker, stillOffScreenFor);
			}
		});
}
