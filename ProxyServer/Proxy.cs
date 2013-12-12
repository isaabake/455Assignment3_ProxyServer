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

        /// <summary>
        /// Starts the proxy server listening on port 8888.
        /// </summary>
        public void Run()
        {
            try
            {
                IPAddress ip = IPAddress.Parse("127.0.0.1");
                this.tcpListener = new TcpListener(ip, ListenPort);
                this.tcpListener.Start();
            }
            catch (Exception e)
            {

                throw;
            }

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

        /// <summary>
        /// Handles a connection from a browser to the proxy server
        /// </summary>
        /// <param name="client">A TcpClient connection from the browser</param>
        private void HandleClient(TcpClient client)
        {
            byte[] message = new byte[40960];
            int bytecount = 0;
            ASCIIEncoding encoder = new ASCIIEncoding();
            NetworkStream clientStream = client.GetStream();

            //Get the header
            string header = string.Empty;
            try
            {
                header = ReadHeader(clientStream);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Execpetion with message: {0}", e.Message);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }

            Console.WriteLine("-----------------------Original Header----------------------------");
            Console.WriteLine(header);
            Console.WriteLine("------------------------------------------------------------------");

            string modifiedHeader = string.Empty;

            //Parse the headers, take out headers with "Connection" and append a "Connection: close" header to the end.
            foreach (string line in header.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("Connection") || line.StartsWith("\r")) //Remove Connection: xxx and Proxy-Connection: xxx headerss
                {
                    continue;
                }
                else
                {
                    modifiedHeader += line + '\n';
                }
            }
            modifiedHeader += "Connection: close\r\n\r\n";

            Console.WriteLine("-----------------------Modified Header----------------------------");
            Console.WriteLine(modifiedHeader);
            Console.WriteLine("------------------------------------------------------------------");

            string[] items = modifiedHeader.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            string serverAddress = items.First(s => s.StartsWith("Host:")).Split(' ')[1];       //Get the host address

            int contentLength = 0;
            string POSTContent = string.Empty;

            //Check if we have a POST header.
            if (items.Where(line => line.Contains("POST")).Count() > 0)
            {
                //Parse out the Content-length
                string contentLengthLine = items.Where(line => line.Contains("Content-Length")).FirstOrDefault();
                if (contentLengthLine != null)
                {
                    int.TryParse(contentLengthLine.Split(' ')[1], out contentLength);
                    bytecount = 0;
                    
                    while (bytecount < contentLength)
                    {
                        try
                        {
                            bytecount += clientStream.Read(message, 0, 40960);  //Read the POST content 
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("ERROR: Execpetion with message: {0}", e.Message);
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey(true);

                        }
                        
                        POSTContent += encoder.GetString(message, 0, bytecount);   //Append all the post content into one string
                    }
                }
            }

            NetworkStream serverStream;
            TcpClient server;

            //Connect to the remote server
            try
            {
                server = new TcpClient();
                IPAddress[] serverIP = Dns.GetHostAddresses(serverAddress);
                IPEndPoint serverEndPoint = new IPEndPoint(serverIP[0], 80);
                server.Connect(serverEndPoint);
                serverStream = server.GetStream();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Execpetion with message: {0}", e.Message);
                Console.WriteLine("Could not connect to remote server. Exiting...");
                Console.ReadKey(true);
                return;
            }

            
            //Write out the modified header to the server
            byte[] buffer = encoder.GetBytes(modifiedHeader);
            try
            {
                serverStream.Write(buffer, 0, buffer.Length);
                serverStream.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Execpetion with message: {0}", e.Message);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            

            //If POST, write conent
            if (contentLength > 0)
            {
                buffer = encoder.GetBytes(POSTContent);
                try
                {
                    serverStream.Write(buffer, 0, buffer.Length);
                    serverStream.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Execpetion with message: {0}", e.Message);
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
                Console.WriteLine("Sent POST to server");
                Console.WriteLine("POST: " + POSTContent);
            }
            
            //Read content from the server and pass it to the client in ~40k chunks
            while (true)
            {
                Array.Clear(message, 0, 40960);
                try
                {
                    bytecount = serverStream.Read(message, 0, 40960);   //Read the resposne from the server
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Execpetion with message: {0}", e.Message);
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
                
                string response = encoder.GetString(message);
                //Console.WriteLine("RESPONSE: " + response);           //Print the response data to standard out. WARNING: Can be huge
                if (bytecount == 0)
                {
                    break;
                }
                try
                {
                    clientStream.Write(message, 0, bytecount);  //Write the data to the client
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Execpetion with message: {0}", e.Message);
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
                
            }
            Console.WriteLine("Recieved response and sent to client");

            //Close the connections
            server.Close();
            client.Close();
        }

        /// <summary>
        /// Reads a header from a client stream. Stops on "\r\n\r\n".
        /// </summary>
        /// <param name="stream">The client NetworkStream to read a header from.</param>
        /// <returns>The header from the client stream.</returns>
        private string ReadHeader(NetworkStream stream)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string header = string.Empty;
            byte[] message = new byte[1];
            int bytesRead = 0;
            try
            {
                //reads header
                while (true)
                {
                    //read 1 byte from buffer at a time and concat to header
                    bytesRead = stream.Read(message, 0, 1); 
                    header += encoder.GetString(message, 0, bytesRead);

                    //Stop on end of header
                    if (header.EndsWith("\r\n\r\n"))
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                //a socket error has occured
                throw e;
            }
            return header;
        }
    }
}
