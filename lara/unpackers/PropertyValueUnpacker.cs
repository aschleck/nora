using log4net;
using nora.lara.state;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nora.lara.unpackers {

    public class PropertyValueUnpacker {

        private static readonly ILog log = LogManager.GetLogger(typeof(PropertyValueUnpacker));

        public const int COORD_INTEGER_BITS = 14;
        public const int COORD_FRACTIONAL_BITS = 5;
        public const int COORD_DENOMINATOR = (1 << (COORD_FRACTIONAL_BITS));
        public const double COORD_RESOLUTION = (1.0 / (COORD_DENOMINATOR));

        public const int NORMAL_FRACTIONAL_BITS = 11;
        public const int NORMAL_DENOMINATOR = ((1 << (NORMAL_FRACTIONAL_BITS)) - 1);
        public const double NORMAL_RESOLUTION = (1.0 / (NORMAL_DENOMINATOR));

        private const uint MAX_STRING_LENGTH = 0x200;

        public uint UnpackInt(PropertyInfo info, Bitstream stream) {
            var flags = info.Flags;

            if (flags.HasFlag(PropertyInfo.MultiFlag.EncodedAgainstTickcount)) {
                if (flags.HasFlag(PropertyInfo.MultiFlag.Unsigned)) {
                    return stream.ReadVarUInt();
                } else {
                    uint value = stream.ReadVarUInt();
                    return unchecked((uint) ((-(value & 1)) ^ (value >> 1)));
                }
            } else {
                byte numBits = info.NumBits;

                uint isUnsigned = Convert.ToUInt32(flags.HasFlag(PropertyInfo.MultiFlag.Unsigned));
                uint signer = (0x80000000 >> (32 - numBits)) & unchecked((uint) (isUnsigned - 1));

                uint value = stream.ReadBits(numBits) ^ signer;
                return value - signer;
            }
        }

        public float UnpackFloat(PropertyInfo info, Bitstream stream) {
            var flags = info.Flags;

            if (flags.HasFlag(PropertyInfo.MultiFlag.Coord)) {
                return UnpackFloatCoord(stream);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.CoordMp)) {
                return UnpackFloatCoordMp(stream, FloatType.None);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.CoordMpLowPrecision)) {
                return UnpackFloatCoordMp(stream, FloatType.LowPrecision);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.CoordMpIntegral)) {
                return UnpackFloatCoordMp(stream, FloatType.Integral);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.NoScale)) {
                return UnpackFloatNoScale(stream);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.Normal)) {
                return UnpackFloatNormal(stream);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.CellCoord)) {
                return UnpackFloatCellCoord(info, stream, FloatType.None);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.CellCoordLowPrecision)) {
                return UnpackFloatCellCoord(info, stream, FloatType.LowPrecision);
            } else if (flags.HasFlag(PropertyInfo.MultiFlag.CellCoordIntegral)) {
                return UnpackFloatCellCoord(info, stream, FloatType.Integral);
            } else {
                uint dividend = stream.ReadBits(info.NumBits);
                uint divisor = (uint) (1 << info.NumBits) - 1;

                float f = ((float) dividend) / divisor;
                float range = info.HighValue - info.LowValue;

                return f * range + info.LowValue;
            }
        }

        private float UnpackFloatCoord(Bitstream stream) {
            bool hasInteger = stream.ReadBool();
            bool hasFraction = stream.ReadBool();

            if (hasInteger || hasFraction) {
                bool sign = stream.ReadBool();

                uint integer = 0;
                if (hasInteger) {
                    integer = stream.ReadBits(COORD_INTEGER_BITS) + 1;
                }

                uint fraction = 0;
                if (hasFraction) {
                    fraction = stream.ReadBits(COORD_FRACTIONAL_BITS);
                }

                float f = (float) (integer + fraction * COORD_RESOLUTION);

                if (sign) {
                    f *= -1;
                }

                return f;
            } else {
                return 0;
            }
        }

        private float UnpackFloatCoordMp(Bitstream stream, FloatType type) {
            throw new NotImplementedException();
        }

        private float UnpackFloatNoScale(Bitstream stream) {
            byte[] data = stream.ReadManyBits(32);
            return BitConverter.ToSingle(data, 0);
        }

        private float UnpackFloatNormal(Bitstream stream) {
            bool sign = stream.ReadBool();
            uint value = stream.ReadBits(NORMAL_FRACTIONAL_BITS);

            float f = (float) (value * NORMAL_RESOLUTION);

            if (sign) {
                f *= -1;
            }

            return f;
        }

        private float UnpackFloatCellCoord(PropertyInfo info, Bitstream stream, FloatType type) {
            uint value = stream.ReadBits(info.NumBits);
            float f = value;

            if ((value >> 31) > 0) {
                f *= -1;
            }

            if (type == FloatType.None) {
                uint fraction = stream.ReadBits(5);

                return f + 0.03125f * fraction;
            } else if (type == FloatType.LowPrecision) {
                uint fraction = stream.ReadBits(3);

                return f + 0.125f * fraction;
            } else if (type == FloatType.Integral) {
                return f;
            } else {
                throw new InvalidOperationException("Unknown float type");
            }
        }

        public Vector UnpackVector(PropertyInfo info, Bitstream stream) {
            float x = UnpackFloat(info, stream);
            float y = UnpackFloat(info, stream);
            float z;

            if (info.Flags.HasFlag(PropertyInfo.MultiFlag.Normal)) {
                bool sign = stream.ReadBool();

                float f = x * x + y * y;

                if (1 >= f) {
                    z = 0;
                } else {
                    z = (float) Math.Sqrt(1 - f);
                }

                if (sign) {
                    z *= -1;
                }
            } else {
                z = UnpackFloat(info, stream);
            }

            return new Vector(x, y, z);
        }

        public VectorXy UnpackVectorXy(PropertyInfo info, Bitstream stream) {
            var x = UnpackFloat(info, stream);
            var y = UnpackFloat(info, stream);
            return new VectorXy(x, y);
        }

        public string UnpackString(PropertyInfo info, Bitstream stream) {
            uint length = stream.ReadBits(9);

            Preconditions.CheckArgument(length <= MAX_STRING_LENGTH);

            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, (int) length);

            return new String((from byte b in buffer select (char) b).ToArray<char>());
        }

        public void UnpackArray(uint tick, List<Property> elements, PropertyInfo info, Bitstream stream) {
            byte countBits = (byte) (Util.Log2(info.NumElements + 1));
            uint count = stream.ReadBits(countBits);

            if (elements.Count > count) {
                elements.RemoveRange(0, elements.Count - (int) count);
            } else {
                while (elements.Count < count) {
                    elements.Add(Property.For(info.ArrayProp));
                }
            }

            foreach (var element in elements) {
                element.Update(tick, this, stream);
            }
        }

        public ulong UnpackInt64(PropertyInfo info, Bitstream stream) {
            if (info.Flags.HasFlag(PropertyInfo.MultiFlag.EncodedAgainstTickcount)) {
                log.DebugFormat(
                    "{0}.{1} is encoded against tick count", 
                    info.DtName, info.VarName);
                return stream.ReadVarUInt();
            } else {
                bool negate = false;
                byte secondBits = (byte) (info.NumBits - 32);

                if (!info.Flags.HasFlag(PropertyInfo.MultiFlag.Unsigned)) {
                    --secondBits;

                    if (stream.ReadBool()) {
                        negate = true;
                    }
                }

                Preconditions.CheckArgument(info.NumBits >= secondBits);

                ulong a = stream.ReadBits(32);
                ulong b = stream.ReadBits(secondBits);
                ulong value = (b << 32) | a;

                if (negate) {
                    value = unchecked((ulong) ((long) value * -1));
                }

                return value;
            }
        }

        private enum FloatType {
            None,
            LowPrecision,
            Integral,
        }
    }
}
