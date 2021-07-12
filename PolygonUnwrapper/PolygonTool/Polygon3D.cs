using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonUnwrapper.PolygonTool
{
    public class Polygon3D
    {
        public string Name;
        public int Page { get; set; }
        private readonly List<Vec3> _vertices = new List<Vec3>();

        public IReadOnlyList<Vec3> Vertices => _vertices;

        public Boundaries Boundaries { get; private set; } = new Boundaries();
        public Vec3 MaxEdge { get; private set; } = new Vec3();
        public double NormalAngleError { get; private set; }
        public double Area { get; private set; }
        public double Perimeter { get; private set; }

        private Polygon3D CalcMetrics()
        {
            Boundaries.CalcMetrics(_vertices);

            var area = 0.0;
            var perimeter = 0.0;
            var maxEdge = new Vec3();
            var maxLength = 0.0;
            var normalAngleError = 0.0;
            for (var i = 0; i < _vertices.Count; i++)
            {
                var nextIndex = i == _vertices.Count - 1 ? 0 : i + 1;
                var vertex = _vertices[i];
                var nextVertex = _vertices[nextIndex];
                var edge = nextVertex.Sub(vertex);
                var length = edge.Length();
                if (length > maxLength)
                {
                    maxEdge = edge;
                    maxLength = length;
                }
                perimeter += length;

                var vec1 = vertex.Sub(_vertices[0]);
                var vec2 = nextVertex.Sub(_vertices[0]);
                var crossProduct = vec1.Cross(vec2);
                area += 0.5 * crossProduct.Length();

                var axis = crossProduct.Cross(Vec3.Front);
                var l = axis.Length();
                if (l > 0.001)
                {
                    normalAngleError += Math.Abs(crossProduct.Angle(Vec3.Front));
                }
            }
            MaxEdge = maxEdge;
            Area = area;
            Perimeter = perimeter;
            NormalAngleError = normalAngleError;

            return this;
        }

        public IList<Polygon3D> GetSubTriangles()
        {
            if (_vertices.Count <= 3) return new List<Polygon3D>(1) { this };

            var subTriangles = new List<Polygon3D>(_vertices.Count - 2);
            for (var i = 1; i < _vertices.Count - 1; i++)
            {
                subTriangles.Add(
                    new Polygon3D()
                    .AddVertices(new Vec3[]
                    {
                        _vertices[0],
                        _vertices[i],
                        _vertices[i + 1]
                    }));
            }
            return subTriangles;
        }

        public Polygon3D AddVertice(Vec3 v)
        {
            _vertices.Add(v);
            CalcMetrics();

            return this;
        }

        public Polygon3D AddVertices(IEnumerable<Vec3> vertices)
        {
            _vertices.AddRange(vertices);
            CalcMetrics();

            return this;
        }

        public Polygon3D Apply(Func<Vec3, Vec3> t)
        {
            for (var i = 0; i < _vertices.Count; i++)
            {
                _vertices[i] = t(_vertices[i]);
            }

            CalcMetrics();

            return this;
        }

        public Polygon3D Translate(Vec3 offset) => Apply(v => v.Add(offset));
        public Polygon3D Rotate(Vec3 axis, double angle) => Apply(v => v.Rotate(axis, angle));

        public Polygon3D Clone()
            => new Polygon3D
            {
                Name = Name
            }
            .AddVertices(Vertices.Select(v => new Vec3(v.X, v.Y, v.Z)));

        public Polygon3D Align()
        {
            var targetNormal = Vec3.Front;
            var targetFirstVector = Vec3.Down;

            var firstVector = Vertices[1].Sub(Vertices[0]);
            var secondVector = Vertices[2].Sub(Vertices[0]);
            var normal = firstVector.Cross(secondVector);

            var axis1 = normal.Cross(targetNormal);
            if (axis1.Length() != 0)
            {
                var angle1 = normal.Angle(targetNormal);
                Apply(v => v.Rotate(axis1, angle1));
            }

            firstVector = Vertices[1].Sub(Vertices[0]);
            var angle2 = firstVector.Angle(targetFirstVector);
            var sign = Math.Sign(firstVector.Cross(targetFirstVector).Dot(targetNormal));
            Apply(v => v.Rotate(targetNormal, sign * angle2));

            return this;
        }
    }
}
