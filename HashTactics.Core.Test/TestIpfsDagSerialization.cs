using System;
using System.Collections.Generic;
using System.Linq;
using Ipfs;
using NUnit.Framework;
using Org.BouncyCastle.Asn1.X509;

namespace HashTactics.Core.Test
{
    [TestFixture]
    public class TestIpfsDagSerialization
    {
        [Test]
        public void TestSerializeBlockchainObject()
        {
            BlockChain genesisBlockTemplate = new BlockChain(null, DateTime.Now, 4, "Howdy World!");

            DagNode genesisNode = IpfsDagSerialization.MapToDag<BlockChain>(genesisBlockTemplate);

            var links = genesisNode.Links.ToList();
            
            Assert.AreEqual(6, links.Count);
            Assert.True(genesisNode.Size > 10);

        }
    }
}