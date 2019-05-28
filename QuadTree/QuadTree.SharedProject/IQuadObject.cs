using System;
using Microsoft.Xna.Framework;

namespace QuadTreeLib
{
	/// <summary>
	/// This is an item that can be added to a quadtree.
	/// </summary>
	public interface IQuadObject
	{
		/// <summary>
		/// the rectanlge around this objetc
		/// </summary>
		Rectangle Bounds { get; }

		/// <summary>
		/// Event that gets called when the location or size of the bounding rect changes.
		/// Used to update the quadtree
		/// </summary>
		event EventHandler BoundsChanged;
	}
}