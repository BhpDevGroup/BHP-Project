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
using System.Text;
using System.Threading.Tasks;

namespace TestBhp
{
    public delegate Transaction SignDelegate(Transaction tx);
    public class TestTransaction
    {
        static System.Timers.Timer timer = new System.Timers.Timer();
        static string assetid = "0x64366593be92cd16471d5db9a4996cc19dd29147caf45794cf6ee42ae089b939";
        static void Test(string[] args)
        {
            timer.Interval = 15000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            var system = new BhpSystem(new LevelDBStore(@"D:\BHP\20181018\test-cli\bhp-cli-3\Chain_00F1A2E3"));
            system.StartNode(20555, 20556);

            Console.ReadKey();
            SignDelegate sign = new SignDelegate(SignWithoutWallet);
            var tx = CreateGlobalTransfer(sign);

            system.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });

            Console.WriteLine("The End");
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
            return tx;
        }

        private static Contract GetContract()
        {
            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Signature },
                "21037fc6eada59947f83edb3658cbd5bed24c5430bcbe2b3c5312ad61e62d91f23a3ac".HexToBytes());

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
                string hexPri = "b9f312a2d07c08fd39da0883f9b1451727aa59160200e70e23614c508174c8e4";
                byte[] bytePri = hexPri.HexToBytes();
                KeyPair key = new KeyPair(bytePri);
                byte[] signature = context.Verifiable.Sign(key);
                Contract contract = GetContract();
                fSuccess |= context.AddSignature(contract, key.PublicKey, signature);
            }
            return fSuccess;
        }

        public static Transaction CreateGlobalTransfer(SignDelegate sign)
        {
            string preTxId = "0x7866a1aae60e7e6a2da87a681edf8c265db200ba68f4a96b447f85bb60c3594b";
            UInt256 hash = UInt256.Parse(preTxId);
            Transaction tx1 = Blockchain.Singleton.GetTransaction(hash);

            Fixed8 preOutVal = tx1.Outputs[100].Value;
            Fixed8 currentOutVal = new Fixed8(1 * (long)Math.Pow(10, 8));

            if (preOutVal < currentOutVal)
            {
                Console.WriteLine("insufficient fund");
                return null;
            }

            var inputs = new List<CoinReference>
            {
                new CoinReference()
                {
                    PrevHash = new UInt256(preTxId.Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 100
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>();

            var output1 = new TransactionOutput()
            {
                AssetId = UInt256.Parse(assetid),
                ScriptHash = "AYuApoS1MQvJMQF7J9GiMcCA9du7s6YBwo".ToScriptHash(),
                Value = currentOutVal
            };
            outputs.Add(output1);
            if (preOutVal > currentOutVal)
            {
                var output2 = new TransactionOutput()
                {
                    AssetId = UInt256.Parse(assetid),
                    ScriptHash = "AZi4EzuSSp4kiWCUvLZcWo8daymKf53ez6".ToScriptHash(),
                    Value = preOutVal - currentOutVal
                };
                outputs.Add(output2);
            }

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
