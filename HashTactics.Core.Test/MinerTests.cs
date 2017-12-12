using Ipfs;
using NUnit.Framework;

namespace HashTactics.Core.Test
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


    [TestFixture]
    public class MinerTests
    {
        [Test]
        public void TestMinerDifficulty()
        {
            Assert.AreEqual(0b11111111, Miner.ThresholdValue(0));
            Assert.AreEqual(0b01111111, Miner.ThresholdValue(1));
            Assert.AreEqual(0b00111111, Miner.ThresholdValue(2));
            Assert.AreEqual(0b00011111, Miner.ThresholdValue(3));
            Assert.AreEqual(0b00001111, Miner.ThresholdValue(4));
            Assert.AreEqual(0b00000111, Miner.ThresholdValue(5));
            Assert.AreEqual(0b00000011, Miner.ThresholdValue(6));
            Assert.AreEqual(0b00000001, Miner.ThresholdValue(7));
            Assert.AreEqual(0b00000000, Miner.ThresholdValue(8));
            Assert.AreEqual(0b00000000, Miner.ThresholdValue(9));
            Assert.AreEqual(0b00000000, Miner.ThresholdValue(33));
        }

        [Test]
        public void TestFindGoldenNonce()
        {

            byte[] bad_nonce = new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF
            };

            Assert.False(Miner.FoundGoldenNonce(bad_nonce, 8));

            byte[] golden_nonce = new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0b00001011, 0xFF
            };

            Assert.True(Miner.FoundGoldenNonce(golden_nonce, 4));

            byte[] golden_nonce_8 = new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0b00000000, 0xFF
            };

            Assert.True(Miner.FoundGoldenNonce(golden_nonce_8, 8));


            byte[] golden_nonce_17 = new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0x00, 0x00, 0b01101111, 0xFF
            };

            Assert.True(Miner.FoundGoldenNonce(golden_nonce_17, 17));

        }

        [Test]
        public void TestCanMineForBasicNoncedValue()
        {
            var snoop = new Rapper(420, "snoop dogg");

            var mined_snoop = Miner.Mine(snoop, 3);

            Assert.AreEqual(mined_snoop.Value.Name, "snoop dogg");
            Assert.AreEqual(mined_snoop.Value.Level, 420);
            Assert.AreEqual(mined_snoop.Nonce, 1347);

            var mined_dag = IpfsDagSerialization.MapToDag(mined_snoop);
            Assert.True(Miner.FoundGoldenNonce(Base58.Decode(mined_dag.Hash), 3));
        }
    }
}