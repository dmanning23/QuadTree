using System;
using System.Windows;

namespace QuadTreeLib
{
    public interface IQuadObject
    {
        Rect Bounds { get; }
        event EventHandler BoundsChanged;
    }
}