using PolygonUnwrapper.ObjParser;
using PolygonUnwrapper.ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonUnwrapper.PolygonTool
{
    public class Vec3
    {
        public double X;
        public double Y;
        public double Z;

        public static readonly Vec3 Up = new Vec3(0, 1, 0);
        public static readonly Vec3 Down = new Vec3(0, -1, 0);
        public static readonly Vec3 Right = new Vec3(1, 0, 0);
        public static readonly Vec3 Left = new Vec3(-1, 0, 0);
        public static readonly Vec3 Front = new Vec3(0, 0, 1);
        public static readonly Vec3 Back = new Vec3(0, 0, -1);

        public Vec3() { }

        public Vec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() => $"{X.ToString("F3")} {Y.ToString("F3")} {Z.ToString("F3")}";

        public Vec3 Add(Vec3 other) => new Vec3(X + other.X, Y + other.Y, Z + other.Z);
        public Vec3 Sub(Vec3 other) => new Vec3(X - other.X, Y - other.Y, Z - other.Z);
        public Vec3 Mul(double scalar) => new Vec3(X*scalar, Y*scalar, Z*scalar);

        public double Dot(Vec3 other) => X*other.X + Y*other.Y + Z*other.Z;
        public Vec3 Cross(Vec3 other)
            => new Vec3(
                Y*other.Z - Z*other.Y,
                Z*other.X - X*other.Z,
                X*other.Y - Y*other.X);

        public double Length() => Math.Sqrt(Dot(this));

        public Vec3 Normalized() => Mul(1/Length());

        public double Angle(Vec3 other) => Math.Acos(Dot(other)/(Length() * other.Length()));

        public Vec3 Rotate(Vec3 axis, double theta)
        {
            var vec = new Vec3();
            var costheta = Math.Cos(theta);
            var sintheta = Math.Sin(theta);
            axis = axis.Normalized();

            vec.X += (costheta + (1 - costheta) * axis.X * axis.X) * X;
            vec.X += ((1 - costheta) * axis.X * axis.Y - axis.Z * sintheta) * Y;
            vec.X += ((1 - costheta) * axis.X * axis.Z + axis.Y * sintheta) * Z;

            vec.Y += ((1 - costheta) * axis.X * axis.Y + axis.Z * sintheta) * X;
            vec.Y += (costheta + (1 - costheta) * axis.Y * axis.Y) * Y;
            vec.Y += ((1 - costheta) * axis.Y * axis.Z - axis.X * sintheta) * Z;

            vec.Z += ((1 - costheta) * axis.X * axis.Z - axis.Y * sintheta) * X;
            vec.Z += ((1 - costheta) * axis.Y * axis.Z + axis.X * sintheta) * Y;
            vec.Z += (costheta + (1 - costheta) * axis.Z * axis.Z) * Z;

            return vec;
        }
    }

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

    public class Polygon
    {
        public string Name;
        private readonly List<Vec3> _vertices = new List<Vec3>();

        public IReadOnlyList<Vec3> Vertices => _vertices;

        public Boundaries Boundaries { get; private set; } = new Boundaries();
        public Vec3 MaxEdge { get; private set; } = new Vec3();
        public double NormalAngleError { get; private set; }
        public double Area { get; private set; }
        public double Perimeter { get; private set; }

        private Polygon CalcMetrics()
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

        public IList<Polygon> GetSubTriangles()
        {
            if (_vertices.Count <= 3) return new List<Polygon>(1) { this };

            var subTriangles = new List<Polygon>(_vertices.Count - 2);
            for (var i = 1; i < _vertices.Count - 1; i++)
            {
                subTriangles.Add(
                    new Polygon()
                    .AddVertices(new Vec3[]
                    {
                        _vertices[0],
                        _vertices[i],
                        _vertices[i + 1]
                    }));
            }
            return subTriangles;
        }

        public Polygon AddVertice(Vec3 v)
        {
            _vertices.Add(v);
            CalcMetrics();

            return this;
        }

        public Polygon AddVertices(IEnumerable<Vec3> vertices)
        {
            _vertices.AddRange(vertices);
            CalcMetrics();

            return this;
        }

        public Polygon Apply(Func<Vec3, Vec3> t)
        {
            for (var i = 0; i < _vertices.Count; i++)
            {
                _vertices[i] = t(_vertices[i]);
            }

            CalcMetrics();

            return this;
        }

        public Polygon Translate(Vec3 offset) => Apply(v => v.Add(offset));
        public Polygon Rotate(Vec3 axis, double angle) => Apply(v => v.Rotate(axis, angle));

        public Polygon Clone()
            => new Polygon
            {
                Name = Name
            }
            .AddVertices(Vertices.Select(v => new Vec3(v.X, v.Y, v.Z)));

        public Polygon Align()
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

    public class PolygonalModelInfo
    {
        public double MaxPolygonWidth;
        public double MaxPolygonHeight;
        public double NormalAngleErrorSum;
        public double AreaSum;
        public double PerimeterSum;
    }

    public class PolygonalModel
    {
        private List<Polygon> _polygons = new List<Polygon>();
        public IReadOnlyList<Polygon> Polygons => _polygons;

        public Boundaries Boundaries { get; private set; } = new Boundaries();

        public PolygonalModelInfo Info { get; private set; } = new PolygonalModelInfo();

        private PolygonalModel CalcMetrics()
        {
            Boundaries.CalcMetrics(_polygons);

            Info.MaxPolygonWidth = 0;
            Info.MaxPolygonHeight = 0;
            Info.NormalAngleErrorSum = 0;
            Info.AreaSum = 0;
            Info.PerimeterSum = 0;
            foreach (var polygon in _polygons)
            {
                Info.MaxPolygonWidth = Math.Max(Info.MaxPolygonWidth, polygon.Boundaries.Width);
                Info.MaxPolygonHeight = Math.Max(Info.MaxPolygonHeight, polygon.Boundaries.Height);
                Info.NormalAngleErrorSum += polygon.NormalAngleError;
                Info.AreaSum += polygon.Area;
                Info.PerimeterSum += polygon.Perimeter;
            }

            return this;
        }

        public PolygonalModel Get(Action<PolygonalModel> modelAccessor)
        {
            modelAccessor(this);
            return this;
        }

        public PolygonalModel LoadFromObj(Obj obj)
        {
            for (var i = 0; i < obj.FaceList.Count; i++)
            {
                var face = obj.FaceList[i];
                var polygon = new Polygon
                {
                    Name = face.GroupName
                };
                var vertices = new List<Vec3>(face.VertexIndexList.Length);
                foreach (var vIndex in face.VertexIndexList)
                {
                    var v = obj.VertexList[vIndex - 1];
                    vertices.Add(new Vec3(v.X, v.Y, v.Z));
                }
                polygon.AddVertices(vertices);
                _polygons.Add(polygon);
            }

            CalcMetrics();

            return this;
        }

        public PolygonalModel Limit(int start, int finish)
        {
            if (start == 0) start = 1;
            if (finish == 0) finish = _polygons.Count;

            _polygons = _polygons
                .Skip(start - 1)
                .Take(finish - start + 1)
                .ToList();

            CalcMetrics();

            return this;
        }

        public PolygonalModel Sort()
        {
            _polygons = _polygons.OrderByDescending(p => p.MaxEdge.Length()).ToList();

            return this;
        }

        public Obj LoadToObj()
        {
            var obj = new Obj();
            foreach (var polygon in Polygons)
            {
                var face = new Face();
                var vertexIndexList = new List<int>();
                foreach (var vec in polygon.Vertices)
                {
                    obj.VertexList.Add(new Vertex() { X = vec.X, Y = vec.Y, Z = vec.Z });
                    vertexIndexList.Add(obj.VertexList.Count);
                }
                face.VertexIndexList = vertexIndexList.ToArray();
                face.GroupName = polygon.Name;
                obj.FaceList.Add(face);
            }
            return obj;
        }

        public PolygonalModel RenamePolygons()
        {
            var number = 1;
            foreach (var polygon in Polygons)
            {
                polygon.Name = $"{number++}";
            }

            return this;
        }

        public PolygonalModel Clone()
        {
            return new PolygonalModel
            {
                _polygons = Polygons.Select(p => p.Clone()).ToList()
            }
            .CalcMetrics();
        }

        public PolygonalModel ReduceToTriangles()
        {
            var newPolygons = new List<Polygon>((int)(_polygons.Count * 1.1)); // 10% reserve for new polygons
            foreach (var polygon in _polygons)
            {
                var subTriangles = polygon.GetSubTriangles();
                newPolygons.AddRange(subTriangles);
            }

            _polygons = newPolygons;
            CalcMetrics();

            return this;
        }

        public PolygonalModel SpreadToGrid(int pageWidth, int pageHeight, int spacing)
        {
            var maxHeight = 0.0;
            var pageTop = 0.0;
            var pos = new Vec3(spacing, -spacing, 0);
            var pageMargin = spacing * 10;

            if (Info.MaxPolygonWidth > pageWidth)
                throw new Exception($"Polygon max width ({Info.MaxPolygonWidth}) greater than page width ({pageWidth}).");
            if (Info.MaxPolygonHeight > pageHeight)
                throw new Exception($"Polygon max height ({Info.MaxPolygonWidth}) greater than page height ({pageWidth}).");

            var polygonsStack = new Stack<Polygon>(_polygons.Reverse<Polygon>());

            while (polygonsStack.Count > 0)
            {
                var firstPolygon = polygonsStack.Pop();

                var shiftToOrigin = new Vec3(-firstPolygon.Boundaries.Left, -firstPolygon.Boundaries.Top, -firstPolygon.Vertices[0].Z);

                if (pageTop - pos.Y + firstPolygon.Boundaries.Height + spacing > pageHeight)
                {
                    // next page
                    pageTop = pageTop - pageHeight - pageMargin - spacing;
                    pos = new Vec3(spacing, pageTop, 0);

                    polygonsStack.Push(firstPolygon);
                    maxHeight = 0;
                    continue;
                }

                if (pos.X + firstPolygon.Boundaries.Width + spacing > pageWidth)
                {
                    // next row
                    pos = new Vec3(spacing, pos.Y - maxHeight - spacing, 0);
                    polygonsStack.Push(firstPolygon);
                    maxHeight = 0;
                    continue;
                }

                firstPolygon.Translate(shiftToOrigin.Add(pos));
                pos = pos.Add(new Vec3(firstPolygon.Boundaries.Width + spacing, 0, 0));
                maxHeight = Math.Max(maxHeight, firstPolygon.Boundaries.Height);
            }

            CalcMetrics(); // update depth

            return this;
        }

        public PolygonalModel Apply(Func<Polygon, Polygon> t)
        {
            for (var i = 0; i < Polygons.Count; i++)
            {
                _polygons[i] = t(_polygons[i]);
            }

            CalcMetrics();

            return this;
        }

        public PolygonalModel Align() => Apply(p => p.Align());
    }
}
