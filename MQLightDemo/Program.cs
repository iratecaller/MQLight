using MQLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQLightDemo
{
    class Program
    {
      
        static void Main(string[] args)
        {
            EnqueueDequeueDemo.Run();
            PubSubDemo.Run();
            Console.WriteLine("Finished.");
            Console.ReadLine();
        }

    }
}
