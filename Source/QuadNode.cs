using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuadTreeLib
{
	internal class QuadNode
	{
		private static int _id = 0;
		public readonly int ID = _id++;

		public QuadNode Parent { get; internal set; }

		private QuadNode[] _nodes = new QuadNode[4];
		public QuadNode this[Direction direction]
		{
			get
			{
				switch (direction)
				{
					case Direction.NW:
						return _nodes[0];
					case Direction.NE:
						return _nodes[1];
					case Direction.SW:
						return _nodes[2];
					case Direction.SE:
						return _nodes[3];
					default:
						return null;
				}
			}
			set
			{
				switch (direction)
				{
					case Direction.NW:
						_nodes[0] = value;
						break;
					case Direction.NE:
						_nodes[1] = value;
						break;
					case Direction.SW:
						_nodes[2] = value;
						break;
					case Direction.SE:
						_nodes[3] = value;
						break;
				}
				if (value != null)
					value.Parent = this;
			}
		}

		public ReadOnlyCollection<QuadNode> Nodes;

		internal List<T> quadObjects = new List<T>();
		public ReadOnlyCollection<T> Objects;

		public Rect Bounds { get; internal set; }

		public bool HasChildNodes()
		{
			return _nodes[0] != null;
		}

		public QuadNode(Rect bounds)
		{
			Bounds = bounds;
			Nodes = new ReadOnlyCollection<QuadNode>(_nodes);
			Objects = new ReadOnlyCollection<T>(quadObjects);
		}

		public QuadNode(double x, double y, double width, double height)
			: this(new Rect(x, y, width, height))
		{

		}
	}
}