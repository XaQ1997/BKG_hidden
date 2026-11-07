using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BKG.Math
{
    static class Statistic
    {
        public static int Distributor(int[] numbers, int index)
        {
            int sum = 0;

            for (int i = 0; i <= index; ++i)
                sum += numbers[i];

            return sum;
        }

        public static Dictionary<string, int[]> Chance(float[] numbers)
        {
            Dictionary<string, int[]> result = new Dictionary<string, int[]>();

            int multiplication = 1;

            for (int i = 0; i < numbers.Length; ++i)
                multiplication = MathOperations.NWW(multiplication, MathOperations.Multiply(numbers[i]));

            int[] tmp = new int[numbers.Length];

            for (int i = 0; i < tmp.Length; ++i)
                tmp[i] = Mathf.RoundToInt(numbers[i] * multiplication);

            result["Wartości"] = tmp;
            result["Mnożnik"] = new int[1] { multiplication };

            return result;
        }
    }
}
