using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyServer
{
    class Proxy
    {
        private static int ListenPort = 8888;
        private TcpListener tcpListener;
        private Task listenTask;

        public Proxy()
        {
        }

        public void Run()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, ListenPort);
            this.listenTask = new Task(ListenForClients);
            this.listenTask.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication 
                //with connected client
                Task clientTask = new Task(() => HandleClient(client));
                clientTask.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream clientStream = client.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
                System.Diagnostics.Debug.WriteLine(encoder.GetString(message, 0, bytesRead));
            }

            client.Close();
        }
    }
}
