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
        static void Main(string[] args)
        {
            if (args.Length == 0) { Console.WriteLine("Input file path argument required"); return; }
            var filepath = args[0];

            int start = 0;
            int finish = 0;
            if (args.Length > 1)
            {
                var limits = args[1];
                if (limits.Contains(":"))
                {
                    var tokens = limits.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!int.TryParse(tokens[0], out start)) { Console.WriteLine("start argument not a number"); return; }
                    if (!int.TryParse(tokens[1], out finish)) { Console.WriteLine("finish argument not a number"); return; }
                    if (start < 1) { Console.WriteLine("start should be a positive number"); return; }
                    if (finish < 1) { Console.WriteLine("finish should be a positive number"); return; }
                    if (start > finish) { Console.WriteLine("start should be less or equal that finish"); return; }
                }
                else
                {
                    if (!int.TryParse(limits, out finish)) { Console.WriteLine("finish argument not a number"); return; }
                    if (finish < 1) { Console.WriteLine("finish should be a positive number"); return; }
                }
            }

            if (!File.Exists(filepath)) { Console.WriteLine("Input file is not exists"); return; }
            if (Path.GetExtension(filepath) != ".obj") { Console.WriteLine("Input file is not OBJ-file"); return; }

            var obj = new ObjParser.Obj();
            obj.LoadObj(filepath);

            var model = new PolygonalModel();
            model.LoadFromObj(obj, start, finish);
            model.RenamePolygons();

            var grid = model
                .Clone()
                .Align()
                .SplitToGrid();

            var modelObj = model.LoadToObj();
            modelObj.WriteObjFile("model.obj", new string[0]);

            var gridObj = grid.LoadToObj();
            gridObj.WriteObjFile("grid.obj", new string[0]);
        }
    }
}
