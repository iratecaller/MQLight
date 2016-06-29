using MQLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQLightDemo
{
    public class EnqueueDequeueDemo
    {
        static int ID_QUEUE_A = 0;
        static int ID_QUEUE_B = 1;
        static int GREETING = 1000;
        static int SHUTDOWN = 2000;

        static async Task WorkerA()
        {
            Random r = new Random(12312);
            bool gotshutdown = false;

            while (!gotshutdown)
            {
                // send a message
                MQ.Default.Enqueue(ID_QUEUE_A, ID_QUEUE_B, GREETING, "Hello from A");

                // waitca bit
                await Task.Delay((int)(r.NextDouble() * 1000.0));

                // check my own queue 
                var M = MQ.Default.Dequeue(ID_QUEUE_A);


                while (M != null)
                {
                    if (M.type == GREETING)
                    {
                        Console.WriteLine("A recieved: " + (string)M.message);
                    }
                    else if (M.type == SHUTDOWN)
                    {
                        gotshutdown = true;
                        break;
                    }
                    M = MQ.Default.Dequeue(ID_QUEUE_A);
                }

            }

            Console.WriteLine("A has shut down.");
        }

        static async Task WorkerB()
        {
            Random r = new Random(543);
            bool gotshutdown = false;
            while (!gotshutdown)
            {
                // send something to A
                MQ.Default.Enqueue(ID_QUEUE_B, ID_QUEUE_A, GREETING, "Hello from B");

                // wait a bit
                await Task.Delay((int)(r.NextDouble() * 1000.0));


                // check my own queue 
                var M = MQ.Default.Dequeue(ID_QUEUE_B);

                while (M != null)
                {
                    if (M.type == GREETING)
                    {
                        Console.WriteLine("B recieved: " + (string)M.message);
                    }
                    else if (M.type == SHUTDOWN)
                    {
                        gotshutdown = true;
                        break;
                    }
                    M = MQ.Default.Dequeue(ID_QUEUE_B);
                }
            }
            Console.WriteLine("B has shut down.");
        }

        static async Task QueueUpdater()
        {
            double t = 0;
            DateTime last = DateTime.Now;
            double timer = 5.0;
            while (true)
            {


                await Task.Delay(1000);
                DateTime cur = DateTime.Now;
                t = (cur - last).TotalSeconds;

                last = cur;
                Console.WriteLine("Refresh delta: " + t);
                timer -= t;
                if (timer < 0)
                {
                    MQ.Default.Enqueue(-1, ID_QUEUE_A, SHUTDOWN, null);
                    MQ.Default.Enqueue(-1, ID_QUEUE_B, SHUTDOWN, null);
                    // update one last time so message propagagte 
                    MQ.Default.Update();
                    break;
                }
                MQ.Default.Update();
            }
            Console.WriteLine("Queue updater has shut down.");

        }
        public static void Run()
        {
            var ta = Task.Run(() => WorkerA());
            var tb = Task.Run(() => WorkerB());
            var tc = Task.Run(() => QueueUpdater());

            Task.WaitAll(ta, tb, tc);

        }
    }
}
