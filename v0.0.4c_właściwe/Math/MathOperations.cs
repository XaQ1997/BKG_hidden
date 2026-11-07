using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BKG.Math
{
    public static class MathOperations
    {
        private const float NoiseFrequency = 0.016f;
        private const float BaseFrequency = 0.35f;
        private const float PlainsStart = 0.46f;
        private const float PlainsEnd = 0.60f;
        private const float HillDamping = 0.4f;
        private const float PlainsRelief = 0.3f;
        private const int BumpRange = 4;
        private const float BumpChance = 0.35f;
        private const int BumpSalt = 0x7f4a7c15;
        private const float StretchRange = 0.2f;
        private const float WarpStrength = 20f;
        private const float WarpFrequency = 0.8f;
        private const int StretchSalt = 0x2545F491;
        private const float SeedOffsetPeriod = 20000f;
        private const int AxisSaltX = 0x1b873593;
        private const int AxisSaltZ = 0x68e31da4;
        private const int WarpSalt = 0x3c6ef372;
        private const int LocalSalt = 0x5bd1e995;
        private const int MaxMultiplier = 1024;
        private const float MultiplyTolerance = 0.0001f;

        private sealed class WeightTable
        {
            public int[] Cumulative;
            public int Total;
        }

        private static readonly Dictionary<long, WeightTable> weightTables = new Dictionary<long, WeightTable>();

        private static Noise.Layer heightNoise;
        private static Noise.Layer flatnessNoise;
        private static Noise.Layer baseNoise;
        private static Noise.Layer warpNoise;
        private static float stretchX = 1f;
        private static float stretchZ = 1f;
        private static float offsetX;
        private static float offsetZ;
        private static float warpOffsetX;
        private static float warpOffsetZ;
        private static int noiseSeed;
        private static bool isNoiseReady = false;

        public static int NWW(int[] numbers)
        {
            int result = numbers[0];

            for (int i = 1; i < numbers.Length; ++i)
                result = NWW(result, numbers[i]);

            return result;
        }

        public static int NWW(int a, int b)
        {
            int divisor = NWD(a, b);

            return divisor == 0 ? 0 : a / divisor * b;
        }

        public static int NWD(int a, int b)
        {
            int result = 0;

            if (a < b)
                result = EuclideanAlgorithm(a, b);
            else
                result = EuclideanAlgorithm(b, a);

            return result;
        }

        public static int EuclideanAlgorithm(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;

            return result < 0 ? result + modulus : result;
        }

        public static int PrimeNumberModularSeed(int count, int seed)
        {
            int result = 0;
            List<int> primeNumbers = PrimeNumbers.Primes(count);

            for (int i = 0; i < count; ++i)
                result += seed % primeNumbers[i];

            return result;
        }

        public static int CreatingSeed(int seed, int bottom, int top, int level, int[] offsets, Vector2Int pos, int worldSeed, float scale=1.0f)
        {
            int result = 0;
            float frequency = NoiseFrequency * scale;

            WeightTable weights = GetWeights(level - BumpRange, level + BumpRange + 1, level);

            int seeding = PositiveModulo(PrimeNumberModularSeed(weights.Total, seed), weights.Total);

            for (int i = 0; i < weights.Cumulative.Length; ++i)
            {
                if (weights.Cumulative[i] > seeding)
                {
                    result = i;
                    break;
                }
            }

            PrepareNoise(worldSeed);

            heightNoise.baseRoughness = frequency;
            flatnessNoise.baseRoughness = frequency * 0.5f;
            baseNoise.baseRoughness = frequency * BaseFrequency;
            warpNoise.baseRoughness = frequency * WarpFrequency;

            //zniekszalcenie pozycji lamie regularne grzbiety (mniej ukierunkowany teren)
            Vector3 warpPos = new Vector3(pos.x + level * 53.3f + warpOffsetX, 0f, pos.y + level * 29.1f + warpOffsetZ);

            float warpX = (Mathf.Clamp01(Noise.GenerateValue(warpNoise, warpPos)) - 0.5f) * 2f * WarpStrength;
            float warpZ = (Mathf.Clamp01(Noise.GenerateValue(warpNoise, warpPos + new Vector3(173.3f, 0f, 91.7f))) - 0.5f) * 2f * WarpStrength;

            //pozycja swiata rozciagnieta wg ziarna swiata (lekko inny przekroj w x niz w z), przesunieta stala zalezna od warstwy (level)
            Vector3 samplePos = new Vector3((pos.x + warpX) * stretchX + level * 131.7f + offsetX, 0f, (pos.y + warpZ) * stretchZ + level * 71.3f + offsetZ);

            //rowninki: 0 = wzgorza (z garbikami), 1 = gladka rownina
            float flatness = Mathf.Clamp(Mathf.Clamp01(Noise.GenerateValue(flatnessNoise, samplePos)), offsets[0], offsets[1]);
            float plains = Mathf.InverseLerp(PlainsStart, PlainsEnd, flatness);

            plains = plains * plains * (3f - 2f * plains);

            float noiseValue = Mathf.Clamp01(Noise.GenerateValue(heightNoise, samplePos));
            float baseValue = 0.5f + (Mathf.Clamp01(Noise.GenerateValue(baseNoise, samplePos)) - 0.5f) * PlainsRelief;

            noiseValue = Mathf.Lerp(noiseValue, baseValue, HillDamping + (1f - HillDamping) * plains);

            int noiseResult = Mathf.FloorToInt(noiseValue * (top - bottom)) + bottom;

            //garbiki: odchylenie -BumpRange..+BumpRange z wag, slabnace na rowninach i obecne tylko w czesci kolumn
            int bump = Mathf.RoundToInt((result - BumpRange) * (1f - plains));

            if (PositiveModulo(Mix(seed ^ BumpSalt), 100) >= (int)(BumpChance * 100f))
                bump = 0;

            result = Mathf.Clamp(noiseResult + bump, bottom, top);

            return result;
        }

        private static WeightTable GetWeights(int bottom, int top, int level)
        {
            long key = ((long)bottom << 40) | ((long)top << 20) | (long)level;

            WeightTable table;

            if (weightTables.TryGetValue(key, out table))
                return table;

            table = BuildWeights(bottom, top, level);
            weightTables[key] = table;

            return table;
        }

        //wagi to ulamki 1/mianownik, najwieksze przy poziomie (level) i malejace w obie strony
        private static WeightTable BuildWeights(int bottom, int top, int level)
        {
            int[,] chances = new int[top - bottom, 2];

            for (int i = 0; i < chances.GetLength(0); ++i)
                chances[i, 0] = 1;

            chances[level - bottom, 1] = 1;

            int down = level - bottom;
            int[] denominators = WeightDenominators(down);

            for (int i = 0; i < down; ++i)
                chances[i, 1] = denominators[denominators.Length - i - 1];

            int up = top - level;
            denominators = WeightDenominators(up - 1);

            for (int i = 0; i < up - 1; ++i)
                chances[down + i + 1, 1] = denominators[i];

            int[] friction = Friction.FrictionSum(chances);

            WeightTable table = new WeightTable();

            table.Cumulative = new int[chances.GetLength(0)];

            int sum = 0;

            for (int i = 0; i < table.Cumulative.Length; ++i)
            {
                sum += Friction.FrictionValue(chances[i, 0], chances[i, 1], friction[1]);
                table.Cumulative[i] = sum;
            }

            table.Total = sum;

            return table;
        }

        private static void PrepareNoise(int worldSeed)
        {
            if (isNoiseReady && noiseSeed == worldSeed)
                return;

            heightNoise = NewNoiseLayer(worldSeed);
            flatnessNoise = NewNoiseLayer(worldSeed + 1);
            baseNoise = NewNoiseLayer(worldSeed + 2);

            baseNoise.octaves = 1;
            baseNoise.strength = 1f;

            warpNoise = NewNoiseLayer(worldSeed + 3);

            warpNoise.octaves = 1;
            warpNoise.strength = 1f;

            //przesuniecia osi z polowek seeda: dolna polowa dla x, gorna dla z (kazda os liczona inaczej)
            offsetX = AxisOffset(DividingSeed(worldSeed, false), AxisSaltX);
            offsetZ = AxisOffset(DividingSeed(worldSeed, true), AxisSaltZ);
            warpOffsetX = AxisOffset(DividingSeed(worldSeed, true), WarpSalt);
            warpOffsetZ = AxisOffset(DividingSeed(worldSeed, false), WarpSalt ^ AxisSaltZ);

            //rozciagniecie osi zalezne od seeda: raz dluzsze grzbiety wzdluz x, raz wzdluz z
            float exponent = (Mix(worldSeed ^ StretchSalt) & 0xFFFF) / 32767.5f - 1f;

            stretchX = Mathf.Pow(2f, exponent * StretchRange);
            stretchZ = Mathf.Pow(2f, -exponent * StretchRange);

            noiseSeed = worldSeed;
            isNoiseReady = true;
        }

        private static float AxisOffset(int half, int salt)
        {
            return (Mix(half ^ salt) & 0xFFFF) / 65536f * SeedOffsetPeriod;
        }

        //szum znormalizowany do [0, 1]: suma amplitud oktaw to 1 + 0.5 + 0.25
        private static Noise.Layer NewNoiseLayer(int seed)
        {
            Noise.Layer layer = new Noise.Layer();

            layer.noiseType = Noise.Type.Simple;
            layer.generator = new SimplexNoise(seed);
            layer.octaves = 3;
            layer.roughness = 2f;
            layer.persistence = 0.5f;
            layer.threshold = 0f;
            layer.strength = 1f / 1.75f;

            return layer;
        }

        public static int DividingSeed(int seed, bool isMain)
        {
            int[] dividedSeeds = new int[2];

            dividedSeeds[0] = seed >> 16;
            dividedSeeds[1] = seed & ((1 << 16) - 1);

            if (isMain == true)
                return dividedSeeds[0];

            return dividedSeeds[1];
        }

        private static int Mix(int value)
        {
            unchecked
            {
                uint hash = (uint)value;

                hash ^= hash >> 16;
                hash *= 0x7feb352dU;
                hash ^= hash >> 15;
                hash *= 0x846ca68bU;
                hash ^= hash >> 16;

                return (int)hash;
            }
        }

        private static int Hash(int seed, Vector2Int pos)
        {
            unchecked
            {
                return Mix(Mix(Mix(seed) + pos.x) + pos.y);
            }
        }

        public static int ChunkSeed(int seed, Vector2Int pos)
        {
            return Hash(seed, pos);
        }

        public static int LocalSeed(int seed, Vector2Int pos)
        {
            return Hash(seed ^ LocalSalt, pos);
        }

        public static int Multiply(float value)
        {
            int multiplier = 1;

            while (multiplier < MaxMultiplier && Mathf.Abs(value * multiplier - Mathf.Round(value * multiplier)) > MultiplyTolerance)
                ++multiplier;

            return multiplier;
        }

        public static bool IsShownChunk(Vector2Int center, Vector2Int chunkPos, float renderDistance)
        {
            int dx = center.x - chunkPos.x;
            int dz = center.y - chunkPos.y;
            return (dx * dx + dz * dz) <= (renderDistance * renderDistance);
        }

        public static int[] WeightDenominators(int target)
        {
            List<int> result = new List<int>();

            int sum = 0;
            int count = 1;
            int index = 0;

            while (sum < target)
            {
                int nextPrime = PrimeNumbers.Primes(index + 1)[index];

                ++index;

                for (int i = 0; i < nextPrime; ++i)
                {
                    if (sum >= target)
                        break;

                    result.Add(1 << count);
                    ++sum;
                }

                count++;
            }

            return result.ToArray();
        }
    }
}
