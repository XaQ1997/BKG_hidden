using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKG.Math
{
    static class PrimeNumbers
    {
        private static readonly List<int> cache = new List<int> { 2 };

        public static List<int> Primes(int count)
        {
            while (cache.Count < count)
                cache.Add(FindPrimeNumber(cache));

            return cache;
        }

        public static List<int> EratostenesSieve(int count)
        {
            if (count < 1)
                return new List<int>();

            return Primes(count).GetRange(0, count);
        }

        public static int FindPrimeNumber(List<int> smallerPrimeNumbers)
        {
            if (smallerPrimeNumbers == null || smallerPrimeNumbers.Count == 0)
            {
                return 2;
            }

            int lastPrime = smallerPrimeNumbers[smallerPrimeNumbers.Count - 1];
            int candidate = lastPrime + 1;

            while (true)
            {
                bool isPrime = true;

                foreach (var prime in smallerPrimeNumbers)
                {
                    if (prime * prime > candidate)
                    {
                        break;
                    }
                    if (candidate % prime == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }

                if (isPrime)
                {
                    return candidate;
                }

                candidate++;
            }
        }
    }
}
