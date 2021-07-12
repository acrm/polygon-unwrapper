using PolygonUnwrapper.ObjParser;
using PolygonUnwrapper.ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PolygonUnwrapper.PolygonTool
{
    public class PolygonalModelInfo
    {
        public double MaxPolygonWidth { get; set; }
        public double MaxPolygonHeight { get; set; }
        public double NormalAngleErrorSum { get; set; }
        public double AreaSum { get; set; }
        public double PerimeterSum { get; set; }
        public double PagesAreaSum { get; set; }
        public double Density { get; set; }
        public int PagesCount { get; set; }
        public int MinPolygonsOnPage { get; set; }
        public int MaxPolygonsOnPage { get; set; }
    }
    public class PolygonalModel
    {
        private List<Polygon3D> _polygons = new List<Polygon3D>();
        public IReadOnlyList<Polygon3D> Polygons => _polygons;

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
                var polygon = new Polygon3D
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

        public PolygonalModel Sort(bool asc)
        {
            _polygons = asc
                ? _polygons.OrderBy(p => p.MaxEdge.Length()).ToList()
                : _polygons.OrderByDescending(p => p.MaxEdge.Length()).ToList();

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

        public PolygonalModel RenumberPolygons()
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
            var newPolygons = new List<Polygon3D>((int)(_polygons.Count * 1.1)); // 10% reserve for new polygons
            foreach (var polygon in _polygons)
            {
                var subTriangles = polygon.GetSubTriangles();
                newPolygons.AddRange(subTriangles);
            }

            _polygons = newPolygons;
            CalcMetrics();

            return this;
        }

        public PolygonalModel SpreadToPages(int pageWidth, int pageHeight, int spacing)
        {
            var pagesArea = 0.0;
            var polygonsArea = 0.0;
            var currentPageArea = 0.0;

            var maxHeight = 0.0;
            var pageTop = 0.0;
            var pos = new Vec3(spacing, -spacing, 0);
            var pageMargin = spacing * 10;

            if (Info.MaxPolygonWidth > pageWidth)
                throw new Exception($"Polygon max width ({Info.MaxPolygonWidth}) greater than page width ({pageWidth}).");
            if (Info.MaxPolygonHeight > pageHeight)
                throw new Exception($"Polygon max height ({Info.MaxPolygonWidth}) greater than page height ({pageWidth}).");

            var polygonsStack = new Stack<Polygon3D>(_polygons.Reverse<Polygon3D>());

            var pageNumber = 1;
            var polygonsInCurrentPage = 0;
            Info.MaxPolygonsOnPage = 0;
            Info.MinPolygonsOnPage = polygonsStack.Count;
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
                    pagesArea += currentPageArea;
                    currentPageArea = 0.0;
                    Info.MinPolygonsOnPage = Math.Min(Info.MinPolygonsOnPage, polygonsInCurrentPage);
                    Info.MaxPolygonsOnPage = Math.Max(Info.MaxPolygonsOnPage, polygonsInCurrentPage);
                    polygonsInCurrentPage = 0;
                    pageNumber++;
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
                firstPolygon.Page = pageNumber;
                polygonsInCurrentPage++;

                pos = pos.Add(new Vec3(firstPolygon.Boundaries.Width + spacing, 0, 0));
                maxHeight = Math.Max(maxHeight, firstPolygon.Boundaries.Height);
                polygonsArea += firstPolygon.Area;
                currentPageArea = (firstPolygon.Boundaries.Right - spacing) * (pageTop - firstPolygon.Boundaries.Bottom - spacing);
            }
            pagesArea += currentPageArea;
            Info.PagesAreaSum = pagesArea;
            Info.Density = polygonsArea / pagesArea;
            Info.PagesCount = pageNumber;
            Info.MinPolygonsOnPage = Math.Min(Info.MinPolygonsOnPage, polygonsInCurrentPage);
            Info.MaxPolygonsOnPage = Math.Max(Info.MaxPolygonsOnPage, polygonsInCurrentPage);

            CalcMetrics(); // update depth

            return this;
        }

        public PolygonalModel Apply(Func<Polygon3D, Polygon3D> t)
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
