using System;

namespace HashTactics.Core.Test
{
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

}
