using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Primitives;

/// <summary>
/// Represents the different types of media sources.
/// </summary>
public enum MediaElementSourceType
{
	/// <summary>
	/// The media source is a Video File.
	/// </summary>
	Video,

	/// <summary>
	/// The media source is an Audio File.
	/// </summary>
	Audio,

	/// <summary>
	/// The media source is an unknown type.
	/// </summary>
	Unknown,
}
