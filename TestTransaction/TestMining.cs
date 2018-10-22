using Akka.Actor;
using Bhp;
using Bhp.IO;
using Bhp.Ledger;
using Bhp.Network.P2P;
using Bhp.Network.P2P.Payloads;
using Bhp.Persistence.LevelDB;
using Bhp.SmartContract;
using Bhp.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestTransaction
{
    public delegate Transaction SignDelegate2(Transaction tx);
    public class TestMining
    {
        static System.Timers.Timer timer = new System.Timers.Timer();
        static string assetid = "0x64366593be92cd16471d5db9a4996cc19dd29147caf45794cf6ee42ae089b939";
        public static void Test(string[] args)
        {
            timer.Interval = 15000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            var system = new BhpSystem(new LevelDBStore(@"D:\BHP-Project\TestTransaction\bin\Debug\netcoreapp2.1\db"));
            system.StartNode(10555, 10556);

            Console.WriteLine("Please input any key...");
            Console.ReadKey();
            SignDelegate2 sign = new SignDelegate2(SignWithoutWallet);

            var tx =  CreateMinnerTransaction(sign);

            system.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
                        
            Console.WriteLine("Transaction Replay. press any key...");
            timer.Stop();
            timer.Elapsed -= Timer_Elapsed;
            timer.Dispose();
            Console.ReadKey();
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("block height : " + Blockchain.Singleton.Height);
        }

        private static Transaction SignWithoutWallet(Transaction tx)
        {
            if (tx == null)
            {
                throw new ArgumentNullException("tx");
            }
            try
            {
                tx.ToJson();
            }
            catch (Exception ex)
            {
                throw new FormatException("format error:" + ex.ToString());
            }

            var context = new ContractParametersContext(tx);
            Sign(context);
            if (context.Completed)
            {
                Console.WriteLine("sign success");
                tx.Witnesses = context.GetWitnesses();
            }
            else
            {
                Console.WriteLine("sign failed");
            }
            Console.WriteLine("Verify the transaction : " + tx.Verify(Blockchain.Singleton.GetSnapshot(), new List<Transaction> { tx }));
            Console.WriteLine("tx.ToArray().ToHexString() : " + tx.ToArray().ToHexString());
            Console.WriteLine("tx.ToJson() : " + tx.ToJson());
            Console.WriteLine("tx.Hash : " + tx.Hash.ToString());
            return tx;
        }

        private static Contract GetContract()
        {
            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Signature },
                "".HexToBytes());

            /*
            Contract contract = new Contract();
            contract.Script = "21037fc6eada59947f83edb3658cbd5bed24c5430bcbe2b3c5312ad61e62d91f23a3ac".HexToBytes();
            contract.ParameterList = new ContractParameterType[1];
            contract.ParameterList[0] = ContractParameterType.Signature;
            */
            return contract;
        }

        private static bool Sign(ContractParametersContext context)
        {
            bool fSuccess = false;
            foreach (UInt160 scriptHash in context.ScriptHashes)
            {
                string hexPri = "adaf58635974a12cb7c46f639b9b0561bf85886dee3e837aa9dcbf4aeccf11b9";
                byte[] bytePri = hexPri.HexToBytes();
                KeyPair key = new KeyPair(bytePri);
                byte[] signature = context.Verifiable.Sign(key);
                Contract contract = GetContract();
                fSuccess |= context.AddSignature(contract, key.PublicKey, signature);
            }
            return fSuccess;
        }

        private static ulong ToUInt64(byte[] value, int startIndex)
        {
            ulong v = 0;
            for (int i = 0; i < sizeof(ulong); i++)
            {
                ulong u = value[i];
                u = u << (i * 8);
                v += u;
            }
            return v;
        }

        private static ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return ToUInt64(nonce,0);
        }

        public static Transaction CreateMinnerTransaction(SignDelegate2 sign)
        {
            // d55ccd8b368aca2402003f39549ad7b75385c226009b3080e0d886959b75b5ce
            TransactionOutput[] outputs = new TransactionOutput[]
            {
                new TransactionOutput
                {
                    AssetId = Blockchain.GoverningToken.Hash,
                    Value = Fixed8.FromDecimal(444),
                    ScriptHash = "AbUAKqt8crJQnhDJWQtFGHT7Pgv9ABnQE6".ToScriptHash()
                }
            };
            ulong nonce = GetNonce();
            MinerTransaction tx = new MinerTransaction
            {
                Nonce = (uint)(nonce % (uint.MaxValue + 1ul)),
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = outputs,
                Witnesses = new Witness[0]
            }; 

            return sign.Invoke(tx);
        }

        public static Transaction CreateGlobalTransfer(SignDelegate2 sign)
        {
            string preTxId = "0x1db3ae57b5f3fda94aa6a81add07a70fc38f0eae9e26f39d7d51183fad0c849f";
            UInt256 hash = UInt256.Parse(preTxId);
            Transaction tx1 = Blockchain.Singleton.GetTransaction(hash);


            CoinReference[] inputs = new CoinReference[]
            {
                new CoinReference()
                {
                    PrevHash = new UInt256(preTxId.Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //vout n
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>();

            var output1 = new TransactionOutput()
            {
                AssetId = UInt256.Parse(assetid),
                ScriptHash = "AbUAKqt8crJQnhDJWQtFGHT7Pgv9ABnQE6".ToScriptHash(),
                Value = Fixed8.FromDecimal(14)
            };
            outputs.Add(output1); 

            var tx = new ContractTransaction()
            {
                Outputs = outputs.ToArray(),
                Inputs = inputs,
                Attributes = new TransactionAttribute[0],
                Witnesses = new Witness[0]               
            };

            


            return sign.Invoke(tx);
        }
    }
}
