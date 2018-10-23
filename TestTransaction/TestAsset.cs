using Bhp;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestTransaction
{
    /// <summary>    
    /// 
    /// </summary>
    public class TestAsset
    {
        /*
        coinbaseTx.vout[0].nValue = nFees + GetBlockSubsidy(nHeight, chainparams.GetConsensus());
        CAmount GetBlockSubsidy(int nHeight, const Consensus::Params& consensusParams)
        {
            int halvings = nHeight / consensusParams.nSubsidyHalvingInterval;
            // Force block reward to zero when right shift is undefined.
            if (halvings >= 64)
            return 0; 
            
            CAmount nSubsidy = 50 * COIN;
            // Subsidy is cut in half every 210,000 blocks which will occur approximately every 4 years.
               nSubsidy >>= halvings;
            return nSubsidy;
        }*/

        private static DateTime btcCreationTime => DateTime.UtcNow;

        public static void Test()
        {
            Console.WriteLine(btcCreationTime);
            Console.WriteLine(btcCreationTime);
        }

        public static Fixed8 GetBlockSubsidy(uint blockIndex)
        {            
            uint halvings = blockIndex / ChainParams.SubsidyHalvingInterval;
            // Force block reward to zero when right shift is undefined.
            if (halvings >= 64)
                return Fixed8.Zero;

            Fixed8 subsidy = Fixed8.FromDecimal(50/40);
            //subsidy >>= halvings;
            return subsidy;
        }
    }
}
