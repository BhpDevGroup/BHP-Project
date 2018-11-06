using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Bhp.IO.Json;

namespace Bhp
{
    public static class tool
    {
        /// <summary>
        /// read walletsnapshot file
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static JArray ReadFile(string fullPath)
        {
            string str = File.ReadAllText(fullPath);
            JArray jarr = (JArray)JObject.Parse(str);
            return jarr;
        }
    }//end of class

    public class WalletSnapshot
    {
        public string walletName { get; set; }
        public string address { get; set; }
        public string priKey { get; set; }
        public string pubKey { get; set; }
        public string script { get; set; }
    }//end of class
}
