using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace QuadTreeLib
{
	/// <summary>
	/// the quadtree object
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class QuadTree<T> where T : class, IQuadObject
	{
		#region Members

		/// <summary>
		/// The smallest size a leaf will split into
		/// </summary>
		private readonly Vector2 minLeafSize;

		/// <summary>
		/// Maximum number of objects per leaf before it forces a split into sub quadrants
		/// </summary>
		private readonly int maxObjectsPerLeaf;

		/// <summary>
		/// A dictionary of items -> nodes 
		/// </summary>
		private Dictionary<T, QuadNode<T>> objectToNodeLookup = new Dictionary<T, QuadNode<T>>();

		/// <summary>
		/// The root (top) node of the quadtree
		/// </summary>
		internal QuadNode<T> Root { get; private set; }

		#endregion //Members

		#region Methods

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="minLeafSize">The smallest size a leaf will split into</param>
		/// <param name="maxObjectsPerLeaf">Maximum number of objects per leaf before it forces a split into sub quadrants</param>
		public QuadTree(Vector2 minLeafSize, int maxObjectsPerLeaf)
		{
			this.minLeafSize = minLeafSize;
			this.maxObjectsPerLeaf = maxObjectsPerLeaf;
		}

		/// <summary>
		/// Add an item to the quadtree
		/// </summary>
		/// <param name="quadObject"></param>
		public void Insert(T quadObject)
		{
			var bounds = quadObject.Bounds;
			if (Root == null)
			{
				var rootSize = new Vector2((float)Math.Ceiling(bounds.Width / minLeafSize.X),
										(float)Math.Ceiling(bounds.Height / minLeafSize.Y));
				var multiplier = Math.Max(rootSize.X, rootSize.Y);
				rootSize = new Vector2(minLeafSize.X * multiplier, minLeafSize.Y * multiplier);
				var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
				var rootOrigin = new Point((int)(center.X - rootSize.X / 2), (int)(center.Y - rootSize.Y / 2));
				Root = new QuadNode<T>(new Rectangle(rootOrigin.X, rootOrigin.Y, (int)rootSize.X, (int)rootSize.Y));
			}

			while (!Root.Bounds.Contains(bounds))
			{
				ExpandRoot(bounds);
			}

			InsertNodeObject(Root, quadObject);
		}

		/// <summary>
		/// Given a qurey rectangle, get all the items that should be checked.
		/// </summary>
		/// <param name="bounds"></param>
		/// <returns></returns>
		public List<T> Query(Rectangle bounds)
		{
			var results = new List<T>();
			if (Root != null)
			{
				Query(bounds, Root, results);
			}
			return results;
		}

		#region Private Methods

		private void Query(Rectangle bounds, QuadNode<T> node, List<T> results)
		{
			if (node == null) return;

			if (bounds.Intersects(node.Bounds))
			{
				foreach (T quadObject in node.Objects)
				{
					if (bounds.Intersects(quadObject.Bounds))
						results.Add(quadObject);
				}

				foreach (QuadNode<T> childNode in node.Nodes)
				{
					Query(bounds, childNode, results);
				}
			}
		}

		private void ExpandRoot(Rectangle newChildBounds)
		{
			var isNorth = Root.Bounds.Y < newChildBounds.Y;
			var isWest = Root.Bounds.X < newChildBounds.X;

			EDirection rootDirection;
			if (isNorth)
			{
				rootDirection = isWest ? EDirection.NW : EDirection.NE;
			}
			else
			{
				rootDirection = isWest ? EDirection.SW : EDirection.SE;
			}

			var newX = (rootDirection == EDirection.NW || rootDirection == EDirection.SW)
							  ? Root.Bounds.X
							  : Root.Bounds.X - Root.Bounds.Width;
			var newY = (rootDirection == EDirection.NW || rootDirection == EDirection.NE)
							  ? Root.Bounds.Y
							  : Root.Bounds.Y - Root.Bounds.Height;
			var newRootBounds = new Rectangle(newX, newY, Root.Bounds.Width * 2, Root.Bounds.Height * 2);
			QuadNode<T> newRoot = new QuadNode<T>(newRootBounds);
			SetupChildNodes(newRoot);
			newRoot[rootDirection] = Root;
			Root = newRoot;
		}

		private void InsertNodeObject(QuadNode<T> node, T quadObject)
		{
			if (!node.Bounds.Contains(quadObject.Bounds))
			{
				throw new Exception("This should not happen, child does not fit within node bounds");
			}

			if (!node.HasChildNodes() && node.Objects.Count + 1 > maxObjectsPerLeaf)
			{
				SetupChildNodes(node);

				var childObjects = new List<T>(node.Objects);
				var childrenToRelocate = new List<T>();

				foreach (var childObject in childObjects)
				{
					foreach (var childNode in node.Nodes)
					{
						if (childNode == null)
						{
							continue;
						}

						if (childNode.Bounds.Contains(childObject.Bounds))
						{
							childrenToRelocate.Add(childObject);
						}
					}
				}

				foreach (var childObject in childrenToRelocate)
				{
					RemoveQuadObjectFromNode(childObject);
					InsertNodeObject(node, childObject);
				}
			}

			foreach (var childNode in node.Nodes)
			{
				if (childNode != null)
				{
					if (childNode.Bounds.Contains(quadObject.Bounds))
					{
						InsertNodeObject(childNode, quadObject);
						return;
					}
				}
			}

			AddQuadObjectToNode(node, quadObject);
		}

		private void RemoveQuadObjectFromNode(T quadObject)
		{
			var node = objectToNodeLookup[quadObject];
			node.quadObjects.Remove(quadObject);
			objectToNodeLookup.Remove(quadObject);
			quadObject.BoundsChanged -= new EventHandler(quadObject_BoundsChanged);
		}

		private void AddQuadObjectToNode(QuadNode<T> node, T quadObject)
		{
			node.quadObjects.Add(quadObject);
			objectToNodeLookup.Add(quadObject, node);
			quadObject.BoundsChanged += new EventHandler(quadObject_BoundsChanged);
		}

		private void quadObject_BoundsChanged(object sender, EventArgs e)
		{
			var quadObject = sender as T;
			if (quadObject != null)
			{
				var node = objectToNodeLookup[quadObject];
				if (!node.Bounds.Contains(quadObject.Bounds) || node.HasChildNodes())
				{
					RemoveQuadObjectFromNode(quadObject);
					Insert(quadObject);
					if (node.Parent != null)
					{
						CheckChildNodes(node.Parent);
					}
				}
			}
		}

		private void SetupChildNodes(QuadNode<T> node)
		{
			if (minLeafSize.X <= node.Bounds.Width / 2 && minLeafSize.Y <= node.Bounds.Height / 2)
			{
				node[EDirection.NW] = new QuadNode<T>(node.Bounds.X, node.Bounds.Y, node.Bounds.Width / 2,
												  node.Bounds.Height / 2);
				node[EDirection.NE] = new QuadNode<T>(node.Bounds.X + node.Bounds.Width / 2, node.Bounds.Y,
												  node.Bounds.Width / 2,
												  node.Bounds.Height / 2);
				node[EDirection.SW] = new QuadNode<T>(node.Bounds.X, node.Bounds.Y + node.Bounds.Height / 2,
												  node.Bounds.Width / 2,
												  node.Bounds.Height / 2);
				node[EDirection.SE] = new QuadNode<T>(node.Bounds.X + node.Bounds.Width / 2,
												  node.Bounds.Y + node.Bounds.Height / 2,
												  node.Bounds.Width / 2, node.Bounds.Height / 2);

			}
		}

		public void Remove(T quadObject)
		{
			if (!objectToNodeLookup.ContainsKey(quadObject))
			{
				throw new KeyNotFoundException("QuadObject not found in dictionary for removal");
			}

			var containingNode = objectToNodeLookup[quadObject];
			RemoveQuadObjectFromNode(quadObject);

			if (containingNode.Parent != null)
			{
				CheckChildNodes(containingNode.Parent);
			}
		}

		private void CheckChildNodes(QuadNode<T> node)
		{
			if (GetQuadObjectCount(node) <= maxObjectsPerLeaf)
			{
				// Move child objects into this node, and delete sub nodes
				var subChildObjects = GetChildObjects(node);
				foreach (var childObject in subChildObjects)
				{
					if (!node.Objects.Contains(childObject))
					{
						RemoveQuadObjectFromNode(childObject);
						AddQuadObjectToNode(node, childObject);
					}
				}
				if (node[EDirection.NW] != null)
				{
					node[EDirection.NW].Parent = null;
					node[EDirection.NW] = null;
				}
				if (node[EDirection.NE] != null)
				{
					node[EDirection.NE].Parent = null;
					node[EDirection.NE] = null;
				}
				if (node[EDirection.SW] != null)
				{
					node[EDirection.SW].Parent = null;
					node[EDirection.SW] = null;
				}
				if (node[EDirection.SE] != null)
				{
					node[EDirection.SE].Parent = null;
					node[EDirection.SE] = null;
				}

				if (node.Parent != null)
				{
					CheckChildNodes(node.Parent);
				}
				else
				{
					// Its the root node, see if we're down to one quadrant, with none in local storage - if so, ditch the other three
					var numQuadrantsWithObjects = 0;
					QuadNode<T> nodeWithObjects = null;
					foreach (var childNode in node.Nodes)
					{
						if (childNode != null && GetQuadObjectCount(childNode) > 0)
						{
							numQuadrantsWithObjects++;
							nodeWithObjects = childNode;
							if (numQuadrantsWithObjects > 1)
							{
								break;
							}
						}
					}
					if (numQuadrantsWithObjects == 1)
					{
						foreach (var childNode in node.Nodes)
						{
							if (childNode != nodeWithObjects)
							{
								childNode.Parent = null;
							}
						}
						Root = nodeWithObjects;
					}
				}
			}
		}

		private List<T> GetChildObjects(QuadNode<T> node)
		{
			var results = new List<T>();
			results.AddRange(node.quadObjects);
			foreach (var childNode in node.Nodes)
			{
				if (childNode != null)
				{
					results.AddRange(GetChildObjects(childNode));
				}
			}
			return results;
		}

		public int GetQuadObjectCount()
		{
			if (Root == null)
			{
				return 0;
			}

			return GetQuadObjectCount(Root);
		}

		private int GetQuadObjectCount(QuadNode<T> node)
		{
			int count = node.Objects.Count;
			foreach (var childNode in node.Nodes)
			{
				if (childNode != null)
				{
					count += GetQuadObjectCount(childNode);
				}
			}
			return count;
		}

		public int GetQuadNodeCount()
		{
			if (Root == null)
			{
				return 0;
			}
			return GetQuadNodeCount(Root, 1);
		}

		private int GetQuadNodeCount(QuadNode<T> node, int count)
		{
			if (node == null)
			{
				return count;
			}

			foreach (var childNode in node.Nodes)
			{
				if (childNode != null)
				{
					count++;
				}
			}
			return count;
		}

		#endregion //Private Methods

		#endregion //Methods
	}
}