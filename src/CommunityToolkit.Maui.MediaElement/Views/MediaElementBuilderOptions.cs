using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Views;

// Static class for holding the default MediaElementOptions that the MediaElement constructor can reference
static class MediaElementBuilderOptions
{
	// Options which can be set in by UseCommunityToolkitMediaElement in builder
	public static MediaElementOptions MediaElementOptions = new();
}
