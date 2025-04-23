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
        private PrimeNumbers prime = new PrimeNumbers();
        private Friction frictions = new Friction();
        private Statistic statistic = new Statistic();

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
            int[] primeNumbers = (prime.EratostenesSieve(count)).ToArray();

            for (int i = 0; i < count; ++i)
                result += seed % primeNumbers[i];

            return result;
        }

        public int CreatingSeed(int seed, int bottom, int top, int level, int[] offsets, Vector2Int pos, float scale=1.0f)
        {
            int result = 0;
            float perlinScale = 0.1f*scale;

            int[,] chances = new int[top - bottom, 2];

            for (int i = 0; i < chances.GetLength(0); ++i)
                chances[i, 0] = 1;

            chances[level - bottom, 1] = 1;

            int down = level - bottom;
            int[] primeNumbers = PrimeNumbers(down);

            for (int i = 0; i < down; ++i)
                chances[i, 1] = primeNumbers[primeNumbers.Length - i - 1];

            int up = top - level;
            primeNumbers = PrimeNumbers(up - 1);

            for (int i = 0; i < up-1; ++i)
                chances[down + i + 1, 1] = primeNumbers[i];
            
            int[] friction = frictions.FrictionSum(chances);

            int[] temp = new int[chances.GetLength(0)];

            for (int i = 0; i < temp.Length; ++i)
                temp[i] = frictions.FrictionValue(new int[2] { temp[i], chances[i, 1] }, friction[1]);

            Dictionary<string, int[]> tmp = statistic.Chance(temp);

            friction[0] *= tmp["Mnożnik"][0];
            friction[1] *= tmp["Mnożnik"][0];

            int seeding = PrimeNumberModularSeed(friction[0], seed) % friction[0];

            for (int i = 0; i < temp.Length; ++i)
            {
                if (statistic.Distributor(temp, i) >= seeding)
                {
                    result = i;
                    break;
                }
            }

            float flatnessFactor = Mathf.Clamp(Mathf.PerlinNoise(pos.x * perlinScale * 0.5f, pos.y * perlinScale * 0.5f), offsets[0], offsets[1]);
            float perlinNoiseValue = Mathf.PerlinNoise((pos.x + DividingSeed(seed, true)) * perlinScale, (pos.y + DividingSeed(seed, false)) * perlinScale);

            perlinNoiseValue = Mathf.Lerp(perlinNoiseValue, 0.5f, flatnessFactor);

            int perlinResult = Mathf.FloorToInt(perlinNoiseValue * (top - bottom)) + bottom;

            result = Mathf.Clamp(perlinResult + (result + bottom) / level, bottom, top);

            return result;
        }

        public int DividingSeed(int seed, bool isMain)
        {
            int[] dividedSeeds = new int[2];

            dividedSeeds[0] = seed >> 16;
            dividedSeeds[1] = seed & ((1 << 16) - 1);

            if (isMain == true)
                return dividedSeeds[0];

            return dividedSeeds[1];
        }

        public int ChunkSeed(int seed, Vector2Int pos)
        {
            int[] dividedSeeds = new int[2] { DividingSeed(seed, true), DividingSeed(seed, false) };

            int result = ((dividedSeeds[0] << 16) * 257 * pos.x) + (dividedSeeds[1] * 17 * pos.y) + seed;

            return result;
        }

        public int LocalSeed(int seed, Vector2Int pos)
        {
            int[] dividedSeeds = new int[2] { DividingSeed(seed, true), DividingSeed(seed, false) };

            int result = ((dividedSeeds[0] << 16) * 17 * pos.x) + (dividedSeeds[1] * pos.y) + seed;

            return result;
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

        public bool IsShownChunk(Vector2Int center, Vector2Int chunkPos, int renderDistance)
        {
            int dx = center.x - chunkPos.x;
            int dz = center.y - chunkPos.y;
            return (dx * dx + dz * dz) <= (renderDistance * renderDistance);
        }

        public int[] PrimeNumbers(int target)
        {
            List<int> result = new List<int>();

            int sum = 0;
            int count = 1;

            while (sum < target)
            {
                int nextPrime = prime.FindPrimeNumber(result);

                for (int i = 0; i < nextPrime; ++i)
                {
                    if (sum >= target)
                        break;
                    
                    result.Add((int)Mathf.Pow(2, count));
                    ++sum;
                }

                count++;
            }

            return result.ToArray();
        }
    }
}
