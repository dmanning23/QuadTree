using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Xna.Framework;

namespace QuadTreeLib
{
	internal class QuadNode<T>
	{
		#region Properties

		/// <summary>
		/// The node that this guy is inside of
		/// </summary>
		public QuadNode<T> Parent { get; internal set; }

		/// <summary>
		/// The four child nodes of this dude
		/// </summary>
		private QuadNode<T>[] _nodes = new QuadNode<T>[4];

		/// <summary>
		/// Get one of the child nodes via the [] operator
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		internal QuadNode<T> this[EDirection direction]
		{
			get
			{
				return _nodes[(int)direction];
				
			}
			set
			{
				//hold onto the node
				_nodes[(int)direction] = value;
				
				//set the node's parent to this dude
				if (value != null)
				{
					value.Parent = this;
				}
			}
		}

		public ReadOnlyCollection<QuadNode<T>> Nodes;

		internal List<T> quadObjects = new List<T>();

		public ReadOnlyCollection<T> Objects;

		/// <summary>
		/// The size and location of this node
		/// </summary>
		public Rectangle Bounds { get; internal set; }

		#endregion //Properties

		#region Methods

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bounds"></param>
		public QuadNode(Rectangle bounds)
		{
			Bounds = bounds;
			Nodes = new ReadOnlyCollection<QuadNode<T>>(_nodes);
			Objects = new ReadOnlyCollection<T>(quadObjects);
		}

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public QuadNode(int x, int y, int width, int height)
			: this(new Rectangle(x, y, width, height))
		{
		}

		/// <summary>
		/// whether or not there are any child nodes of this dude
		/// </summary>
		/// <returns></returns>
		public bool HasChildNodes()
		{
			return _nodes[0] != null;
		}

		#endregion //Methods
	}
}