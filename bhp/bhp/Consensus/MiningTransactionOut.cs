using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.Wallets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bhp.Consensus
{
    /// <summary>
    /// 挖矿交易输出(区块产出)
    /// </summary>
    public class MiningTransactionOut
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="wallet"></param>
        /// <param name="amount_netfee"></param>
        /// <returns></returns>
        public TransactionOutput[] MiningTransactionOuts(uint blockIndex,Wallet wallet,Fixed8 amount_netfee)
        {
            TransactionOutput[] outputs = null;
            if (amount_netfee == Fixed8.Zero)
            {
                //网络费为空，只构造一笔挖矿交易
                outputs = new TransactionOutput[1] { MiningOutput(blockIndex, wallet) };
            }
            else
            {
                //构造一笔挖矿交易和网络费用交易
                outputs = new TransactionOutput[2] { MiningOutput(blockIndex, wallet), NetFeeOutput(wallet,amount_netfee) };
            }
            return outputs;
        }

        /// <summary>
        /// 锚定比特币产出规则,区块产出
        /// Blockchain.AmountOfMiningGoverningToken
        /// </summary>
        /// <returns></returns>
        private Fixed8 MiningAmountOfCurrentBlock(uint blockIndex)
        {
            return Fixed8.FromDecimal((blockIndex % 10) == 0 ? 10 : 5);
        }

        /// <summary>
        /// 挖矿交易
        /// </summary>
        /// <returns></returns>
        private TransactionOutput MiningOutput(uint blockIndex, Wallet wallet)
        {
            return new TransactionOutput
            {
                AssetId = Blockchain.GoverningToken.Hash,
                Value = MiningAmountOfCurrentBlock(blockIndex),
                ScriptHash = wallet.GetChangeAddress()
            };
        }

        private TransactionOutput NetFeeOutput(Wallet wallet,Fixed8 amount_netfee)
        {
            return new TransactionOutput
            {
                AssetId = Blockchain.UtilityToken.Hash,
                Value = amount_netfee,
                ScriptHash = wallet.GetChangeAddress()
            };
        }
    }
}
