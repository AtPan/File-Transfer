using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSFinal_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Server.Create();
            Server.StartListening();
            
        }

        
    }
}
