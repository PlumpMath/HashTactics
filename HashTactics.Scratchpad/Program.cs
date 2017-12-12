using System;
using HashTactics.Core;

namespace HashTactics.Scratchpad
{
    public class Rapper
    {
        public Rapper(int v1, string v2)
        {
            Level = v1;
            Name = v2;
        }

        public int Level { get; }
        public string Name { get; }
    }

    class Program
    {
        static void Main(string[] args)
        {

            var snoop = new Rapper(420, "snoop dogg");
            DateTime d = DateTime.Now;

            Console.WriteLine(d.ToString());

            var mined_snoop = Miner.Mine(snoop, 16);
            DateTime d2 = DateTime.Now;
            TimeSpan ts = d2.Subtract(d);
            Console.WriteLine(ts.Seconds.ToString());
            Console.WriteLine(mined_snoop.Nonce);
        }
    }
}
