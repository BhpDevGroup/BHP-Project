﻿using Bhp.IO.Json;
using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UserWallet = Bhp.Wallets.SQLite.UserWallet;


namespace Bhp.Wallets.BRC6
{
    public class BRC6Wallet : Wallet
    { 
        public override event EventHandler<WalletTransactionEventArgs> WalletTransaction;
        private readonly WalletIndexer indexer;
        private readonly string path;
        private string password;
        private string name;
        private Version version;
        public readonly ScryptParameters Scrypt;
        private readonly Dictionary<UInt160, BRC6Account> accounts;
        private readonly JObject extra;
        private readonly Dictionary<UInt256, Transaction> unconfirmed = new Dictionary<UInt256, Transaction>();

        public override string Name => name;
        public override Version Version => version;
        public override uint WalletHeight => indexer.IndexHeight;

        public string Path => path;

        public BRC6Wallet(WalletIndexer indexer, string path, string name = null)
        {
            this.indexer = indexer;
            this.path = path;
            if (File.Exists(path))
            {
                JObject wallet;
                using (StreamReader reader = new StreamReader(path))
                {
                    wallet = JObject.Parse(reader);
                }
                this.name = wallet["name"]?.AsString();
                this.version = Version.Parse(wallet["version"].AsString());
                this.Scrypt = ScryptParameters.FromJson(wallet["scrypt"]);
                this.accounts = ((JArray)wallet["accounts"]).Select(p => BRC6Account.FromJson(p, this)).ToDictionary(p => p.ScriptHash);
                this.extra = wallet["extra"];
                indexer.RegisterAccounts(accounts.Keys);
            }
            else
            {
                this.name = name;
                this.version = Version.Parse("1.0");
                this.Scrypt = ScryptParameters.Default;
                this.accounts = new Dictionary<UInt160, BRC6Account>();
                this.extra = JObject.Null;
            } 
            indexer.WalletTransaction += WalletIndexer_WalletTransaction;
        }

        private void AddAccount(BRC6Account account, bool is_import)
        {
            lock (accounts)
            {
                if (accounts.TryGetValue(account.ScriptHash, out BRC6Account account_old))
                {
                    account.Label = account_old.Label;
                    account.IsDefault = account_old.IsDefault;
                    account.Lock = account_old.Lock;
                    if (account.Contract == null)
                    {
                        account.Contract = account_old.Contract;
                    }
                    else
                    {
                        BRC6Contract contract_old = (BRC6Contract)account_old.Contract;
                        if (contract_old != null)
                        {
                            BRC6Contract contract = (BRC6Contract)account.Contract;
                            contract.ParameterNames = contract_old.ParameterNames;
                            contract.Deployed = contract_old.Deployed;
                        }
                    }
                    account.Extra = account_old.Extra;
                }
                else
                {
                    indexer.RegisterAccounts(new[] { account.ScriptHash }, is_import ? 0 : Blockchain.Singleton.Height);
                }
                accounts[account.ScriptHash] = account;
            }
        }

        public override void ApplyTransaction(Transaction tx)
        {
            lock (unconfirmed)
            {
                unconfirmed[tx.Hash] = tx;
            }
            WalletTransaction?.Invoke(this, new WalletTransactionEventArgs    
            {
                Transaction = tx,
                RelatedAccounts = tx.Witnesses.Select(p => p.ScriptHash).Union(tx.Outputs.Select(p => p.ScriptHash)).Where(p => Contains(p)).ToArray(),
                Height = null,
                Time = DateTime.UtcNow.ToTimestamp()
            });
        }

        public override bool Contains(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.ContainsKey(scriptHash);
            }
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            KeyPair key = new KeyPair(privateKey);
            BRC6Contract contract = new BRC6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            BRC6Account account = new BRC6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, false);
            return account;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
        {
            BRC6Contract BRC6contract = contract as BRC6Contract;
            if (BRC6contract == null)
            {
                BRC6contract = new BRC6Contract
                {
                    Script = contract.Script,
                    ParameterList = contract.ParameterList,
                    ParameterNames = contract.ParameterList.Select((p, i) => $"parameter{i}").ToArray(),
                    Deployed = false
                };
            }
            BRC6Account account;
            if (key == null)
                account = new BRC6Account(this, BRC6contract.ScriptHash);
            else
                account = new BRC6Account(this, BRC6contract.ScriptHash, key, password);
            account.Contract = BRC6contract;
            AddAccount(account, false);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            BRC6Account account = new BRC6Account(this, scriptHash);
            AddAccount(account, true);
            return account;
        }

        public KeyPair DecryptKey(string brc2key)
        {
            return new KeyPair(GetPrivateKeyFromBRC2(brc2key, password, Scrypt.N, Scrypt.R, Scrypt.P));
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            bool removed;
            lock (accounts)
            {
                removed = accounts.Remove(scriptHash);
            }
            if (removed)
            {
                indexer.UnregisterAccounts(new[] { scriptHash });
            }
            return removed;
        }

        public override void Dispose()
        { 
            indexer.WalletTransaction -= WalletIndexer_WalletTransaction;
        }

        public override Coin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount, UInt160[] from)
        {
            return FindUnspentCoins(FindUnspentCoins(from).ToArray().Where(p => GetAccount(p.Output.ScriptHash).Contract.Script.IsSignatureContract()), asset_id, amount) ?? base.FindUnspentCoins(asset_id, amount, from);
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            lock (accounts)
            {
                accounts.TryGetValue(scriptHash, out BRC6Account account);
                return account;
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            lock (accounts)
            {
                foreach (BRC6Account account in accounts.Values)
                    yield return account;
            }
        }

        public override IEnumerable<Coin> GetCoins(IEnumerable<UInt160> accounts)
        {
            if (unconfirmed.Count == 0)
                return indexer.GetCoins(accounts);
            else
                return GetCoinsInternal();
            IEnumerable<Coin> GetCoinsInternal()
            {
                HashSet<CoinReference> inputs, claims;
                Coin[] coins_unconfirmed;
                lock (unconfirmed)
                {
                    inputs = new HashSet<CoinReference>(unconfirmed.Values.SelectMany(p => p.Inputs));
                    claims = new HashSet<CoinReference>(unconfirmed.Values.OfType<ClaimTransaction>().SelectMany(p => p.Claims));
                    coins_unconfirmed = unconfirmed.Values.Select(tx => tx.Outputs.Select((o, i) => new Coin
                    {
                        Reference = new CoinReference
                        {
                            PrevHash = tx.Hash,
                            PrevIndex = (ushort)i
                        },
                        Output = o,
                        State = CoinState.Unconfirmed
                    })).SelectMany(p => p).ToArray();
                }
                foreach (Coin coin in indexer.GetCoins(accounts))
                {
                    if (inputs.Contains(coin.Reference))
                    {
                        if (coin.Output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                            yield return new Coin
                            {
                                Reference = coin.Reference,
                                Output = coin.Output,
                                State = coin.State | CoinState.Spent
                            };
                        continue;
                    }
                    else if (claims.Contains(coin.Reference))
                    {
                        continue;
                    }
                    yield return coin;
                }
                HashSet<UInt160> accounts_set = new HashSet<UInt160>(accounts);
                foreach (Coin coin in coins_unconfirmed)
                {
                    if (accounts_set.Contains(coin.Output.ScriptHash))
                        yield return coin;
                }
            }
        }

        public override IEnumerable<UInt256> GetTransactions()
        {
            foreach (UInt256 hash in indexer.GetTransactions(accounts.Keys))
                yield return hash;
            lock (unconfirmed)
            {
                foreach (UInt256 hash in unconfirmed.Keys)
                    yield return hash;
            }
        }

        public override WalletAccount Import(X509Certificate2 cert)
        {
            KeyPair key;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                key = new KeyPair(ecdsa.ExportParameters(true).D);
            }
            BRC6Contract contract = new BRC6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            BRC6Account account = new BRC6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return account;
        }

        public override WalletAccount Import(string wif)
        {
            KeyPair key = new KeyPair(GetPrivateKeyFromWIF(wif));
            BRC6Contract contract = new BRC6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            BRC6Account account = new BRC6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return account;
        }

        public override WalletAccount Import(string brc2, string passphrase)
        {
            KeyPair key = new KeyPair(GetPrivateKeyFromBRC2(brc2, passphrase));
            BRC6Contract contract = new BRC6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            BRC6Account account;
            if (Scrypt.N == 16384 && Scrypt.R == 8 && Scrypt.P == 8)
                account = new BRC6Account(this, contract.ScriptHash, brc2);
            else
                account = new BRC6Account(this, contract.ScriptHash, key, passphrase);
            account.Contract = contract;
            AddAccount(account, true);
            return account;
        }

        internal void Lock()
        {
            password = null;
        }

        public static BRC6Wallet Migrate(WalletIndexer indexer, string path, string db3path, string password)
        {
            using (UserWallet wallet_old = UserWallet.Open(indexer, db3path, password))
            {
                BRC6Wallet wallet_new = new BRC6Wallet(indexer, path, wallet_old.Name);
                using (wallet_new.Unlock(password))
                {
                    foreach (WalletAccount account in wallet_old.GetAccounts())
                    {
                        wallet_new.CreateAccount(account.Contract, account.GetKey());
                    }
                }
                return wallet_new;
            }
        }

        public void Save()
        {
            JObject wallet = new JObject();
            wallet["name"] = name;
            wallet["version"] = version.ToString();
            wallet["scrypt"] = Scrypt.ToJson();
            wallet["accounts"] = new JArray(accounts.Values.Select(p => p.ToJson()));
            wallet["extra"] = extra;
            File.WriteAllText(path, wallet.ToString());
        }
        
        public IDisposable Unlock(string password)
        {
            if (!VerifyPassword(password))
                throw new CryptographicException();
            this.password = password;
            return new WalletLocker(this);
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="Oldpassword">老密码</param>
        /// <param name="Newpassword">新密码</param>
        /// <param name="wallet">修改密码的钱包</param>
        /// <returns></returns>
        public IDisposable ChangeBrc6WalletPassword(string Oldpassword, string Newpassword,BRC6Wallet wallet)
        {
            if (!VerifyPassword(Oldpassword))
                throw new CryptographicException();
            
            for (int i = 0; i < wallet.GetAccounts().ToArray().Length; i++)
            {
                byte[] privatekey_1 = wallet.GetAccount(wallet.GetAccounts().ToArray()[i].Address.ToScriptHash()).GetKey().PrivateKey.ToHexString().HexToBytes();
                KeyPair key = new KeyPair(privatekey_1);
                BRC6Account account = new BRC6Account(this, wallet.GetAccounts().ToArray()[i].Contract.ScriptHash, key, Newpassword)
                {
                    Contract = wallet.GetAccounts().ToArray()[i].Contract
                };
                accounts[wallet.GetAccounts().ToArray()[i].Address.ToScriptHash()] = account;
            }
            this.password = Newpassword;
            return new WalletLocker(this);
        }
        

        public override bool VerifyPassword(string password)
        {
            lock (accounts)
            {
                BRC6Account account = accounts.Values.FirstOrDefault(p => !p.Decrypted);
                if (account == null)
                {
                    account = accounts.Values.FirstOrDefault(p => p.HasKey);
                }
                if (account == null) return true;
                if (account.Decrypted)
                {
                    return account.VerifyPassword(password);
                }
                else
                {
                    try
                    {
                        account.GetKey(password);
                        return true;
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                }
            }
        }

        private void WalletIndexer_WalletTransaction(object sender, WalletTransactionEventArgs e)
        {
            lock (unconfirmed)
            {
                unconfirmed.Remove(e.Transaction.Hash);
            }
            UInt160[] relatedAccounts;
            lock (accounts)
            {
                relatedAccounts = e.RelatedAccounts.Where(p => accounts.ContainsKey(p)).ToArray();
            }
            if (relatedAccounts.Length > 0)
            {
                WalletTransaction?.Invoke(this, new WalletTransactionEventArgs               
                {
                    Transaction = e.Transaction,
                    RelatedAccounts = relatedAccounts,
                    Height = e.Height,
                    Time = e.Time
                });
            }
        }
    }
}
