using System.Collections.Generic;
using System.Linq;

namespace PolygonUnwrapper.PolygonTool
{
    public class Boundaries
    {
        public double Top { get; set; }
        public double Bottom { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }
        public double MaxDepth { get; set; }
        public double MinDepth { get; set; }
        public double Width => Right - Left;
        public double Height => Top - Bottom;
        public double Depth => MaxDepth - MinDepth;

        public Vec3 Middle => new Vec3(Left, Top, MinDepth).Add(new Vec3(Right, Bottom, MaxDepth)).Mul(0.5);

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

        public Boundaries CalcMetrics(IReadOnlyList<Polygon3D> polygons)
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
