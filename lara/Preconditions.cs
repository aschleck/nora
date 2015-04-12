using System;

namespace nora.lara {
    public class Preconditions {

        public static void CheckArgument(bool test) {
            if (!test) {
                throw new ArgumentException();
            }
        }

        public static void CheckArgument(bool test, string message) {
            if (!test) {
                throw new ArgumentException(message);
            }
        }

        public static T CheckNotNull<T>(T obj) {
            if (obj == null) {
                throw new ArgumentNullException();
            }

            return obj;
        }

        public static T CheckNotNull<T>(T obj, string message) {
            if (obj == null) {
                throw new ArgumentNullException(message);
            }

            return obj;
        }
    }
}
