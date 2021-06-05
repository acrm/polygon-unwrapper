﻿using System.Collections.Generic;
using System.Linq;

namespace PolygonUnwrapper.PolygonTool
{
    public class Boundaries
    {
        public double Top { get; private set; }
        public double Bottom { get; private set; }
        public double Left { get; private set; }
        public double Right { get; private set; }
        public double MaxDepth { get; private set; }
        public double MinDepth { get; private set; }
        public double Width => Right - Left;
        public double Height => Top - Bottom;
        public double Depth => MaxDepth - MinDepth;

        public Boundaries CalcMetrics(IReadOnlyList<Vec3> vertices)
        {
            Top = vertices.Max(v => v.Y);
            Bottom = vertices.Min(v => v.Y);
            Left = vertices.Min(v => v.X);
            Right = vertices.Max(v => v.X);
            MinDepth = vertices.Min(v => v.Z);
            MaxDepth = vertices.Max(v => v.Z);

            return this;
        }

        public Boundaries CalcMetrics(IReadOnlyList<Polygon> polygons)
        {
            Top = polygons.Max(p => p.Boundaries.Top);
            Bottom = polygons.Max(p => p.Boundaries.Bottom);
            Left = polygons.Max(p => p.Boundaries.Left);
            Right = polygons.Max(p => p.Boundaries.Right);
            MinDepth = polygons.Min(p => p.Boundaries.MinDepth);
            MaxDepth = polygons.Max(p => p.Boundaries.MaxDepth);

            return this;
        }
    }
}