using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBhp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Main");

            DateTime date = DateTime.Now;
            Console.WriteLine($"Locale: {date}");
            Console.WriteLine($"UTC:    {date.ToUniversalTime()}");
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            uint d = (uint)(date.ToUniversalTime() - unixEpoch).TotalSeconds;
            Console.WriteLine(d);

            TestMining.Test(null);

            Console.ReadLine();
        }
    }
}
