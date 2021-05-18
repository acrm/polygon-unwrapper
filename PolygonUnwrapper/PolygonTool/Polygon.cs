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

    public class Polygon
    {
        public string Name;
        public List<Vec3> Vertices = new List<Vec3>();

        public Polygon Apply(Func<Vec3, Vec3> t)
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] = t(Vertices[i]);
            }
            return this;
        }

        public Polygon Translate(Vec3 offset) => Apply(v => v.Add(offset));
        public Polygon Rotate(Vec3 axis, double angle) => Apply(v => v.Rotate(axis, angle));

        public Polygon Clone()
            => new Polygon
            {
                Name = Name,
                Vertices = Vertices.Select(v => new Vec3(v.X, v.Y, v.Z)).ToList()
            };

        public Polygon Align()
        {
            var targetNormal = new Vec3(0, 0, 1);
            var targetFirstVector = new Vec3(0, -1, 0);

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

    public class PolygonalModel
    {
        public List<Polygon> Polygons = new List<Polygon>();

        public void LoadFromObj(Obj obj, int start, int finish)
        {
            if (start == 0) start = 1;
            if (finish == 0) finish = obj.FaceList.Count;
            for(var i = start-1; i < finish; i++)
            {
                var face = obj.FaceList[i];
                var polygon = new Polygon
                {
                    Name = face.GroupName
                };
                foreach (var vIndex in face.VertexIndexList)
                {
                    var v = obj.VertexList[vIndex - 1];
                    polygon.Vertices.Add(new Vec3(v.X, v.Y, v.Z));
                }
                Polygons.Add(polygon);
            }
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

        public void RenamePolygons()
        {
            var number = 1;
            foreach (var polygon in Polygons)
            {
                polygon.Name = $"{number++}";
            }
        }

        public PolygonalModel Clone()
        {
            return new PolygonalModel
            {
                Polygons = Polygons.Select(p => p.Clone()).ToList()
            };
        }

        public PolygonalModel SplitToGrid(int pageWidth, int pageHeight)
        {
            var maxHeight = 0.0;
            var pos = new Vec3(0, 0, 0);
            var pageTop = pos.Y;
            var pageMargin = pageHeight / 10;

            for (var i = 0; i < Polygons.Count; i++)
            {
                var polygon = Polygons[i];
                var top = polygon.Vertices.Max(v => v.Y);
                var bottom = polygon.Vertices.Min(v => v.Y);
                var left = polygon.Vertices.Min(v => v.X);
                var right = polygon.Vertices.Max(v => v.X);

                var shiftToOrigin = new Vec3(-left, -top, -polygon.Vertices[0].Z);
                var width = right - left;
                var height = top - bottom;

                if (width > pageWidth)
                    throw new Exception($"polygon width ({width}) greater than page width ({pageWidth})");

                if (pos.X + width > pageWidth)
                {
                    // next row
                    pos = new Vec3(0, pos.Y - maxHeight, 0);
                    
                    if (Math.Abs(pos.Y - height - pageTop) > pageHeight)
                    {
                        // next page
                        pageTop = pos.Y - pageMargin;
                        pos.Y = pageTop;
                    }
                    i--;
                    maxHeight = 0;
                    continue;
                }

                polygon.Translate(shiftToOrigin.Add(pos));
                pos = pos.Add(new Vec3(width, 0, 0));
                maxHeight = Math.Max(maxHeight, height);
            }
            return this;
        }

        public PolygonalModel Apply(Func<Polygon, Polygon> t)
        {
            for (var i = 0; i < Polygons.Count; i++)
            {
                Polygons[i] = t(Polygons[i]);
            }
            return this;
        }

        public PolygonalModel Align() => Apply(p => p.Align());
    }
}
