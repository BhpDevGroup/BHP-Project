using Akka.Actor;
using Bhp.Wallets;
using Bhp.Wallets.BRC6;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using Bhp.Network.P2P;
using Bhp.Network.P2P.Payloads;
using Bhp.SmartContract;
using System.Threading.Tasks;
using Bhp.Ledger;
using Bhp.IO.Json;

namespace Bhp.UI
{
    public partial class FrmTransfer : Form
    {
        public FrmTransfer()
        {
            InitializeComponent();
        }

        private List<BRC6Wallet> fromWallets = null;
        private List<string> toAddressList = null;
        StreamWriter sw = null;
        System.Timers.Timer timer = null;
        UIntBase assetId = UIntBase.Parse("0x13f76fabfe19f3ec7fd54d63179a156bafc44afc53a7f07a7a15f6724c0aa854");

        private void FrmTransfer_Shown(object sender, EventArgs e)
        {
            fromWallets = new List<BRC6Wallet>();
            toAddressList = new List<string>();
            timer = new System.Timers.Timer();
            timer.Interval = 60000;
            timer.Elapsed += Timer_Elapsed;
            CreateLogFile();
        }

        bool isTransfer = false;
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isTransfer) return;
            try
            {
                isTransfer = true;
                Transfer();
            }
            catch
            {
            }
            finally
            {
                isTransfer = false;
            }
        }

        /// <summary>
        /// choose from wallet path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                if(dialog.ShowDialog() == DialogResult.OK)
                {
                    string dir = dialog.SelectedPath;
                    this.textBox1.Text = dir;
                    OpenWallets(dir);
                }
            }
        }

        /// <summary>
        /// open all wallets under the specified path
        /// </summary>
        /// <param name="path">file directory</param>
        private void OpenWallets(string path)
        {
            clearWallets();            
            DirectoryInfo dInfo = new DirectoryInfo(path);
            FileInfo[] fInfo = dInfo.GetFiles("wallet*.json");
            if (fInfo.Length == 0)
                return;
            Task task = new Task(() => OpenWallet(fInfo));
            task.Start();
        }

        /// <summary>
        /// open wallet task
        /// </summary>
        /// <param name="fInfo">files</param>
        private void OpenWallet(FileInfo[] fInfo)
        {
            int len = fInfo.Length;
            for (int i = 0; i < len; i++)
            {
                try
                {                    
                    BRC6Wallet wallet = new BRC6Wallet(new WalletIndexer(fInfo[i].Name), fInfo[i].FullName);                    
                    wallet.Unlock(txt_password.Text.Trim());
                    fromWallets.Add(wallet);
                    UInt160 script_hash = wallet.GetAccounts().ToArray()[0].Address.ToScriptHash();                   
                    AccountState account = Blockchain.Singleton.Store.GetAccounts().TryGet(script_hash) ?? new AccountState(script_hash);
                    string value = "0";
                    if (account.Balances.Count > 0)
                    {
                        value = account.Balances.Values.ToArray()[0].ToString();
                    }

                    this.Invoke(new Action(() =>
                    {
                        listBox1.Items.Add($"{i + 1}:{fInfo[i].FullName}--{value}");
                    }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }
        }

        /// <summary>
        /// clear wallets and listbox1
        /// </summary>
        private void clearWallets()
        {
            foreach (BRC6Wallet wallet in fromWallets)
            {
                wallet.Dispose();
            }
            fromWallets.Clear();
            listBox1.Items.Clear();            
        }

        /// <summary>
        /// choose to address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_to_Click(object sender, EventArgs e)
        {
            toAddressList.Clear();
            listBox2.Items.Clear();
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string path = dialog.FileName;
                    this.textBox2.Text = path;
                    JArray toArr = tool.ReadFile(dialog.FileName);
                    int i = 1;
                    foreach (JObject toA in toArr)
                    {
                        toAddressList.Add(toA["address"].AsString());
                        listBox2.Items.Add($"{i++}:{toA["address"].AsString()}");
                    }
                }
            }
        }

        /// <summary>
        /// transfer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            Transfer();
        }

        /// <summary>
        /// transfer
        /// </summary>
        private void Transfer()
        {
            WriteLog("----------transfer start----------");
            toolStripProgressBar1.Value = 0;
            if (fromWallets.Count == 0 || toAddressList.Count == 0)
            {
                return;
            }
            int len = fromWallets.Count;
            if (fromWallets.Count > toAddressList.Count)
            {
                len = toAddressList.Count;
            }            
            Random rd = new Random();
            double step = (double)100 / len;
            for (int i = 0; i < len; i++)
            {                
                AssetDescriptor descriptor = new AssetDescriptor(assetId);
                string fromStr = fromWallets[i].GetAccounts().ToArray()[0].Address;
                UInt160 from = fromStr.ToScriptHash();
                UInt160 to = toAddressList[i].ToScriptHash();
                double val = (double)rd.Next(1000) / 1000;
                BigDecimal value = BigDecimal.Parse(val.ToString(), descriptor.Decimals);
                if (value.Sign <= 0)
                {
                    WriteLog($"from:{fromStr}  value.Sign <= 0");
                    setProgressValue(len, i, step);
                    continue;
                }
                Fixed8 fee = Fixed8.Zero;
                Transaction tx = fromWallets[i].MakeTransaction(null, new[]
                {
                    new TransferOutput
                    {
                        AssetId = assetId,
                        Value = value,
                        ScriptHash = to
                    }
                }, from, from, fee);
                if (tx == null)
                {
                    WriteLog($"from:{fromStr}  tx == null");
                    setProgressValue(len, i, step);
                    continue;
                }
                ContractParametersContext context = new ContractParametersContext(tx);
                fromWallets[i].Sign(context);
                if (context.Completed)
                {
                    tx.Witnesses = context.GetWitnesses();
                    fromWallets[i].ApplyTransaction(tx);
                    Program.BhpSystem.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
                }
                setProgressValue(len, i, step);
                WriteLog($"success {i} from:{fromStr} account:{val} trans_id:{tx.Hash}");
                System.Threading.Thread.Sleep(50);
            }
            toolStripProgressBar1.Value = 100;        
            WriteLog("----------transfer end----------");
        }

        /// <summary>
        /// change bar value
        /// </summary>
        /// <param name="len">array length</param>
        /// <param name="i">current index</param>
        /// <param name="step">bar step</param>
        private void setProgressValue(int len, int i, double step)
        {
            if (i == len - 1)
            {
                this.Invoke(new Action(() => {
                    toolStripProgressBar1.Value = 100;
                }));                
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    toolStripProgressBar1.Value = (int)(i * step);
                }));                
            }
        }

        /// <summary>
        /// create log file
        /// </summary>
        private void CreateLogFile()
        {
            string logpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            if (!Directory.Exists(logpath))
            {
                Directory.CreateDirectory(logpath);
            }
            logpath = Path.Combine(logpath, "log.txt");
            sw = new StreamWriter(logpath, true);
        }

        /// <summary>
        /// write log
        /// </summary>
        /// <param name="str">werite message</param>
        private void WriteLog(string str)
        {
            string strline = $"{DateTime.Now.ToString("yyyy-MM-dd:HH:mm:ss.fff")}:{str}";
            sw.WriteLine(strline);
            sw.Flush();
        }

        private void FrmTransfer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(sw != null)
            {
                sw.Flush();
                sw.Close();
                sw = null;
            }
            if(fromWallets != null)
            {                
                fromWallets = null;
            }
            if(toAddressList != null)
            {
                toAddressList = null;
            }
            if(timer != null)
            {
                timer.Elapsed -= Timer_Elapsed;
                timer.Dispose();
            }
        }

        bool isStart = false;
        private void btn_timer_Click(object sender, EventArgs e)
        {
            if(!isStart)
            {
                WriteLog("----------start----------");
                isStart = true;
                btn_timer.Text = "timer stop";
                timer.Start();                
            }
            else
            {
                WriteLog("----------end----------");
                isStart = false;
                btn_timer.Text = "timer start";
                timer.Stop();
                isTransfer = false;
            }
        }

        private void btn_one_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string name = dialog.FileName.Substring(dialog.FileName.LastIndexOf("\\") + 1);
                        BRC6Wallet wallet = new BRC6Wallet(new WalletIndexer(name), dialog.FileName);
                        wallet.Unlock(txt_password.Text.Trim());
                        UInt160 script_hash = wallet.GetAccounts().ToArray()[0].Address.ToScriptHash();
                        AccountState account = Blockchain.Singleton.Store.GetAccounts().TryGet(script_hash) ?? new AccountState(script_hash);
                        string value = "0";
                        if (account.Balances.Count > 0)
                        {
                            value = account.Balances.Values.ToArray()[0].ToString();
                        }
                        clearWallets();
                        txt_one.Text = dialog.FileName;
                        listBox1.Items.Add($"{dialog.FileName}--{value}");
                        fromWallets.Add(wallet);
                    }
                }
            }
            catch
            { }
        }

        private void btn_many_Click(object sender, EventArgs e)
        {
            SendMany();
        }

        /// <summary>
        /// send many
        /// </summary>
        private void SendMany()
        {
            WriteLog("----------send many start----------");
            toolStripProgressBar1.Value = 0;
            if (fromWallets.Count == 0 || toAddressList.Count == 0)
            {
                return;
            }                     

            AssetDescriptor descriptor = new AssetDescriptor(assetId);
            string fromStr = fromWallets[0].GetAccounts().ToArray()[0].Address;
            UInt160 from = fromStr.ToScriptHash();            
            double val = double.Parse(txt_value.Text.Trim());
            BigDecimal value = BigDecimal.Parse(val.ToString(), descriptor.Decimals);
            Fixed8 fee = Fixed8.Zero;

            List<TransferOutput> outputs = new List<TransferOutput>();
            for (int i = 0; i < toAddressList.Count; i++)
            {
                UInt160 to = toAddressList[i].ToScriptHash();
                outputs.Add(new TransferOutput
                {
                    AssetId = assetId,
                    Value = value,
                    ScriptHash = to
                });                         
            }

            Transaction tx = fromWallets[0].MakeTransaction(null, outputs, from, from, fee);
            if (tx == null)
            {
                WriteLog($"from:{fromStr}  tx == null");
                return;
            }
            ContractParametersContext context = new ContractParametersContext(tx);
            fromWallets[0].Sign(context);
            if (context.Completed)
            {
                tx.Witnesses = context.GetWitnesses();
                fromWallets[0].ApplyTransaction(tx);
                Program.BhpSystem.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
            }
            toolStripProgressBar1.Value = 100;
            WriteLog("----------send many end----------");
        }
    }//end of class
}
