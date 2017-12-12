using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public long GetBlockchainDepth()
        {
            if (IsGenesisBlock())
            {
                return 0;
            }

            return 1 + PreviousBlock.Value.GetBlockchainDepth();
        }

        public IEnumerable<BlockChain> BlocksTowardsGenesis()
        {
            BlockChain current = this;

            do
            {
                yield return current;
                current = current.PreviousBlock?.Value;
            } while (current != null);
        }
    }

    public class BlockChainValidator : IChainValidator<BlockChain, List<string>>
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

            foreach (var block in BlocksTowardsGenesis(bc))
            { 
                ledger.Add(block.Message);
            }

            ledger.Reverse();
            return ledger;
        }

        public IEnumerable<BlockChain> BlocksTowardsGenesis(BlockChain bc)
        {
            if (bc == null)
            {
                return Enumerable.Empty<BlockChain>();
            }

            return bc.BlocksTowardsGenesis();
        }

        public bool ValidateLedger(List<string> ledger)
        {
            if (Enumerable.Any(ledger.Where(x => x == null)))
            {
                return false;
            }

            HashSet<string> vals = new HashSet<string>(ledger);

            return vals.Count == ledger.Count;
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
            
            // validate previous block and its targeted hash 
            bool previousNodeValid = Validate(chain.PreviousBlock);

            if (!previousNodeValid)
            {
                return false;
            }

            bool transactionsValid = ValidateLedger(GetLedger(chain));
            return transactionsValid;
        }

        public long GetBlockchainDepth(BlockChain bc)
        {
            return bc.GetBlockchainDepth();
        }
    }

    public interface IChainValidator<BlockchainType, LedgerType>
    {
        bool Validate(BlockchainType chain);
        bool ValidateLedger(LedgerType ledger);
    }

    public interface IBlockChainProtocol<LedgerType, TransactionType>
    {
        
            
    }

    public class BlockChainProtocol: IBlockChainProtocol<BlockChain, string>
    {

        private IChainValidator<BlockChain, List<string>> validator; 

        private string currentTransaction;
        private Nonced<BlockChain> currentBlock;

        private List<Nonced<BlockChain>> competingBlocks = new List<Nonced<BlockChain>>();

        private void addCompetingBlock(Nonced<BlockChain> block)
        {
            var valid = new BlockChainValidator().Validate(block);
            if (valid)
            {
                if (currentBlock == null)
                {
                    currentBlock = block;
                }

                // descended from block 
                // Below the tree
                if (block.Value.GetBlockchainDepth() > currentBlock.Value.GetBlockchainDepth())
                {
                    
                }
            }
                
        }

        public Nonced<BlockChain> CurrentBlock => GetLongestBlock();

        private Nonced<BlockChain> GetLongestBlock()
        {
            return competingBlocks.OrderByDescending(x => x.Value.GetBlockchainDepth()).FirstOrDefault();
        }

        private void CullCandidateBlocks()
        {
            long maxDepth = GetLongestBlock().Value.GetBlockchainDepth();
            competingBlocks = competingBlocks.Where(x => x.Value.GetBlockchainDepth() > maxDepth - 2).ToList();

            OnReceivedTransaction += (sender, args) => RecieveTransaction(sender, args);
        }

        private HashSet<string> rejectedTransactions;
        private CancellationToken fullStopCancellationToken;
        private CancellationToken miningCancellationToken;

        public event BlockRecievedHandler OnRecievedBlock;

        public event BlockFoundHandler OnFoundBlock; 

        public event TransactionReceivedHandler OnReceivedTransaction;

        public Nonced<BlockChain> BegForCurrentBlock()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            var token = source.Token;
            source.CancelAfter(400);
            // Call out to the network and wait for a value. 

            var blockChainAlms = BegForCurrentBlock(token);

            if (blockChainAlms != null)
            {
                competingBlocks.Add(blockChainAlms);
            }

            return GetLongestBlock();
        }

        public Nonced<BlockChain> BegForCurrentBlock(CancellationToken ct)
        {
            return null;
        }

        public void RecieveTransaction(object sender, TransactionRecievedEventArgs e)
        {
            currentTransaction = e.ProposedTransaction;
        }

        public BlockChainProtocol(Nonced<BlockChain> startingBlock, CancellationToken fullStopCancellationToken)
        {
            competingBlocks.Add(startingBlock);
            currentBlock = startingBlock;
            this.fullStopCancellationToken = fullStopCancellationToken;
        }

        // TODO: We want to use a token source to combine CancellationTokens, this is really bad at the moment. 
        private bool ContinueMining(CancellationToken suppliedCancellationToken)
        {
            return !(suppliedCancellationToken.IsCancellationRequested ||
                     fullStopCancellationToken.IsCancellationRequested);
        }

        // TODO: Obviously for a real chain this will be different. 
        public int GetCurrentDifficulty()
        {
            return 4;
        }

        public void StartMining(CancellationToken allMiningCancellationToken)
        {
            while (ContinueMining(allMiningCancellationToken))
            {
                CullCandidateBlocks();

                // We need to wait for a transaction before we start mining. 
                if (currentTransaction == null)
                {
                    allMiningCancellationToken.WaitHandle.WaitOne(50);
                    fullStopCancellationToken.WaitHandle.WaitOne(50);
                    continue;
                }

                if (currentBlock == null)
                {
                    currentBlock = BegForCurrentBlock();
                    if (currentBlock == null)
                    {
                        // If cancelled or didn't find a block
                        break;
                    }
                    continue;
                }

                currentBlock = GetLongestBlock();

                if (currentBlock == null)
                {
                    continue;
                }

                BlockChain candidateChain = new BlockChain(currentBlock, DateTime.Now, GetCurrentDifficulty(), currentTransaction);
                var minedResult = Miner.Mine(candidateChain, candidateChain.Target, allMiningCancellationToken);

                if (minedResult != null)
                {
                    competingBlocks.Add(minedResult);
                    OnFoundBlock?.Invoke(this, new BlockFoundEventArgs(minedResult));
                }
            }
        }
    }

    public delegate void BlockRecievedHandler(object sender, BlockRecievedEventArgs e);

    public delegate void BlockFoundHandler(object sender, BlockFoundEventArgs e);

    public delegate void TransactionReceivedHandler(object sender, TransactionRecievedEventArgs args);

    public class TransactionRecievedEventArgs
    {
        public string ProposedTransaction { get; set; }

        public TransactionRecievedEventArgs(string transaction)
        {
            ProposedTransaction = transaction;
        }

        public string TransactionHash => IpfsDagSerialization.MapToDag(this.ProposedTransaction).Hash;
    }

    public class BlockFoundEventArgs
    {
        public BlockFoundEventArgs(Nonced<BlockChain> minedResult)
        {
            FoundBlock = minedResult;
        }

        public Nonced<BlockChain> FoundBlock { get; }
    }

    public class BlockRecievedEventArgs
    {
    }


    [TestFixture]
    public class TestBlockchainPrototype
    {
        [Test]
        public void TestBasicBlockchainFlow()
        {
            BlockChain genesisBlockTemplate = new BlockChain(null, DateTime.Now, 4, "Howdy World!");

            Assert.True(new BlockChainValidator().Validate(genesisBlockTemplate));
            var minedGenesisBlock = Miner.Mine(genesisBlockTemplate, 4, CancellationToken.None);

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
            var minedGenesisBlock = Miner.Mine(genesisBlockTemplate, 4, CancellationToken.None);

            BlockChain secondBlock = new BlockChain(minedGenesisBlock, DateTime.Now, 5, null);
            Assert.False(new BlockChainValidator().Validate(secondBlock));

            var minedSecondBlock = Miner.Mine(secondBlock, 4, CancellationToken.None);

            Assert.False(new BlockChainValidator().Validate(secondBlock));

            BlockChain thirdBlock = new BlockChain(minedSecondBlock, DateTime.Now, 4, "Valid message");

            Assert.False(new BlockChainValidator().Validate(thirdBlock));
        }

        [Test]
        public void TestBasicBlockchainProtocol()
        {
            BlockChain genesisBlockTemplate = new BlockChain(null, DateTime.Now, 4, "Howdy World!");
            var minedGenesisBlock = Miner.Mine(genesisBlockTemplate, 4, CancellationToken.None);

            List<string> transactions = new List<string>(){"How are you doing?", "I am doing fine.", "Isn't this cool?"};

            CancellationTokenSource source = new CancellationTokenSource(60000);
            BlockChainProtocol protocol = new BlockChainProtocol(minedGenesisBlock, source.Token);
            

            CancellationTokenSource cancelMining = new CancellationTokenSource(60000);
            protocol.OnFoundBlock += (o,v)=>cancelMining.Cancel();

            foreach (var transaction in transactions)
            {
                cancelMining = new CancellationTokenSource(60000);
                protocol.RecieveTransaction(this, new TransactionRecievedEventArgs(transaction));
                protocol.StartMining(cancelMining.Token);
            }

            var ledger = new BlockChainValidator().GetLedger(protocol.CurrentBlock.Value);
            var expected = new []{ "Howdy World!","How are you doing?", "I am doing fine.", "Isn't this cool?"};

            for (int i = 0; i < ledger.Count; i++)
            {
                Assert.AreEqual(expected[i], ledger[i]);                
            }

            Assert.True(new BlockChainValidator().Validate(protocol.CurrentBlock.Value));
        }
    }
}
