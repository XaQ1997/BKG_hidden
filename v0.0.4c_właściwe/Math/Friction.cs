using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKG.Math
{
    static class Friction
    {
        public static int[] FrictionSum(int[,] frictions)
        {
            int[] result = new int[2];
            result[0] = 0;

            int rowCount = frictions.GetLength(0);
            int[] denominators = new int[rowCount];

            for (int i = 0; i < rowCount; ++i)
                denominators[i] = frictions[i, 1];

            result[1] = MathOperations.NWW(denominators);

            for (int i = 0; i < rowCount; ++i)
                result[0] += FrictionValue(frictions[i, 0], frictions[i, 1], result[1]);

            return result;
        }

        public static int FrictionValue(int[] friction, int denominator)
        {
            return FrictionValue(friction[0], friction[1], denominator);
        }

        public static int FrictionValue(int numerator, int denominator, int commonDenominator)
        {
            int result = (int)((long)numerator * commonDenominator / denominator);

            return result;
        }
    }
}
