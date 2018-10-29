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
        //common
        static string assetid = "0x13f76fabfe19f3ec7fd54d63179a156bafc44afc53a7f07a7a15f6724c0aa854";
        static string dbPath = @"D:\Projects\BHP-Project\TestTransaction\bin\Debug\netcoreapp2.1\db";
        static string currentScript = "21036c383c530ad4ec9ed3370877ead28223bba812d30208055fb125e57995b06c01ac";
        static string currentHexPri = "d55ccd8b368aca2402003f39549ad7b75385c226009b3080e0d886959b75b5ce";
        //MinnerTransaction
        static string currentAddress = "AFwBJwUrh9RFGp4UE4wBE9GR2zsAGeWJsM";
        //GlobalTransfer
        static string preTxId = "0xf9b9ee754e63dbcf632f87cdc135cc9a2fbc26953a3de0d7c1dacdd78fd97f8c";
        static string tansferAddress = "AHB1muqmPTbeAJ9PPuXV8XfbjfLKtTunjC";        
        static Fixed8 tansferValue = Fixed8.FromDecimal(4.69M);        
        static ushort prevIndex = 0;

        public static void Test()
        {
            timer.Interval = 5000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            var system = new BhpSystem(new LevelDBStore(dbPath));
            system.StartNode(10555, 11556);

            Console.WriteLine("block height : " + Blockchain.Singleton.Height);
            Console.WriteLine("Please input any key...");
            Console.ReadKey();

            SignDelegate2 sign = new SignDelegate2(SignWithoutWallet);
            //SignDelegate2 sign = new SignDelegate2(SignWithoutWallet2);

            var tx = CreateGlobalTransfer(sign);
            //var tx = CreateMinnerTransaction(sign);

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
