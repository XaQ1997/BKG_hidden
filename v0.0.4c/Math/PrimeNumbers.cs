using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKG.Math
{
    class PrimeNumbers
    {
        public List<int> EratostenesSieve(int count)
        {
            if (count < 1)
                return new List<int>();

            List<int> result = new List<int> { 2 };

            for (int i = 1; i < count; ++i)
                result.Add(FindPrimeNumber(result));

            return result;
        }

        public int FindPrimeNumber(List<int> smallerPrimeNumbers)
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
