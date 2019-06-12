using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;

namespace CSFinal_Server
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
    class Server
    {
        private static Socket server;
        private static readonly ManualResetEvent mre = new ManualResetEvent(false);
        
        public static void Create()
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public static void StartListening()
        {
            Console.WriteLine("Listening for connections...");
            try
            {
                server.Bind(new IPEndPoint(IPAddress.Parse("192.168.56.1"), 8080));
                server.Listen(100);
                while (true)
                {
                    mre.Reset();
                    server.BeginAccept(AcceptCallBack, server);
                    mre.WaitOne();
                }
            } catch(Exception e)
            {
                Console.WriteLine("An error occured while listening for connections: " + e.StackTrace);
            }
        }

        private static void AcceptCallBack(IAsyncResult result)
        {
            mre.Set();
            Console.WriteLine("Connection found\n");
            Socket listener = result.AsyncState as Socket;
            Socket client = listener.EndAccept(result);
            State state = new State(client, new byte[2048]);
            state.WorkSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, RecieveCallBack, state);
        }
        
        private static void RecieveCallBack(IAsyncResult result)
        {
            try
            {
                State state = result.AsyncState as State;
                int recieve = state.WorkSocket.EndReceive(result);
                if (recieve > 0)
                {
                    Console.WriteLine("\nRecieving message" + Encoding.UTF8.GetString(state.Buffer));
                    state.SBuild.Append(Encoding.UTF8.GetString(state.Buffer), 0, state.BufferLength);

                    Console.WriteLine("{0}\nMessage Recieved From {1}:\n{2}\n{3}",
                                      FormatLine(),
                                      (state.WorkSocket.RemoteEndPoint as IPEndPoint).Address.ToString(),
                                      state.SBuild.ToString().Replace("\0", string.Empty),
                                      FormatLine());

                    State nState = new State(state.WorkSocket, new byte[2048]);
                    string[] files = state.SBuild.ToString().Replace("\0", string.Empty).Split(new string[] { "%%2" }, 0);

                    for (int x = 0; x < files.Length; x++)
                    {
                        if (files[x] == string.Empty) continue;

                        string file = files[x];
                        string path = "C:/Users/AntPan/Desktop/Recieved/" + file.Split('$')[file.Split('$').Length - 1];

                        Console.WriteLine("Saving file: {0}\n", path);
                        File.WriteAllBytes(path, Encoding.UTF8.GetBytes(CreateFileText(file)));
                    }

                    state.WorkSocket.BeginReceive(nState.Buffer, 0, nState.BufferLength, 0, RecieveCallBack, nState);
                }
            } catch (Exception e)
            {
                Console.WriteLine("An Error Occured: " + e.StackTrace);
            }
        }

        private static string CreateFileText(string message)
        {
            string[] bits = message.Split('$');
            string toReturn = "";
            for (int x = 0; x < bits.Length - 1; x++)
                toReturn += bits[x];
            return toReturn;
        }

        private static string FormatLine()
        {
            string s = "";
            for (int x = 0; x < 50; x++)
                s += "-";
            return s;
        }
    }
}
