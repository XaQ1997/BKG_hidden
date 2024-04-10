using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKG.Math
{
    class Statistic
    {
        public int Distributor(int[] numbers, int index)
        {
            if (index == 0)
                return numbers[0];

            return numbers[index] + Distributor(numbers, index - 1);
        }

        public Dictionary<string, int[]> Chance(int[] numbers)
        {
            Dictionary<string, int[]> result = new Dictionary<string, int[]>();

            int[] tmp = new int[numbers.Length];

            int multiplication = 1;

            for(int i=0;i<numbers.Length;++i)
            {
                tmp[i] = numbers[i];

                if (numbers[i]%1!=0)
                    multiplication *= (new MathOperations()).Multiply(numbers[i]);
            }

            for (int i = 0; i < tmp.Length; ++i)
                tmp[i] *= multiplication;

            result["Wartości"] = tmp;
            result["Mnożnik"] = new int[1];

            result["Mnożnik"][0] = multiplication;

            return result;
        }
    }
}
