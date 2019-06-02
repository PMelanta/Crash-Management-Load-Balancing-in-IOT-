using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
namespace UM
{
    public partial class MonitoringFrm : Form
    {
        public MonitoringFrm()
        {
            InitializeComponent();
        }

   
        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;
        Thread thread, broadcastThread;

        MqttClient client = null, reqclient = null, clientack = null;
        string clientId;
        string mqttserverip = "18.188.213.251";
        string ipaddress= string.Empty;
        public void BroadCast()
        {
            int p = 0;
            while (true)
            {
                if (TxtCpuUsage.Text.Length == 0 || TxtMemory.Text.Length == 0) continue;
                string message =ipaddress + "," + TxtCpuUsage.Text + "," + TxtMemory.Text;
                client.Publish("dc113", Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                Thread.Sleep(5000);
            }
        }

        public IPAddress GetIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public void Find()
        {
            while (true)
            {
                try
                {
                    TxtCpuUsage.Text = getCurrentCpuUsage();
                    TxtMemory.Text = getAvailableRAM();
                    Thread.Sleep(5000);
                    if (TxtCpuUsage.Text.Length != 0 && TxtMemory.Text.Length != 0)
                    {
                        ChartRep.Series[0].Points.AddXY("CPU", double.Parse(TxtCpuUsage.Text) * 100);
                        ChartRep.Series[1].Points.AddXY("RAM", double.Parse(TxtMemory.Text));
                    }
                }
                catch (Exception e) { }
            }

        }

        public string getCurrentCpuUsage()
        {
            return cpuCounter.NextValue().ToString();
        }

        public string getAvailableRAM()
        {
            return ramCounter.NextValue().ToString();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            TextBox.CheckForIllegalCrossThreadCalls = false;
            LstReqViewer.Items.Clear();
            if (BtnStart.Tag.ToString() == "Start")
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                client = new MqttClient(mqttserverip);
                clientId = Guid.NewGuid().ToString();
                client.Connect(clientId);

                thread = new Thread(new ThreadStart(Find));
                thread.Start();
                
          
                broadcastThread = new Thread(new ThreadStart(BroadCast));
                broadcastThread.Start();

                BtnStart.Tag = "Stop";
            }
            else
            {
                clientack.Publish("dc113", Encoding.UTF8.GetBytes("dead"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                thread.Abort();
                broadcastThread.Abort();
                BtnStart.Tag = "Start";
            }
        }


        void reqclient_MqttRequestUrlPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            string[] request = ReceivedMessage.Split(',');
            ListViewItem L = new ListViewItem(request[0]);
            L.SubItems.Add(request[1]); 
            string fullpath = TxtPath.Text + "\\" + request[1];
            byte[] bytedata = null;
            if (System.IO.File.Exists(fullpath))
            {
                bytedata = System.IO.File.ReadAllBytes(fullpath);
                L.SubItems.Add("File sent");
            }
            else
            {
                bytedata = ASCIIEncoding.ASCII.GetBytes("File not found..");
                L.SubItems.Add("File not found");
            }
            LstReqViewer.Items.Add(L);
            string pagecontent = ASCIIEncoding.ASCII.GetString(bytedata);
            clientack.Publish("dc113ack", Encoding.UTF8.GetBytes(pagecontent), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }

        private void MonitoringFrm_Load(object sender, EventArgs e)
        {
            ipaddress = GetIPAddress().ToString();
            reqclient = new MqttClient(mqttserverip);
            string clientId = Guid.NewGuid().ToString();
            reqclient.Connect(clientId);
            reqclient.Subscribe(new string[] { ipaddress }, new byte[] { 2 });
            reqclient.MqttMsgPublishReceived += reqclient_MqttRequestUrlPublishReceived;


            clientack = new MqttClient(mqttserverip);
            string clientId1 = Guid.NewGuid().ToString();
            clientack.Connect(clientId1);
        }

        private void clearItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LstReqViewer.Items.Clear();
        }

        private void BtnDirBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtPath.Text = dialog.SelectedPath;
        } 
    }
}
