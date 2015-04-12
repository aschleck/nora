using System;

namespace nora.lara.state {

    public struct Vector {

        public static bool operator ==(Vector a, Vector b) {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vector a, Vector b) {
            return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
        }

        public readonly float X, Y, Z;

        public Vector(float x, float y, float z) {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override bool Equals(object obj) {
            if (!(obj is Vector)) {
                return false;
            }
            Vector o = (Vector) obj;
            return X == o.X && Y == o.Y && Z == o.Z;
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + X.GetHashCode();
            hash = hash * 23 + Y.GetHashCode();
            hash = hash * 23 + Z.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return String.Format("<{0}, {1}, {2}>", X, Y, Z);
        }
    }

    public struct VectorXy {

        public readonly float X, Y;

        public VectorXy(float x, float y) {
            this.X = x;
            this.Y = y;
        }

        public override string ToString() {
            return String.Format("<{0}, {1}>", X, Y);
        }
    }
}
