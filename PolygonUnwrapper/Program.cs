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
                    .Sort()
                    .Limit(parameters.Start, parameters.Finish)
                    .RenamePolygons();

                var grid = model
                    .Clone()
                    .Align()
                    .SplitToGrid(parameters.Width, parameters.Height, parameters.Spacing);

                var modelObj = model.LoadToObj();
                modelObj.WriteObjFile("model.obj", new string[0]);

                var gridObj = grid.LoadToObj();
                gridObj.WriteObjFile("grid.obj", new string[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
