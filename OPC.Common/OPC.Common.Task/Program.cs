using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OPC.Common.Task.Task;

namespace OPC.Common.Task
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskMain main = new TaskMain();
            main.Start().Wait();
            Console.Read();
        }
    }
}
