using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolygonUnwrapper.ObjParser.Types;

namespace PolygonUnwrapper.ObjParser
{
	public class Obj
	{
		public static string CurrentGroupName { get; set; }

		public List<Vertex> VertexList;
		public List<Face> FaceList;

		public Extent Size { get; set; }

        /// <summary>
        /// Constructor. Initializes VertexList, FaceList and TextureList.
        /// </summary>
	    public Obj()
	    {
            VertexList = new List<Vertex>();
            FaceList = new List<Face>();
        }

        /// <summary>
        /// Load .obj from a filepath.
        /// </summary>
        /// <param name="file"></param>
        public void LoadObj(string path)
        {
            LoadObj(File.ReadAllLines(path));
        }

        /// <summary>
        /// Load .obj from a stream.
        /// </summary>
        /// <param name="file"></param>
	    public void LoadObj(Stream data)
	    {
            using (var reader = new StreamReader(data))
            {
                LoadObj(reader.ReadToEnd().Split(Environment.NewLine.ToCharArray()));
            }
	    }

        /// <summary>
        /// Load .obj from a list of strings.
        /// </summary>
        /// <param name="data"></param>
	    public void LoadObj(IEnumerable<string> data)
	    {
            foreach (var line in data)
            {
                processLine(line);
            }

            updateSize();
        }

		public void WriteObjFile(string path, string[] headerStrings)
		{
			if (File.Exists(path)) File.Delete(path);

			using (var outStream = File.OpenWrite(path))
			using (var writer = new StreamWriter(outStream))
			{
				// Write some header data
			    WriteHeader(writer, headerStrings);

				VertexList.ForEach(v => writer.WriteLine(v));
				foreach (Face face in FaceList)
				{
					writer.WriteLine(face);
				}
			}
		}

	    private void WriteHeader(StreamWriter writer, string[] headerStrings)
	    {
	        if (headerStrings == null || headerStrings.Length == 0)
	        {
	            writer.WriteLine("# Generated by ObjParser");
	            return;
	        }

	        foreach (var line in headerStrings)
	        {
	            writer.WriteLine("# " + line);
	        }
	    }

	    /// <summary>
		/// Sets our global object size with an extent object
		/// </summary>
		private void updateSize()
		{
            // If there are no vertices then size should be 0.
	        if (VertexList.Count == 0)
	        {
	            Size = new Extent
	            {
                    XMax = 0,
                    XMin = 0,
                    YMax = 0,
                    YMin = 0,
                    ZMax = 0,
                    ZMin = 0
	            };

	            // Avoid an exception below if VertexList was empty.
	            return;
	        }

			Size = new Extent
			{
				XMax = VertexList.Max(v => v.X),
				XMin = VertexList.Min(v => v.X),
				YMax = VertexList.Max(v => v.Y),
				YMin = VertexList.Min(v => v.Y),
				ZMax = VertexList.Max(v => v.Z),
				ZMin = VertexList.Min(v => v.Z)
			};		
		}

		static int faceCounter = 0;
		/// <summary>
		/// Parses and loads a line from an OBJ file.
		/// Currently only supports V, VT, F and MTLLIB prefixes
		/// </summary>		
		private void processLine(string line)
		{
			if (faceCounter > 100) return;

			string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length > 0)
			{
				switch (parts[0])
				{
					case "g":
						CurrentGroupName = string.Join(" ", parts.Skip(1));
						if (string.IsNullOrWhiteSpace(CurrentGroupName))
							CurrentGroupName = null;
						break;
					case "v":
						Vertex v = new Vertex();
						v.LoadFromStringArray(parts);
						VertexList.Add(v);
						v.Index = VertexList.Count();
						break;
					case "f":
						Face f = new Face();
						if (CurrentGroupName != null)
							f.GroupName = CurrentGroupName;
						f.LoadFromStringArray(parts);
						FaceList.Add(f);
						faceCounter++;
						break;
					case "vt":
						TextureVertex vt = new TextureVertex();
						vt.LoadFromStringArray(parts);
						break;

				}
			}
		}

	}
}