using Bhp.Ledger;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bhp.Mining
{
    public class MiningSubsidy
    {
        public const long COIN = 100000000;
        private const long CatchUpWithBTC = 3315200;
        private const long SubsidyHalvingInterval = 8400000;
        private const long offsetHeightOfBHP = SubsidyHalvingInterval - CatchUpWithBTC;
        private const long FactorOfSubsidy = 100;
        public const long FactorOfCOIN = COIN * FactorOfSubsidy;
        private const int genesisCoin = 469; //4.69
        private const int maxCycleOfHalvings = 60 * 40;
 

        private long GetBlockSubsidy(uint blockIndex)
        {
            int halvings = (int)((blockIndex + offsetHeightOfBHP) / SubsidyHalvingInterval);
            // Force block reward to zero when right shift is undefined.
            if (halvings >= maxCycleOfHalvings)
                return 0;

            long nSubsidy = genesisCoin * COIN;
            // Subsidy is cut in half every 8400000 blocks which will occur approximately
            // every 4 years.
            nSubsidy = nSubsidy >> halvings;
            return nSubsidy;
        }

        public Fixed8 GetMiningSubsidy(uint blockIndex)
        {
            long subsidy = GetBlockSubsidy(blockIndex);
            if (subsidy == 0)
            {
                return Fixed8.Zero;
            }
            else
            {
                Fixed8 fSubsidy = Fixed8.FromDecimal(subsidy);
                fSubsidy = fSubsidy / COIN;
                Fixed8 value = fSubsidy / FactorOfSubsidy;
                return value;
            }
        }
    }
}
