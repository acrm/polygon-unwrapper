using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonUnwrapper.PolygonTool
{
    public class PolygonsPage
    {
        public Vec2 TopLeft { get; set; }

        private readonly List<RectangularPair> _pairs = new List<RectangularPair>();
        public IReadOnlyList<RectangularPair> Pairs => _pairs;

        private readonly List<Polygon3D> _polygons = new List<Polygon3D>();
        public IReadOnlyList<Polygon3D> Polygons => _polygons;
    }
}
