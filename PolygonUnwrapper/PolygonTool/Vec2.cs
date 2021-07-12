using System;

namespace PolygonUnwrapper.PolygonTool
{
    public class Vec2
    {
        public double X;
        public double Y;

        public static readonly Vec2 Up = new Vec2(0, 1);
        public static readonly Vec2 Down = new Vec2(0, -1);
        public static readonly Vec2 Right = new Vec2(1, 0);
        public static readonly Vec2 Left = new Vec2(-1, 0);

        public Vec2() { }

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"{X.ToString("F3")} {Y.ToString("F3")}";

        public Vec2 Add(Vec2 other) => new Vec2(X + other.X, Y + other.Y);
        public Vec2 Sub(Vec2 other) => new Vec2(X - other.X, Y - other.Y);
        public Vec2 Mul(double scalar) => new Vec2(X*scalar, Y*scalar);

        public double Dot(Vec2 other) => X*other.X + Y*other.Y;

        public double Length() => Math.Sqrt(Dot(this));

        public Vec2 Normalized() => Mul(1/Length());

        public double Angle(Vec2 other) => Math.Acos(Dot(other)/(Length() * other.Length()));

        public Vec2 Rotate(double theta)
        {
            var vec = new Vec2();
            var costheta = Math.Cos(theta);
            var sintheta = Math.Sin(theta);

            vec.X += costheta * X;
            vec.X += -sintheta * Y;

            vec.Y += sintheta * X;
            vec.Y += costheta * Y;

            return vec;
        }
    }
}
