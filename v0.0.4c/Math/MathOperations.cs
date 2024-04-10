using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BKG.Math
{
    public class MathOperations
    {
        public int NWW(int[] numbers)
        {
            int result = numbers[0];

            for (int i = 1; i < numbers.Length; ++i)
                result = (result * numbers[i]) / NWD(result, numbers[i]);

            return result;
        }

        public int NWD(int a, int b)
        {
            int result = 0;

            if (a < b)
                result = EuclideanAlgorithm(a, b);
            else
                result = EuclideanAlgorithm(b, a);

            return result;
        }

        public int EuclideanAlgorithm(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public int PrimeNumberModularSeed(int count, int seed)
        {
            int result = 0;
            int[] primeNumbers = (new PrimeNumbers()).EratostenesSieve(count).ToArray();

            for (int i = 0; i < count; ++i)
                result += seed % primeNumbers[i];

            return result;
        }

        public int CreatingSeed(int seed, int bottom, int top, int level)
        {
            int[,] chances = new int[top - bottom, 2];

            for (int i = 0; i < chances.Length; ++i)
                chances[i, 0] = 1;

            chances[level - bottom, 1] = 1;

            for (int i = 1; i <= level - bottom; ++i)
                chances[level - bottom - i, 1] = i + 1;

            for (int i = 1; i < top-level; ++i)
                chances[level - bottom + i, 1] = i + 1;

            int[] friction = (new Friction()).FrictionSum(chances);

            int[] temp = new int[chances.Length];

            for (int i = 0; i < temp.Length; ++i)
                temp[i] = (new Friction()).FrictionValue(new int[2] { temp[i], chances[i, 1] }, friction[1]);

            Dictionary<string, int[]> tmp = (new Statistic()).Chance(temp);

            friction[0] *= tmp["Mnożnik"][0];
            friction[1] *= tmp["Mnożnik"][0];


            int result = 0;

            int seeding = PrimeNumberModularSeed(friction[0], seed)%friction[0];

            for(int i=0;i<temp.Length;++i)
            {
                if((new Statistic()).Distributor(temp, i)>=seeding)
                {
                    result = bottom+i;
                    break;
                }
            }

            return result;
        }

        public int DividingSeed(int seed, bool isMain)
        {
            int[] dividedSeeds = new int[2];

            dividedSeeds[0] = seed >> 16;
            dividedSeeds[1] = seed & ((1 << 16) - 1);

            if (isMain==true)
                return dividedSeeds[0];

            return dividedSeeds[1];
        }

        public int Multiply(int value)
        {
            int multiplier = 1;

            while (value % 1 != 0)
            {
                value += value;
                multiplier++;
            }

            return multiplier;
        }

        public int CalculatingOffset(int seed, int minOffset, int min, int maxOffset, int max, int defaultLevel, int currentLevel)
        {
            int result = 0;

            int down = Mathf.Max(min, currentLevel + minOffset);
            int up = Mathf.Min(max, currentLevel + maxOffset);

            int level = (int)Mathf.Sign(currentLevel - defaultLevel);

            int[,] chances = new int[up - down, 2];

            for (int i = 0; i < chances.Length; ++i)
                chances[i, 0] = 1;

            chances[currentLevel - down, 1] = 1;

            for (int i = 1; i <= currentLevel - down; ++i)
                chances[currentLevel - down - i, 1] = i + 1;

            for (int i = 1; i < up - currentLevel; ++i)
                chances[up - currentLevel + i, 1] = i + 1;

            int[,] tmp = new int[up - down, 2];

            for (int i = 0; i < tmp.Length; ++i)
            {
                tmp[i, 1] = 1;
                tmp[i, 0] = 1;
            }

            if (level == 1)
            {
                if (maxOffset > max - currentLevel)
                    for (int i = 1; i < tmp.Length; ++i)
                        tmp[i, 1] = i + 1;
                else
                {
                    int tempy = max - currentLevel;

                    for (int i = tempy + 1; i < max; ++i)
                        tmp[i, 1] = i - tempy + 1;

                    for (int i = 0; i < tempy; ++i)
                        tmp[i, 1] = tempy - i + 1;
                }
            }
            if(level==-1)
            {
                if (minOffset < currentLevel - min)
                    for (int i = 1; i < tmp.Length; ++i)
                        tmp[i, 1] = tmp.Length - i + 1;
                else
                {
                    int tempy = currentLevel - min;


                    for (int i = tempy + 1; i < max; ++i)
                        tmp[i, 1] = i - tempy + 1;

                    for (int i = 0; i < tempy; ++i)
                        tmp[i, 1] = tempy - i + 1;
                }
            }

            for (int i = 0; i < chances.Length; ++i)
                chances[i, 1] *= tmp[i, 1];

            int[] friction = (new Friction()).FrictionSum(chances);

            int[] temp = new int[chances.Length];

            for (int i = 0; i < temp.Length; ++i)
                temp[i] = (new Friction()).FrictionValue(new int[2] { temp[i], chances[i, 1] }, friction[1]);

            Dictionary<string, int[]> temporary = (new Statistic()).Chance(temp);

            friction[0] *= temporary["Mnożnik"][0];
            friction[1] *= temporary["Mnożnik"][0];

            int seeding = PrimeNumberModularSeed(friction[0], seed) % friction[0];

            for (int i = 0; i < temp.Length; ++i)
            {
                if ((new Statistic()).Distributor(temp, i) >= seeding)
                {
                    result = down + i;
                    break;
                }
            }

            return result;
        }

        public int CalculatingAllignOffset(int seed, int min, int minOffset, int max, int maxOffset, int defaultLevel, int[] layer)
        {
            int result = 0;

            int[] range = new int[2];

            if(layer[0]<=layer[1])
            {
                range[0] = Mathf.Max(min, layer[1] + minOffset);
                range[1] = Mathf.Min(max, layer[0] + maxOffset);
            }
            else
            {
                range[0] = Mathf.Max(min, layer[0] + minOffset);
                range[1] = Mathf.Min(max, layer[1] + maxOffset);
            }

            return result;
        }
    }
}
