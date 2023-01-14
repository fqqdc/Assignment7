using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structs
{
    public struct Random
    {
        private Int64 seed;

        public Random(int seed) => this.seed = seed;

        public Random() : this(Environment.TickCount) { }

        private void UpdateSeed()
        {
            seed = (314159269L * seed + 453806245L) % 2147483648L;
        }

        public long Next()
        {
            UpdateSeed();
            return seed;
        }


        public float NextFloat()
        {
            UpdateSeed();
            return (float)seed / 2147483648L;
        }

        public int NextInt(int min, int max)
        {
            UpdateSeed();
            return (int)(NextFloat() * (max - min) + min);
        }
    }
}
