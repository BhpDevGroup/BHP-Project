using Bhp;
using Bhp.Mining;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace TestTransaction
{
    /// <summary>    
    /// 
    /// </summary>
    public class TestAsset
    { 

        

        public static void Test()
        {
            DateTime bhpCreationTime = DateTime.UtcNow;
            FileStream fs = new FileStream($"{System.Environment.CurrentDirectory}\\BHP_Output.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine($"Start {DateTime.Now}");
            sw.Flush();

            uint blockIndex = 0;
            MiningSubsidy miningSubsidy = new MiningSubsidy();
            Console.WriteLine(bhpCreationTime);
            string line = "";
            Fixed8 lastSubsidy = Fixed8.Zero;
            Fixed8 totalSubsidy = Fixed8.Zero;
            Fixed8 lastTotal = Fixed8.Zero;
            while (true)
            {
                Fixed8 nSubsidy = miningSubsidy.GetMiningSubsidy(blockIndex);
                if (nSubsidy == Fixed8.Zero)
                {
                    break;
                }

                totalSubsidy = totalSubsidy + nSubsidy;
                lastTotal = lastTotal + nSubsidy;

                line = string.Format("Height: {0},Time: {1},Subsidy:{2} BHP,Current:{3} BHP, Total:{4} BHP", blockIndex, bhpCreationTime.ToLocalTime(),
                    nSubsidy, lastTotal, totalSubsidy);
                Console.WriteLine(line);

                if (lastSubsidy != nSubsidy)
                {
                    sw.WriteLine(line);
                    sw.Flush();

                    lastSubsidy = nSubsidy;
                    lastTotal = Fixed8.Zero;
                }

                bhpCreationTime = bhpCreationTime.AddSeconds(15);
                blockIndex++;

            }
            Console.WriteLine(blockIndex);

            sw.WriteLine($"The End {DateTime.Now}");
            sw.Flush();

            //关闭流
            sw.Close();
            fs.Close();
        }


 
    }
}
