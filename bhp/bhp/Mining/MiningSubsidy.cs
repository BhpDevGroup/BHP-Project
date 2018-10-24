using Bhp.Ledger;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bhp.Mining
{
    public class MiningSubsidy
    {
        private const ulong COIN = 100000000;
        private const ulong CatchUpWithBTC = 3315200;
        private const ulong SubsidyHalvingInterval = 8400000;
        private const ulong FactorOfSubsidy = 100;
        private const ulong genesisCoin = 469; //4.69

        private ulong GetBlockSubsidy(uint nHeight)
        {
            int halvings = (int)((nHeight + CatchUpWithBTC) / SubsidyHalvingInterval);
            // Force block reward to zero when right shift is undefined.
            if (halvings >= 64 * 40)
                return 0;

            ulong nSubsidy = genesisCoin * COIN;
            // Subsidy is cut in half every 210,000 blocks which will occur approximately
            // every 4 years.
            nSubsidy = nSubsidy >> halvings;
            return nSubsidy;
        }
    }
}
