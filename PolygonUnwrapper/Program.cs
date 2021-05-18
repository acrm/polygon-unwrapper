﻿using PolygonUnwrapper.ObjParser.Types;
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
                                if (!File.Exists(res.FilePath)) { Console.WriteLine("Input file is not exists"); return null; }
                                if (Path.GetExtension(res.FilePath) != ".obj") { Console.WriteLine("Input file is not OBJ-file"); return null; }
                                break;
                            case "-w":
                                if (!int.TryParse(value, out res.Width)) { Console.WriteLine("width argument is not a number"); return null; }
                                break;
                            case "-h":
                                if (!int.TryParse(value, out res.Height)) { Console.WriteLine("height argument is not a number"); return null; }
                                break;
                            case "-l":
                                var limits = value;
                                if (limits.Contains(":"))
                                {
                                    var tokens = limits.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (!int.TryParse(tokens[0], out res.Start)) { Console.WriteLine("start argument is not a number"); return null; }
                                    if (!int.TryParse(tokens[1], out res.Finish)) { Console.WriteLine("finish argument is not a number"); return null; }
                                    if (res.Start < 1) { Console.WriteLine("start should be a positive number"); return null; }
                                    if (res.Finish < 1) { Console.WriteLine("finish should be a positive number"); return null; }
                                    if (res.Start > res.Finish) { Console.WriteLine("start should be less or equal that finish"); return null; }
                                }
                                else
                                {
                                    if (!int.TryParse(limits, out res.Finish)) { Console.WriteLine("finish argument is not a number"); return null; }
                                    if (res.Finish < 1) { Console.WriteLine("finish should be a positive number"); return null; }
                                }
                                break;
                            //case "-a":
                            //    if (value.Length == 1 && "xyz".Contains(value.ToLower()[0]))
                            //        res.Axis = value.ToLower()[0];
                            //    else { Console.WriteLine("axis should be 'x', 'y', or 'z'"); return null; }
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
            var parameters = Parameters.Parse(args);
            if (parameters == null) return;

            var obj = new ObjParser.Obj();
            obj.LoadObj(parameters.FilePath);

            var model = new PolygonalModel();
            model.LoadFromObj(obj, parameters.Start, parameters.Finish);
            model.RenamePolygons();

            var grid = model
                .Clone()
                .Align()
                .SplitToGrid(parameters.Width, parameters.Height);

            var modelObj = model.LoadToObj();
            modelObj.WriteObjFile("model.obj", new string[0]);

            var gridObj = grid.LoadToObj();
            gridObj.WriteObjFile("grid.obj", new string[0]);
        }
    }
}
