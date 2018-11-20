using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Bhp.Wallets.BRC6;
using Bhp.Wallets;
using Bhp.Properties;
using Bhp.IO.Json;

namespace Bhp.UI
{
    public partial class FrmCreateWallets : Form
    {
        public FrmCreateWallets()
        {
            InitializeComponent();
        }
        
        string path = string.Empty;
        string password = "1";

        private WalletIndexer indexer = null;
        private WalletIndexer GetIndexer()
        {
            if (indexer is null)
                indexer = new WalletIndexer(Settings.Default.Paths.Index);
            return indexer;
        }

        /// <summary>
        /// choose path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if(dialog.ShowDialog() == DialogResult.OK)
                {
                    this.txt_dir.Text = dialog.SelectedPath;
                    path = dialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// create wallets
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            int num = int.Parse(this.txt_num.Text.Trim());
            if (num <= 0) return;
            if (String.IsNullOrEmpty(path)) return;
            listBox1.Items.Clear();            
            Task task = new Task(() =>
            {
                CreateWallets(num);
            });
            task.Start();
        }

        /// <summary>
        /// create wallets
        /// </summary>
        /// <param name="num"></param>
        private void CreateWallets(int num)
        {            
            List<WalletSnapshot> wss = new List<WalletSnapshot>();            
            for (int i = 0; i < num; i++)
            {
                string walletName = $"wallet{i}.json";
                string spath = Path.Combine(path, walletName);
                if (File.Exists(spath))
                    continue;
                BRC6Wallet wallet = new BRC6Wallet(GetIndexer(), spath);
                wallet.Unlock(password);
                wallet.CreateAccount();
                wallet.Save();
                WalletSnapshot ws = new WalletSnapshot();
                ws.walletName = walletName;
                ws.address = wallet.GetAccounts().ToArray()[0].Address;
                ws.priKey = wallet.GetAccount(ws.address.ToScriptHash()).GetKey().PrivateKey.ToHexString();
                ws.pubKey = wallet.GetAccount(ws.address.ToScriptHash()).GetKey().PublicKey.ToString();
                ws.script = wallet.GetAccounts().ToArray()[0].Contract.ScriptHash.ToString();
                wss.Add(ws);
                this.Invoke(new Action(() =>
                {
                    listBox1.Items.Add($"{i} : {spath}");
                }));
            }            
            JObject json = new JObject();
            json = new JArray(wss.Select(p =>
            {
                JObject jobj = new JObject();
                jobj["walletName"] = p.walletName;
                jobj["address"] = p.address;
                jobj["privKye"] = p.priKey;
                jobj["pubKye"] = p.pubKey;
                jobj["script"] = p.script;
                return jobj;
            }));
            File.WriteAllLines(Path.Combine(path, "walletsnapshot.txt"), new string[] { json.ToString()});            
        }

        private void FrmCreateWallets_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(indexer != null)
            {
                indexer.Dispose();
            }
        }
    }//end of class
}
