using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using PolygonUnwrapper.ObjParser.Types;
using PolygonUnwrapper.PolygonTool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonUnwrapper
{
    class Program
    {
        class Parameters
        {
            public string FilePath;
            public int Start;
            public int Finish;
            public int Width;
            public int Height;
            public int Spacing = 3;
            public char Axis = 'z';

            public static Parameters Parse(string[] args)
            {
                var res = new Parameters();
                try
                {
                    var queue = new Queue<string>(args);
                    while (queue.Count > 0)
                    {
                        var name = queue.Dequeue();
                        var value = queue.Dequeue();
                        switch (name.ToLower())
                        {
                            case "-f":
                                res.FilePath = value;
                                if (!File.Exists(res.FilePath)) throw new Exception("Input file is not exists");
                                if (Path.GetExtension(res.FilePath) != ".obj") throw new Exception("Input file is not OBJ-file");
                                break;
                            case "-w":
                                if (!int.TryParse(value, out res.Width)) throw new Exception("Width argument is not a number");
                                break;
                            case "-h":
                                if (!int.TryParse(value, out res.Height)) throw new Exception("Height argument is not a number");
                                break;
                            case "-l":
                                var limits = value;
                                if (limits.Contains(":"))
                                {
                                    var tokens = limits.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (!int.TryParse(tokens[0], out res.Start)) throw new Exception("Start argument is not a number");
                                    if (!int.TryParse(tokens[1], out res.Finish)) throw new Exception("Finish argument is not a number");
                                    if (res.Start < 1) throw new Exception("Start should be a positive number");
                                    if (res.Finish < 1) throw new Exception("Finish should be a positive number");
                                    if (res.Start > res.Finish) throw new Exception("Start should be less or equal that finish");
                                }
                                else
                                {
                                    if (!int.TryParse(limits, out res.Finish)) throw new Exception("Finish argument is not a number");
                                    if (res.Finish < 1) throw new Exception("Finish should be a positive number");
                                }
                                break;
                            case "-s":
                                if (!int.TryParse(value, out res.Spacing)) throw new Exception("Spacing argument is not a number");
                                if (res.Spacing < 0) throw new Exception("Spacing argument should be a positive number");
                                break;
                            //case "-a":
                            //    if (value.Length == 1 && "xyz".Contains(value.ToLower()[0]))
                            //        res.Axis = value.ToLower()[0];
                            //    else throw new Exception("Axis should be 'x', 'y', or 'z'");
                            //    break;
                            case "--help":
                                Console.WriteLine("Argument keys:\n"
                                    + "-f\tInput file path. Required.\n"
                                    + "-w\tPage width. Required.\n"
                                    + "-h\tPage height. Required.\n"
                                    + "-l\tPolygons loading limits. Optional. By default loading all polygons. Two forms of limits:\n\t\t1. 'start:finish' - loading polygons from number 'start' to number 'finish'.\n\t\t2. 'finish'- loading polygons from begining to 'finish' number.\n"
                                    //+ "-a\tAxis orthogonal to page orientation. Optional. Default: 'z'.\n"
                                    );
                                return null;
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Invalid arguments. Type '--help' to see help."); return null;
                }

                return res;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                var parameters = Parameters.Parse(args);
                if (parameters == null) return;

                var obj = new ObjParser.Obj();
                obj.LoadObj(parameters.FilePath);

                var model = new PolygonalModel()
                    .LoadFromObj(obj)
                    .Limit(parameters.Start, parameters.Finish)
                    .Sort(asc: true)
                    .ReduceToTriangles()
                    .RenumberPolygons();

                var infoBuilder = new StringBuilder();

                var grid = model
                    .Clone()
                    .Align()
                    .Sort(asc: false)
                    .SpreadToPages(parameters.Width, parameters.Height, parameters.Spacing)
                    .Get(m =>
                    {
                        infoBuilder
                            .AppendLine(
                                $"Max polygon: {m.Info.MaxPolygonWidth:N1}x{m.Info.MaxPolygonHeight:N1};\t"
                                + $"Polygons total perimeter: {m.Info.PerimeterSum:N1};\t"
                                + $"Polygons total area: {m.Info.AreaSum:N1};\t"
                            )
                            .AppendLine(
                                $"Pages count: {m.Info.PagesCount:D};\t"
                                + $"Minimum polygons on page: {m.Info.MinPolygonsOnPage:D};\t"
                                + $"Maximum polygons on page: {m.Info.MaxPolygonsOnPage:D};\t"
                            )
                            .AppendLine(
                                $"Pages total area: {m.Info.PagesAreaSum:N1};\t"
                                + $"Avarage density: {m.Info.Density:P1};\t"
                            );
                        foreach (var polygonGroup in m.Polygons.GroupBy(p => p.Page))
                        {
                            infoBuilder.AppendLine($"Page #{polygonGroup.Key}");
                            foreach (var polygon in polygonGroup)
                            {
                                infoBuilder.AppendLine(
                                    $"Name: {polygon.Name};\t"
                                    + $"Max edge: {polygon.MaxEdge.Length():N1};\t"
                                    + $"Perimeter: {polygon.Perimeter:N1};\t"
                                    + $"Area: {polygon.Area:N1};\t"
                                );
                            }
                        }
                    });

                var modelObj = model.LoadToObj();
                modelObj.WriteObjFile("model.obj", new string[0]);

                var gridObj = grid.LoadToObj();
                gridObj.WriteObjFile("grid.obj", new string[0]);

                File.WriteAllText("info.txt", infoBuilder.ToString());

                void OutputDxfFiles()
                {
                    Vector3 Vec3ToVector3(Vec3 v) => new Vector3(v.X, v.Y, v.Z);
                    void CreateTextEnity(Polygon3D polygon, DxfDocument doc)
                    {
                        var orderedVertices = new Vec3[3];
                        for (var i = 0; i < 3; i++)
                        {
                            var edge = polygon.Vertices[(i + 1) % 3].Sub(polygon.Vertices[i]);
                            if (edge.Sub(polygon.MaxEdge).Length() <= double.Epsilon)
                            {
                                orderedVertices[0] = polygon.Vertices[i];
                                orderedVertices[1] = polygon.Vertices[(i + 1) % 3];
                                orderedVertices[2] = polygon.Vertices[(i + 2) % 3];
                                break;
                            }
                        }

                        Vec3 a = orderedVertices[0];
                        Vec3 b = orderedVertices[1];
                        Vec3 c = orderedVertices[2];

                        const double k = 1;
                        var n = polygon.Name.Length;

                        var ab = b.Sub(a).Length();
                        var bc = c.Sub(b).Length();
                        var ca = a.Sub(c).Length();
                        var cosAlpha = (ab * ab + ca * ca - bc * bc) / 2.0 / ab / ca;
                        var sinAlpha = Math.Sqrt(1 - cosAlpha * cosAlpha);
                        var beta = Math.Acos((b.X - a.X)/ab)*Math.Sign(b.Y - a.Y);
                        var t = 1 / (1 + k * n * sinAlpha * ca / ab);
                        var h = t * ca * sinAlpha;
                        var w = (1 - t) * ab;
                        double error = w/h - n * k;
                        var d = c.Sub(a).Mul(t).Add(a);
                        var pos = b.Sub(a).Mul(cosAlpha * d.Sub(a).Length() / ab).Add(a);

                        var textSize = h*0.8;
                        var text = new Text(
                            polygon.Name,
                            new Vector2(
                                pos.X + (w * Math.Cos(beta) - h * Math.Sin(beta)) * 0.05,
                                pos.Y + (w * Math.Sin(beta) + h * Math.Cos(beta)) * 0.05),
                            textSize);
                        text.Rotation = beta / Math.PI * 180;
                        doc.AddEntity(text);
                    }

                    var dxf = new DxfDocument();
                    var namesLayer = new Layer("Names");
                    var polygonsLayer = new Layer("Polygons");

                    foreach (var p in grid.Polygons)
                    {
                        var entity = new Polyline(p.Vertices.Select(Vec3ToVector3), true);
                        entity.Layer = polygonsLayer;
                        dxf.AddEntity(entity);
                        CreateTextEnity(p, dxf);
                    }
                    dxf.Save("polygons+names.dxf");
                }
                OutputDxfFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
