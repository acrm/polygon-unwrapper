using System;

namespace PolygonUnwrapper.PolygonTool
{
    public class Vec3
    {
        public double X;
        public double Y;
        public double Z;

        public static readonly Vec3 Up = new Vec3(0, 1, 0);
        public static readonly Vec3 Down = new Vec3(0, -1, 0);
        public static readonly Vec3 Right = new Vec3(1, 0, 0);
        public static readonly Vec3 Left = new Vec3(-1, 0, 0);
        public static readonly Vec3 Front = new Vec3(0, 0, 1);
        public static readonly Vec3 Back = new Vec3(0, 0, -1);

        public Vec3() { }

        public Vec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() => $"{X.ToString("F3")} {Y.ToString("F3")} {Z.ToString("F3")}";

        public Vec3 Add(Vec3 other) => new Vec3(X + other.X, Y + other.Y, Z + other.Z);
        public Vec3 Sub(Vec3 other) => new Vec3(X - other.X, Y - other.Y, Z - other.Z);
        public Vec3 Mul(double scalar) => new Vec3(X*scalar, Y*scalar, Z*scalar);

        public double Dot(Vec3 other) => X*other.X + Y*other.Y + Z*other.Z;
        public Vec3 Cross(Vec3 other)
            => new Vec3(
                Y*other.Z - Z*other.Y,
                Z*other.X - X*other.Z,
                X*other.Y - Y*other.X);

        public double Length() => Math.Sqrt(Dot(this));

        public Vec3 Normalized() => Mul(1/Length());

        public double Angle(Vec3 other) => Math.Acos(Dot(other)/(Length() * other.Length()));

        public Vec3 Rotate(Vec3 axis, double theta)
        {
            var vec = new Vec3();
            var costheta = Math.Cos(theta);
            var sintheta = Math.Sin(theta);
            axis = axis.Normalized();

            vec.X += (costheta + (1 - costheta) * axis.X * axis.X) * X;
            vec.X += ((1 - costheta) * axis.X * axis.Y - axis.Z * sintheta) * Y;
            vec.X += ((1 - costheta) * axis.X * axis.Z + axis.Y * sintheta) * Z;

            vec.Y += ((1 - costheta) * axis.X * axis.Y + axis.Z * sintheta) * X;
            vec.Y += (costheta + (1 - costheta) * axis.Y * axis.Y) * Y;
            vec.Y += ((1 - costheta) * axis.Y * axis.Z - axis.X * sintheta) * Z;

            vec.Z += ((1 - costheta) * axis.X * axis.Z - axis.Y * sintheta) * X;
            vec.Z += ((1 - costheta) * axis.Y * axis.Z + axis.X * sintheta) * Y;
            vec.Z += (costheta + (1 - costheta) * axis.Z * axis.Z) * Z;

            return vec;
        }
    }
}
