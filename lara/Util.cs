namespace nora.lara {

    public class Util {

        public static byte Log2(uint n) {
            Preconditions.CheckArgument(n >= 1);

            byte r = 0;
            uint acc = 1;

            while (acc < n) {
                ++r;
                acc *= 2;
            }

            return r;
        }
    }
}
