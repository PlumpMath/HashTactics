using System;
using System.Collections.Generic;
using System.Linq;
using Ipfs;
using NUnit.Framework;

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

    public class BlockChainValidator
    {
        public BlockChainValidator()
        {
            CurrentVersion = 420;
            // Init storage for parsing this blockchain 
        }

        public int CurrentVersion { get; }


        public List<string> GetLedger(BlockChain bc)
        {
            List<string> ledger = new List<string>();

            BlockChain current = bc;

            while (current != null)
            {
                ledger.Add(current.Message);
                current = current.PreviousBlock?.Value;
            }

            return ledger;
        }

        public bool ValidateLedger(List<string> ledger)
        {
            if (Enumerable.Any(ledger.Where(x => x == null)))
            {
                return false;
            }

            return true;
        }

        public bool Validate(Nonced<BlockChain> chain)
        {
            DagNode chainNode = IpfsDagSerialization.MapToDag(chain);
            var chainNodeHash = Base58.Decode(chainNode.Hash);

            if (Miner.FoundGoldenNonce(chainNodeHash, chain.Value.Target))
            {
                return Validate(chain.Value);
            }
            return false;
        }

        public bool Validate(BlockChain chain)
        {
            if (chain.Version != CurrentVersion)
            {
                return false;
            }

            if (chain.IsGenesisBlock())
            {
                return ValidateLedger(GetLedger(chain));
            }
            else
            {
                // validate previous block and its targeted hash 
                bool previousNodeValid = Validate(chain.PreviousBlock);

                if (!previousNodeValid)
                {
                    return false;
                }

                bool transactionsValid = ValidateLedger(GetLedger(chain));
                return transactionsValid;
            }
        }
    }

    [TestFixture]
    public class TestBlockchainPrototype
    {
        [Test]
        public void TestBasicBlockchainFlow()
        {
            BlockChain genesisBlockTemplate = new BlockChain(null, DateTime.Now, 4, "Howdy World!");

            Assert.True(new BlockChainValidator().Validate(genesisBlockTemplate));
            var minedGenesisBlock = Miner.Mine(genesisBlockTemplate, 4);

            Assert.True(new BlockChainValidator().Validate(minedGenesisBlock.Value));

            BlockChain secondBlock = new BlockChain(minedGenesisBlock, DateTime.Now, 5, "Yo dawg we heard you like blockchains so we made you a blockchain.");

            Assert.True(new BlockChainValidator().Validate(secondBlock));
        }

        [Test]
        public void TestBlockchainInvalidViaWrongHash()
        {
            BlockChain genesisBlockTemplate = new BlockChain(null, DateTime.Now, 4, "Howdy World!");

            Assert.True(new BlockChainValidator().Validate(genesisBlockTemplate));
            var invalidGenesisBlock = new Nonced<BlockChain>(genesisBlockTemplate, 1337);
            Assert.False(new BlockChainValidator().Validate(invalidGenesisBlock));
        }

        [Test]
        public void TestBlockchainInvalidViaWrongLedger()
        {
            BlockChain genesisBlockTemplate = new BlockChain(null, DateTime.Now, 4, "Howdy World!");
            var minedGenesisBlock = Miner.Mine(genesisBlockTemplate, 4);

            BlockChain secondBlock = new BlockChain(minedGenesisBlock, DateTime.Now, 5, null);
            Assert.False(new BlockChainValidator().Validate(secondBlock));

            var minedSecondBlock = Miner.Mine(secondBlock, 4);

            Assert.False(new BlockChainValidator().Validate(secondBlock));

            BlockChain thirdBlock = new BlockChain(minedSecondBlock, DateTime.Now, 4, "Valid message");

            Assert.False(new BlockChainValidator().Validate(thirdBlock));
        }
    }
}
