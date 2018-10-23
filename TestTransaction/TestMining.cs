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
using Bhp.Cryptography;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
        static string dbPath = @"D:\Projects\BHP-Project\TestTransaction\bin\Debug\netcoreapp2.1\db";

        static string currentScript = "2102cfce51880b622d41a28ff04ed06c0e0ba9cc8a33627b948af436fe01f4943f7bac";
        static string currentHexPri = "c72915340453c40a3c47f3522785b49d2d61f9dfdd53f0376ed2f524bd462716";
        static string currentAddress = "AQ7WcGjsdJoF95PvPy6RHZj9P9Wz9TZifk";
        
        static string preTxId = "0xb57c7b968ef80396cb611f225504a50ae606645155c9870943d828247e325ef3";
        static string tansferAddress = "AQ7WcGjsdJoF95PvPy6RHZj9P9Wz9TZifk";
        //Ad44qaLgbADLbiHTjVBTntkfCNXQfKm6aw        0xa5fd834425bc0c7ab8e7ce7d9c5bac1e5e38b39f631280f9a7bcfeac64b2ddf4 1
        //AYuyQLezvjZTpKPLEtQTLLdPGJQh3g8ZP6        0xa5fd834425bc0c7ab8e7ce7d9c5bac1e5e38b39f631280f9a7bcfeac64b2ddf4 0
        static Fixed8 tansferValue = Fixed8.FromDecimal(10);
        static ushort prevIndex = 0;

        public static void Test(string[] args)
        {
            timer.Interval = 5000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            var system = new BhpSystem(new LevelDBStore(dbPath));
            system.StartNode(10555, 10556);

            Console.WriteLine("block height : " + Blockchain.Singleton.Height);
            Console.WriteLine("Please input any key...");
            Console.ReadKey();

            //SignDelegate2 sign = new SignDelegate2(SignWithoutWallet);
            SignDelegate2 sign = new SignDelegate2(SignWithoutWallet2);

            //var tx = CreateGlobalTransfer(sign);
            var tx = CreateMinnerTransaction(sign);

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
                currentScript.HexToBytes());

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
                byte[] bytePri = currentHexPri.HexToBytes();
                KeyPair key = new KeyPair(bytePri);
                byte[] signature = context.Verifiable.Sign(key);
                Contract contract = GetContract();
                fSuccess |= context.AddSignature(contract, key.PublicKey, signature);
            }
            return fSuccess;
        }

        private static Transaction SignWithoutWallet2(Transaction tx)
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

            Sign2(tx);                                   
            Console.WriteLine("tx.ToJson() : " + tx.ToJson());
            Console.WriteLine("tx.Hash : " + tx.Hash.ToString());
            return tx;
        }

        private static void Sign2(Transaction tx)
        {            
            byte[] bytePri = currentHexPri.HexToBytes();
            KeyPair key = new KeyPair(bytePri);            
            byte[] hashDataOfMining = null;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(tx.Outputs[0].AssetId);
                writer.Write(tx.Outputs[0].Value);
                writer.Write(tx.Outputs[0].ScriptHash);
                writer.Flush();
                hashDataOfMining = ms.ToArray();
            }
            byte[] signatureOfMining = Crypto.Default.Sign(hashDataOfMining, key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray());
            tx.Attributes = new TransactionAttribute[]
                            {
                                new TransactionAttribute
                                {
                                    Usage = TransactionAttributeUsage.Description,
                                    Data = signatureOfMining
                                }
                            };
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
                    ScriptHash = currentAddress.ToScriptHash()
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
            CoinReference[] inputs = new CoinReference[]
            {
                new CoinReference()
                {
                    PrevHash = new UInt256(preTxId.Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = prevIndex //vout n
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>();
            var output1 = new TransactionOutput()
            {
                AssetId = UInt256.Parse(assetid),
                ScriptHash = tansferAddress.ToScriptHash(),
                Value = tansferValue
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
