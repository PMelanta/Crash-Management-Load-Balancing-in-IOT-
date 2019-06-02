using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
namespace Master
{
    public partial class Form1 : Form
    {
        Thread   reqthread;

        MqttClient client = null , mqttsubmiturl=null;
        string clientId, clientId1;
        string mqttserverip = "18.188.213.251";
        string MQTTMESSAGE = string.Empty;
        public Form1()
        {
            InitializeComponent();
        }
       

       
        void FindEfficient(ref string ip)
        {
            int sz = LoadList.Items.Count;
            if (sz == 0) return;
            ListViewItem item = new ListViewItem();
            double small = 0;
            if (TimeOption.Checked)
                small = Double.Parse(LoadList.Items[0].SubItems[1].Text); //cpu
            else
                small = Double.Parse(LoadList.Items[0].SubItems[2].Text); //time
            item = LoadList.Items[0];  // ip
            for (int i = 1; i < sz; i++)
            {
                double cval = 0;
                if (TimeOption.Enabled)
                    cval = Double.Parse(LoadList.Items[i].SubItems[1].Text);  // cpu
                else
                    cval = Double.Parse(LoadList.Items[i].SubItems[2].Text); // mem

                
                if (cval < small)
                {
                    small = cval;
                    item = LoadList.Items[i];
                }
            }

            TxtIpAddress.Text = item.SubItems[0].Text;  // ip
            if (TimeOption.Checked)
                TxtValue.Text = item.SubItems[1].Text;  // cpu 
            else
                TxtValue.Text = item.SubItems[2].Text;  // mem

            ip = TxtIpAddress.Text;
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (BtnStartStop.Text == "Start")
            {
               
                //thread = new Thread(new ThreadStart(ReadLoad));
                //thread.Start();
                client = new MqttClient(mqttserverip);
                mqttsubmiturl = new MqttClient(mqttserverip);

                client.Subscribe(new string[] {"dc113"}, new byte[] { 2 });
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                 mqttsubmiturl.Subscribe(new string[] {"dc113url"}, new byte[] { 2 });
                 mqttsubmiturl.MqttMsgPublishReceived += client_accepturl;
                // use a unique id as client id, each time we start the application
                clientId = Guid.NewGuid().ToString();
                clientId1 = Guid.NewGuid().ToString();
                
                client.Connect(clientId);
                mqttsubmiturl.Connect(clientId1);

                reqthread = new Thread(new ThreadStart(ReqHandler));
                reqthread.Start();
                BtnStartStop.Text = "Stop";
            }
            else
            {
                client.Disconnect();
                mqttsubmiturl.Disconnect();
                reqthread.Abort();
                BtnStartStop.Text = "Start";
            }
        }

        private void ReqHandler()
        {
            string ip = string.Empty;
            while (true)
            {
                FindEfficient(ref ip);
                if (ip != string.Empty && ReqListView.Items.Count != 0)
                {
                    mqttsubmiturl.Publish(ip, Encoding.UTF8.GetBytes(ip + "," + ReqListView.Items[0].SubItems[1].Text), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    ReqListView.Items.RemoveAt(0);
                    ip = string.Empty;
                }
                Thread.Sleep(3000);
            }
        }

        public IPAddress GetIPAddress()
        {
            var host =  Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            TextBox.CheckForIllegalCrossThreadCalls = false;
            ListBox.CheckForIllegalCrossThreadCalls = false;
        }

        void client_accepturl(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            string[] request = ReceivedMessage.Split(',');   // contains IP and Url address
            if (request.Length == 2 && request[0]!="0" && request[1]!="0")
            {
                ListViewItem L = new ListViewItem(request[0]);
                L.SubItems.Add(request[1]);
                ReqListView.Items.Add(L);
            }
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ip = string.Empty;
           
            MQTTMESSAGE = Encoding.UTF8.GetString(e.Message);
            if (MQTTMESSAGE == "dead")
            {
                LoadList.Items.Clear();
                lblCount.Text = "(@)";
                return;
            }

            string[] cols = MQTTMESSAGE.Split(','); // ip, cpu,mem
            ListViewItem item = new ListViewItem(cols[0]);  // ip
            item.Name = cols[0]; // ip 
            item.SubItems.Add(cols[1]);   // cpu
            item.SubItems.Add(cols[2]);   //mem
            if (LoadList.Items.ContainsKey(cols[0]) == false)
                LoadList.Items.Add(item);
            else
            {
                ListViewItem[] L = LoadList.Items.Find(cols[0], false);
                L[0].SubItems[1].Text = cols[1];
                L[0].SubItems[2].Text = cols[2];
            }
            

            lblCount.Text = LoadList.Items.Count.ToString();
            
        }
 
    }
}
