using System;
using OPC.Task.Task;

namespace OPC.Task
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
