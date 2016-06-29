using MQLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQLightDemo
{
    public class PubSubDemo
    {
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
                    MQ.Default.Enqueue(-1, -1,-1, "Goodbye!");
                
                    // update one last time so message propagagte 
                    MQ.Default.Update();
                    break;
                }
                MQ.Default.Enqueue(-1, -1, -1, "Hello!");
                MQ.Default.Update();
            }
            Console.WriteLine("Queue updater has shut down.");

        }
        public static void Run()
        {
            MQ.Default.Purge();

            MQ.Default.Subscribe(SubscriptionType.ALL, 0, GotMessage);
            var task = Task.Run(() => QueueUpdater());

            Task.WaitAll(task);
            Console.WriteLine("Pub Sub demo complete.");
        }

        private static bool GotMessage(Message m)
        {
            Console.WriteLine("Got message \"{0}\" and removing it.", (string)m.message);
            return true;
        }
    }
}
