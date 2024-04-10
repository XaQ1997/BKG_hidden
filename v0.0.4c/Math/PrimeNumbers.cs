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

            List<int> result = new List<int>(2);

            for (int i = 3; result.Count < count; i+=2)
            {
                bool isPrime = true;

                for (int j = 3; j * j <= i; j+=2)
                {
                    if (i % j == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }

                if (isPrime)
                {
                    result.Add(i);
                }
            }

            return result;
        }
    }
}
