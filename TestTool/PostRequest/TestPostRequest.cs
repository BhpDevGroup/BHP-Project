using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace PostResquest
{
    public partial class TestPostRequest : Form
    {
        private static System.Timers.Timer aTimer;
        private static System.Timers.Timer bTimer;
        public TestPostRequest()
        {
            InitializeComponent();
            InitTimer();
        }

        private void InitTimer()
        {
            aTimer = new System.Timers.Timer();

            //注册计时器的事件
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            //设置时间间隔为2秒（2000毫秒），覆盖构造函数设置的间隔
            aTimer.Interval = 120000;
            //设置是执行一次（false）还是一直执行(true)，默认为true
            aTimer.AutoReset = true;

            bTimer = new System.Timers.Timer();
            //注册计时器的事件
            bTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent2);
            //设置时间间隔为2秒（2000毫秒），覆盖构造函数设置的间隔
            bTimer.Interval = 10000;
            //设置是执行一次（false）还是一直执行(true)，默认为true
            bTimer.AutoReset = true;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        string sdsasdasd(string json)
        {
            try
            {
                string postAddress = textBox1.Text.Trim();
                HttpClient client = new HttpClient();
                //string uri = "127.0.0.1:20557";
                string uri = postAddress;
                client.BaseAddress = new Uri($"http:\\\\{postAddress}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                StringContent theContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(uri, theContent).Result;
                Task<System.IO.Stream> task = response.Content.ReadAsStreamAsync();
                System.IO.Stream backStream = task.Result;
                System.IO.StreamReader reader = new System.IO.StreamReader(backStream);
                string json1 = reader.ReadToEnd();
                return json1;
            }
            catch (Exception e)
            {
                return "error";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            aTimer.Start();
            bTimer.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            aTimer.Stop();
            bTimer.Stop();
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //unlock请求
            string json = " {  \"jsonrpc\": \"2.0\",  \"method\": \"unlock\",  \"params\": [\"1\",1],  \"id\": 1} ";
            
            this.Invoke(new Action(() => { listBox1.Items.Add(sdsasdasd(json)); })); 
        }
        private void OnTimedEvent2(object source, ElapsedEventArgs e)
        {
            //getblance
            string json = " { \"jsonrpc\": \"2.0\", \"method\": \"getbalance\",\"params\": [\"0x13f76fabfe19f3ec7fd54d63179a156bafc44afc53a7f07a7a15f6724c0aa854\"],\"id\": 1}";
            this.Invoke(new Action(() => { listBox2.Items.Add(sdsasdasd(json)); }));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //unlock请求
            string json = " {  \"jsonrpc\": \"2.0\",  \"method\": \"unlock\",  \"params\": [\"1\",1],  \"id\": 1} ";
            this.Invoke(new Action(() => { listBox1.Items.Add(sdsasdasd(json)); }));

            //getblance
            string json2 = " { \"jsonrpc\": \"2.0\", \"method\": \"getbalance\",\"params\": [\"0x13f76fabfe19f3ec7fd54d63179a156bafc44afc53a7f07a7a15f6724c0aa854\"],\"id\": 1}";
            this.Invoke(new Action(() => { listBox2.Items.Add(sdsasdasd(json2)); }));

        }
    }
}
