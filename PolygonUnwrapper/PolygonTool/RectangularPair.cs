using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonUnwrapper.PolygonTool
{
    public class RectangularPair
    {
        public Polygon3D First;

        public Polygon3D Second;

        public static RectangularPair Place(Polygon3D first, Polygon3D second, double margin)
        {
            var pair = new RectangularPair { First = first, Second = second };
            second.Translate(first.Vertices.First().Sub(second.Vertices.First()));
            return pair;
        }
    }
}
