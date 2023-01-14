using ILGPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structs
{
    public struct IntStack
    {
        private int[] values;
        private int top;

        public IntStack(int size)
        {
            values = new int[size];
        }
        public readonly int Count
        {
            get => top;
        }
        public void Push(int value)
        {
            values[top] = value;
            if (top < values.Length) top++;
            else Interop.WriteLine("stack full");
        }

        public static void Push(int value, ref IntStack ints)
        {
            ints.values[ints.top] = value;
            if (ints.top < ints.values.Length) ints.top++;
            else Interop.WriteLine("stack full");
        }

        public int Pop()
        {
            if (top > 0) top--;
            else Interop.WriteLine("stack empty");
            return values[top];
        }
    }
}
