using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKG.Math
{
    class Friction
    {
        public int[] FrictionSum(int[,] frictions)
        {
            int[] result = new int[2];

            int[] denominators = new int[frictions.Length];

            for (int i = 0; i < frictions.Length; ++i)
                denominators[i] = frictions[i, 1];

            result[1] = (new MathOperations()).NWW(denominators);

            for (int i = 0; i < frictions.Length; ++i)
                result[0] += FrictionValue(new int[2] { frictions[i, 0], frictions[i, 1] }, result[1]);

            return result;
        }

        public int FrictionValue(int[] friction, int denominator)
        {
            int result = friction[0] * denominator / friction[1];

            return result;
        }
    }
}
