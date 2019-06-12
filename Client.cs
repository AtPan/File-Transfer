using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;

namespace CSFinal_Client
{
    class State
    {
        public byte[] Buffer { get; }
        public Socket WorkSocket { get; }
        public StringBuilder SBuild { get; }
        public int BufferLength => Buffer.Length;
        public State(Socket socket, byte[] buffer)
        {
            WorkSocket = socket;
            Buffer = buffer;
            SBuild = new StringBuilder();
        }
    }
    class Client
    {
        private readonly ManualResetEvent mre = new ManualResetEvent(false);
        private readonly TcpClient client;

        public Client()
        {
            client = new TcpClient();
        }

        public void StartClient()
        {
            IPAddress ip = IPAddress.Parse("192.168.56.1");
            int port = 8080;
            IPEndPoint ipEnd = new IPEndPoint(ip, port);
            client.Connect(ipEnd);
            Socket handler = client.Client;

            string path;

            Console.Write("Would you like to send a file or a directory: ");
            if(Console.ReadLine().ToLower() == "file")
            {
                Console.Write("Choose a File to Send: ");
                path = Console.ReadLine();
                while(!File.Exists(path))
                {
                    Console.Write("ERROR: File Entered Does not Exist on Selected Disk\nChoose a File to Send: ");
                    path = Console.ReadLine();
                }
                FileStream file = File.OpenRead(path);
                byte[] b = new byte[2048];
                file.Read(b, 0, b.Length);
                string fileSend = Encoding.UTF8.GetString(b) + "$" + file.Name + "%%2";

                b = Encoding.UTF8.GetBytes(fileSend);

                Console.Write("Sending File: " + file.Name + "... ");
                handler.BeginSend(b, 0, b.Length, 0, SendCallBack, new State(handler, b));

            } else {
                Console.Write("Choose a Directory to Send: ");
                path = Console.ReadLine();
                while (!Directory.Exists(path))
                {
                    Console.Write("ERROR: Directory Entered Does not Exist on Selected Disk\nChoose a Directory to Send: ");
                    path = Console.ReadLine();
                }
                DirectoryInfo directory = new DirectoryInfo(path);
                IEnumerator<FileInfo> files = directory.EnumerateFiles().GetEnumerator();
                Console.WriteLine(directory.GetFiles().Length);

                while (files.MoveNext())
                {
                    mre.Reset();
                    FileInfo file = files.Current;
                    Console.Write("Sending File: " + file.Name + "... ");

                    byte[] b = new byte[file.Length];
                    file.OpenRead().Read(b, 0, b.Length);
                    string fileSend = Encoding.UTF8.GetString(b) + "$" + file.Name + "%%2";
                    b = Encoding.UTF8.GetBytes(fileSend);
                    handler.BeginSend(b, 0, b.Length, 0, SendCallBack, new State(handler, b));

                    mre.WaitOne();
                }
            }

            handler.BeginDisconnect(false, DisconnectCallBack, handler);
        }

        private void SendCallBack(IAsyncResult result)
        {
            State state = result.AsyncState as State;
            state.WorkSocket.EndSend(result);
            Console.WriteLine("File Sent");
            mre.Set();
        }

        private void DisconnectCallBack(IAsyncResult result)
        {
            Socket handler = result.AsyncState as Socket;
            handler.EndDisconnect(result);
            Console.WriteLine("Disconnected");
        }
    }
}
