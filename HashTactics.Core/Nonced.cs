using System;
using System.Collections.Generic;
using System.Text;

namespace HashTactics.Core
{
    public class Nonced<InnerType>
    {
        public InnerType Value { get; }
        public long Nonce { get; }

        public Nonced(InnerType vaue, long ourNonce)
        {
            Value = vaue;
            Nonce = ourNonce;
        }
    }


}
