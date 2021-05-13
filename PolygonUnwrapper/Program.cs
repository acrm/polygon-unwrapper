using PolygonUnwrapper.ObjParser.Types;
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

            if (!File.Exists(filepath)) { Console.WriteLine("Input file is not exists"); return; }
            if (Path.GetExtension(filepath) != ".obj") { Console.WriteLine("Input file is not OBJ-file"); return; }

            var model = new ObjParser.Obj();
            model.LoadObj(filepath);

            var side = Math.Ceiling(Math.Sqrt(model.FaceList.Count)) + 1;

            var row = 0;
            var col = 0;
            var newVertices = new List<Vertex>(model.VertexList.Count);
            var newFaces = new List<Face>(model.FaceList.Count);
            for (var i = 0; i < model.FaceList.Count; i++)
            {
                var face = model.FaceList[i];
                var pos = new Vertex() { X = col*10, Y = row*10, Z = 0 };
                col++;
                if (col >= side)
                {
                    row++;
                    col = 0;
                    continue;
                }
                var newFace = new Face();
                var newVertexIndexList = new List<int>();
                Vertex offset = null;
                foreach (var vIndex in face.VertexIndexList)
                {
                    var v = model.VertexList[vIndex - 1];
                    if (offset == null)
                    {
                        offset = new Vertex().Sub(v);
                    }
                    newVertices.Add(v.Add(offset).Add(pos));
                    newVertexIndexList.Add(newVertices.Count);
                }
                newFace.VertexIndexList = newVertexIndexList.ToArray();
                newFaces.Add(newFace);
            }

            var groupNumber = 1;
            foreach (var face in newFaces)
            {
                face.GroupName = $"G{groupNumber++}";
            }

            model.VertexList = newVertices;
            model.FaceList = newFaces;

            model.WriteObjFile("output.obj", new string[0]);
        }
    }
}
