using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Structs
{
    public struct Random
    {
        ulong seed;

        public Random() => seed = (ulong)DateTime.Now.Ticks;
        public Random(ulong _seed) => seed = _seed;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateSeed()
        {
            const ulong a = 25214903917ul;
            const ulong c = 11ul;
            const ulong m = (1ul << 48) - 1;

            seed = (a * seed + c) & m;
        }

        public ulong NextUInt64()
        {
            UpdateSeed();
            var a =  BitOperations.RotateLeft(seed, 5);
            UpdateSeed();
            var b = BitOperations.RotateRight(seed, 29);

            return seed = a ^ b;
        }

        public long NextInt64()
        {
            while (true)
            {
                // Get top 63 bits to get a value in the range [0, long.MaxValue], but try again
                // if the value is actually long.MaxValue, as the method is defined to return a value
                // in the range [0, long.MaxValue).
                ulong result = NextUInt64() >> 1;
                if (result != long.MaxValue)
                {
                    return (long)result;
                }
            }
        }

        public int Next()
        {
            while (true)
            {
                // Get top 31 bits to get a value in the range [0, int.MaxValue], but try again
                // if the value is actually int.MaxValue, as the method is defined to return a value
                // in the range [0, int.MaxValue).
                ulong result = NextUInt64() >> 33;
                if (result != int.MaxValue)
                {
                    return (int)result;
                }
            }
        }

        public int Next(int maxValue)
        {
            Debug.Assert(maxValue >= 0);

            return (int)NextUInt32((uint)maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            Debug.Assert(minValue <= maxValue);

            return (int)NextUInt32((uint)(maxValue - minValue)) + minValue;
        }

        public double NextDouble() =>
            // As described in http://prng.di.unimi.it/:
            // "A standard double (64-bit) floating-point number in IEEE floating point format has 52 bits of significand,
            //  plus an implicit bit at the left of the significand. Thus, the representation can actually store numbers with
            //  53 significant binary digits. Because of this fact, in C99 a 64-bit unsigned integer x should be converted to
            //  a 64-bit double using the expression
            //  (x >> 11) * 0x1.0p-53"
            (NextUInt64() >> 11) * (1.0 / (1ul << 53));

        public float NextSingle() =>
            // Same as above, but with 24 bits instead of 53.
            (NextUInt64() >> 40) * (1.0f / (1u << 24));


        public uint NextUInt32() => (uint)(NextUInt64() >> 32);

        public uint NextUInt32(uint maxValue)
        {
            ulong randomProduct = (ulong)maxValue * NextUInt32();
            uint lowPart = (uint)randomProduct;

            if (lowPart < maxValue)
            {
                uint remainder = (0u - maxValue) % maxValue;

                while (lowPart < remainder)
                {
                    randomProduct = (ulong)maxValue * NextUInt32();
                    lowPart = (uint)randomProduct;
                }
            }

            return (uint)(randomProduct >> 32);
        }

        public void NextBytes(byte[] buffer) => NextBytes((Span<byte>)buffer);

        public void NextBytes(Span<byte> buffer)
        {
            while (buffer.Length >= sizeof(ulong))
            {
                ulong s = NextUInt64();

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), s);

                buffer = buffer.Slice(sizeof(ulong));
            }

            if (!buffer.IsEmpty)
            {
                ulong s = NextUInt64();

                var remainingBytes = MemoryMarshal.AsBytes<ulong>(new(ref s));
                Debug.Assert(buffer.Length < sizeof(ulong));

                //for (int i = 0; i < buffer.Length; i++)
                //{
                //    buffer[i] = remainingBytes[i];
                //}
                remainingBytes.Slice(0, buffer.Length).CopyTo(buffer);
            }
        }
    }
}
