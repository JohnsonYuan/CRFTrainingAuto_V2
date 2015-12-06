using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread[] threads = new Thread[5];
            for (int i = 0; i < threads.Length; i++)
            {
                ParameterizedThreadStart threadStart = new ParameterizedThreadStart(Process);
                threads[i] = new Thread(new ParameterizedThreadStart(Process));
                threads[i].Start(i);
            }
        }

        public static void Process(object index)
        {
            Console.WriteLine(Convert.ToInt16(index));
        }
    }
}
