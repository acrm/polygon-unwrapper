using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonUnwrapper.PolygonTool
{
    public class Triangle2D
    {
        private Vec2[] _vertices = new Vec2[3];

        public Triangle2D(params Vec2[] vertices)
        {
            SetOrderedVertices(vertices);
        }

        private void SetOrderedVertices(Vec2[] vertices)
        {
            if (vertices.Length != 3)
                throw new ArgumentException("Should be exact 3 vertices");

            var maxEdgeStartIndex = -1;
            var maxEdgeLength = 0.0;
            for (var i = 1; i < 3; i++)
            {
                var edge = vertices[(i + 1) % 3].Sub(vertices[i]);
                var edgeLength = edge.Length();
                if (edgeLength > maxEdgeLength)
                {
                    maxEdgeStartIndex = i;
                    maxEdgeLength = edgeLength;
                }
            }
            if (maxEdgeStartIndex == -1)
                throw new ArgumentException("Should be 3 different coordinates");

            _vertices[0] = vertices[maxEdgeStartIndex];
            _vertices[1] = vertices[(maxEdgeStartIndex + 1) % 3];
            _vertices[2] = vertices[(maxEdgeStartIndex + 2) % 3];
        }

        public IReadOnlyList<Vec2> Vertices => _vertices;
        public Vec2 A => _vertices[0];
        public Vec2 B => _vertices[1];
        public Vec2 C => _vertices[2];

        public string Name;
        public int Page { get; set; }

        public Boundaries Boundaries { get; private set; } = new Boundaries();
        public Vec3 MaxEdge { get; private set; } = new Vec3();
        public double NormalAngleError { get; private set; }
        public double Area { get; private set; }
        public double Perimeter { get; private set; }

        public Boundaries CalcMetrics() => new Boundaries
        {
            Top = _vertices.Max(v => v.Y),
            Bottom = _vertices.Min(v => v.Y),
            Left = _vertices.Min(v => v.X),
            Right = _vertices.Max(v => v.X)
        };

        //private Triangle2D CalcMetrics()
        //{
        //    Boundaries.CalcMetrics(_vertices);

        //    var area = 0.0;
        //    var perimeter = 0.0;
        //    var maxEdge = new Vec3();
        //    var maxLength = 0.0;
        //    var normalAngleError = 0.0;
        //    for (var i = 0; i < _vertices.Count; i++)
        //    {
        //        var nextIndex = i == _vertices.Count - 1 ? 0 : i + 1;
        //        var vertex = _vertices[i];
        //        var nextVertex = _vertices[nextIndex];
        //        var edge = nextVertex.Sub(vertex);
        //        var length = edge.Length();
        //        if (length > maxLength)
        //        {
        //            maxEdge = edge;
        //            maxLength = length;
        //        }
        //        perimeter += length;

        //        var vec1 = vertex.Sub(_a);
        //        var vec2 = nextVertex.Sub(_a);
        //        var crossProduct = vec1.Cross(vec2);
        //        area += 0.5 * crossProduct.Length();

        //        var axis = crossProduct.Cross(Vec3.Front);
        //        var l = axis.Length();
        //        if (l > 0.001)
        //        {
        //            normalAngleError += Math.Abs(crossProduct.Angle(Vec3.Front));
        //        }
        //    }
        //    MaxEdge = maxEdge;
        //    Area = area;
        //    Perimeter = perimeter;
        //    NormalAngleError = normalAngleError;

        //    return this;
        //}

        public Triangle2D Apply(Func<Vec2, Vec2> t)
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                _vertices[i] = t(Vertices[i]);
            }

            //CalcMetrics();

            return this;
        }

        public Triangle2D Translate(Vec2 offset) => Apply(v => v.Add(offset));
        //public Triangle2D Rotate(Vec2 axis, double angle) => Apply(v => v.Rotate(axis, angle));

        public Triangle2D Clone()
            => new Triangle2D(
                Vertices
                    .Select(v => new Vec2(v.X, v.Y))
                    .ToArray())
                {
                    Name = Name
                };

        //public Triangle2D Align()
        //{
        //    var targetNormal = Vec3.Front;
        //    var targetFirstVector = Vec3.Down;

        //    var firstVector = Vertices[1].Sub(Vertices[0]);
        //    var secondVector = Vertices[2].Sub(Vertices[0]);
        //    var normal = firstVector.Cross(secondVector);

        //    var axis1 = normal.Cross(targetNormal);
        //    if (axis1.Length() != 0)
        //    {
        //        var angle1 = normal.Angle(targetNormal);
        //        Apply(v => v.Rotate(axis1, angle1));
        //    }

        //    firstVector = Vertices[1].Sub(Vertices[0]);
        //    var angle2 = firstVector.Angle(targetFirstVector);
        //    var sign = Math.Sign(firstVector.Cross(targetFirstVector).Dot(targetNormal));
        //    Apply(v => v.Rotate(targetNormal, sign * angle2));

        //    return this;
        //}
    }
}
