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

    public class Transactions
    {

    }

    public class BlockChain
    {
        // If it's good enough for Bitcoin it's good enough for us.
        public uint MagicNumber => 0xD9B4BEF9;
        public uint Version => 420;
        public Nonced<BlockChain> PreviousBlock { get; }
        public DateTime Timestamp { get; }
        public string Message { get; }
        public int Target { get; }

        public bool IsGenesisBlock()
        {
            return PreviousBlock == null;
        }

        public BlockChain(Nonced<BlockChain> previousBlock, DateTime timestamp, int Target, string Message)
        {
            this.PreviousBlock = previousBlock;
            this.Timestamp = timestamp;
            this.Target = Target;
            this.Message = Message;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            BlockChain genesisBlockTemplate = new BlockChain(null, DateTime.Now, 4, "Howdy World!");

            DateTime d = DateTime.Now;

            Console.WriteLine(d.ToString());

            var minedGenesisBlock = Miner.Mine(genesisBlockTemplate, 4);

            DateTime d2 = DateTime.Now;
            TimeSpan ts = d2.Subtract(d);
            Console.WriteLine(ts.Seconds.ToString());
            Console.WriteLine(minedGenesisBlock.Nonce);


            BlockChain secondBlock = new BlockChain(minedGenesisBlock, DateTime.Now, 5, "Yo dawg we heard you like blockchains so we made you a blockchain.");


            d = DateTime.Now;

            Console.WriteLine(d.ToString());

            var minedSecondBlock = Miner.Mine(secondBlock, 6);

            d2 = DateTime.Now;
            ts = d2.Subtract(d);
            Console.WriteLine(ts.Seconds.ToString());
            Console.WriteLine(minedSecondBlock.Nonce);

            Console.ReadLine();
        }
    }
}
