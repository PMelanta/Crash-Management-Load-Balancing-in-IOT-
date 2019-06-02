using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Diagnostics;
namespace Client
{
    public partial class Form1 : Form
    {

        MqttClient client = null , recvclient = null;
        string clientId;
        string mqttserverip = "18.188.213.251";
        public Form1()
        {
            InitializeComponent();
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (TxtUrl.Text.ToLower().EndsWith(".txt"))
            {
                string message = GetIPAddress().ToString() + "," + TxtUrl.Text;
                client.Publish("dc113url", Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            }
            else
                MessageBox.Show("Sorry this version works on Text file (.txt) only for demo sake", "Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void Form1_Load(object sender, EventArgs e)
        {
            client = new MqttClient(mqttserverip);
            clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);

            recvclient = new MqttClient(mqttserverip);
            string clientId1 = Guid.NewGuid().ToString();
            recvclient.Connect(clientId1);

            recvclient.Subscribe(new string[] { "dc113ack" }, new byte[] { 2 });
            recvclient.MqttMsgPublishReceived += client_acceptack;

            client.Publish("dc113url", Encoding.UTF8.GetBytes("0,0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
     
            RichTextBox.CheckForIllegalCrossThreadCalls = false;
        }

        void client_acceptack(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            if( TxtUrl.Text.Length!=0 )
                richTextBox1.Text = ReceivedMessage;

        }

        

        private void BtnClear_Click(object sender, EventArgs e)
        {
            client.Disconnect();
            Application.ExitThread();
        }
    }
}
