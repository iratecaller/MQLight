# MQLight
## Unobtrusive Message Queue for Games and Business Applications

MQLight is designed for use in games but can be used just about anywhere messaging is required within your application.

## License

Completely free to use for whatever purpose. I make no warranties.

## User Guide

This will be published in the Wiki when I get to it. For now, this readme, and the code in the demo should help. This readme has
a few complete examples at the end.


##NuGet

    Install-Package MQLight 

## Description

The key that makes MQLight special is that it is designed with games in mind. Specifically, MQLight doesn't service message subcribers
and pump out messages when it feels like it. On the contrary, YOU must tell it when to do so. (Your game's update loop is a prime spot.)

At it's simplest, if your application only needs a single message queue (normal case), you can use MQ.Default  for all your needs:

     MQ.Default.Enqueue();
     MQ.Default.Subscribe();
     MQ.Default.Peek();
     MQ.Default.Dequeue();
     MQ.Default.Purge();
     MQ.Default.Update();  // Call this often!

## Important!

At the top of your game's Update cycle, where you check inputs, and update motion and all that fun stuff, just precede everything with:

    MQ.Default.Update();

What this does is process all freshly enqueued messages (during your last update cycle) and services the subscribers (if any), then
pumps out the messages to the approriate queues.   

## A note on subscribers

Firstly, the publisher/subscriber approach is stricly optional, but beneficial at times. 

When a subscriber receives a message, it can choose to leave the message in queue, or have it removed. Simply return 'true' to
remove it, or 'false' to leave it there. Leaving messages in queue is a great way for a subscriber to act as a Monitor.

## Example Subscriber Behavior

        private static bool GotMessage(Message m)
        {
            Console.WriteLine("Got message \"{0}\" and removing it.", (string)m.message);
            return true;  // returning true removes the message.
        }

Subscribers should not invoke methods on the queue. They should simply process each message as they are received.


## Usage / API

As mentioned, I'll create a Wiki when I get the chance. For now, the information in this readme should be enogh to get your started.

MQLight is very easy to use. Check out the demo app for now.

## Example 1 - Pub / Sub pattern

NOTE: For this example, the Update() method is called in a thread. MQLight is thread safe but your game however should never do this.
For games, make sure to call Update() at the very start of your game's principal update loop.

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

## Example 2 - Enqueue / Dequeue

This examples has two worker threads communicating with each other, and a queue update thread that tells them both to shut down after
5 seconds.

NOTE: Again this example uses threads. Great for business applications, but not for games. Stay away from threads in your games unless
you are a veteran game dev (unlike myself.)

This demo runs when Run() is invoked.

      
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

     








