using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Primitives;

/// <summary>
/// 
/// </summary>
public class GridEventArgs : EventArgs
{
	/// <summary>
	/// 
	/// </summary>
	public Microsoft.UI.Xaml.Controls.Grid Grid { get; }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="grid"></param>
	public GridEventArgs(Microsoft.UI.Xaml.Controls.Grid grid)
	{
		Grid = grid;
	}
}
