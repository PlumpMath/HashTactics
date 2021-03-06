﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Ipfs;

namespace HashTactics.Core
{
    public static class Miner
    {
        private const int starting_index = 6;


        public static byte ThresholdValue(int zerosInFront)
        {
            if (zerosInFront > 7)
            {
                return 0;
            }

            return (byte)(0b11111111 >> (byte)zerosInFront);
        }


        public static bool FoundGoldenNonce(object obj, int zerosInFront)
        {
            return FoundGoldenNonce(Base58.Decode(IpfsDagSerialization.MapToDag(obj).Hash), zerosInFront);
        }

        public static bool FoundGoldenNonce(byte[] hash, int zerosInFront)
        {
            int maxBytes = (int)Math.Ceiling((double)zerosInFront / 8);
            int zerosCounted = 0;

            bool match = true;
            for (int i = 0; i < maxBytes; i++)
            { 
                if (zerosCounted > zerosInFront)
                {
                    break;
                }

                byte currentThresholdByte = ThresholdValue(zerosInFront - zerosCounted);
                byte hashByte = hash[starting_index + i];
                if (hashByte > currentThresholdByte)
                {
                    match = false;
                    break;
                }

                zerosCounted += 8;
            }

            return match;
        }

        public static Nonced<InnerType> Mine<InnerType>(InnerType value, int zerosInFront, CancellationToken cancellationToken)
        {
            // TODO: This is a total no-no for competitive mining. 
            long ourNonce = 1337;

            for (;;) // the cookie monster loop is the most delicious infinite loop 
            {
                if (cancellationToken != null && cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                Nonced<InnerType> attempt = new Nonced<InnerType>(value, ourNonce);
                DagNode attemptedNode = IpfsDagSerialization.MapToDag<Nonced<InnerType>>(attempt);

                bool match = FoundGoldenNonce(Base58.Decode(attemptedNode.Hash), zerosInFront);

                if (match)
                {
                    return attempt;
                }

                ourNonce++;
            }
        }
    }
}
