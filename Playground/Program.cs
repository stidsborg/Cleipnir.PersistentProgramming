using System;
using System.Collections.Generic;

namespace Playground
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var l = new List<Action>();
            for (var i = 0; i < 10; i++)
            {
                var j = i;
                l.Add(() => Console.WriteLine(j));
            }

            l.ForEach(a => a());
            
            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }
}
