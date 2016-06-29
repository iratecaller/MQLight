# MQLight
## Unobtrusive Message Queue for Games and Business Applications

MQLight is designed for use in games but can be used just about anywhere messaging is required within your application.

## Features

1. Game developer friendly! (Read: Fast, Easy, Small, and Predictable) 
    
     - I designed it for games, but have used it in business apps too. Yawn.

2. Publisher / Subscriber support
    
    - Can choose to subscribe to all messages, messages of a certain type, or messages sent to specific destinations.
    - Subscribers can leave messages in queue and act as monitors
    
3. Source / Destintation queue routing

    - When enqueueing / dequeueing messages, target queues are specified. 
    - Support for any number of destination queues (as expected)

4. Simple and Unobtrusive. 

    - minimalistic API with absolutely no intention of making it anymore complex.

5. Fast (due to it's simple implementation.)

    - It should be about as fast as possible since there is no "dead letter" management, message expiry and all that stuff.
    - Freshly inserted messages are held out of the main queues until the Update cycle is called. This keeps things snappy.
    
6. Thread safe
    - Makes use of distinct C# locks for inerting, removing and updating.  
    

## Limitations

This version does not support purging 'old' messages automatically, or moving messages to a dead letter queue. That is to say that if your app/game ignores messages, or forgets to check certain queues, then you can have a surprise when you run out of memory!

The reasons MQLight doesn't support this is to make it as responsive as possible.  

I have no plans to support this mechanism for MQLight. I have have a more enterpris-y Message Queue that is backed by SQL Server and supports expiration, dead letter queues and all that typical enterprise capability. The reason why enterprise Message Queue system are so bloated, is that they absolutely need to guarantee that messages get delivered. IBM WebShpere MQ, Rabbit MQ, and Microsoft's MSMQ are all fantastic products which I love using, but, for most client-side apps like games, these aren't necessary.

You wouldn't use MSMQ for games. It's an awesome product, but it's just not designed for this purpose.
Similarly, you wouldn't want to use MQLight on a government social security server because it cannot guarantee delivery.


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

Subscribers should not read more messages from the queue. They should simply process each message as they are received. Subscribers are
of course free to enqueue new messages. 


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
        
                        // wait a hundred ms, then subtract time delta from countdown timer.
                        await Task.Delay(100);
                        DateTime cur = DateTime.Now;
                        t = (cur - last).TotalSeconds;
                        last = cur;
                        timer -= t;
                        
                        // that's it we're done.
                        if (timer < 0)
                        {
                            MQ.Default.Enqueue(-1, -1,-1, "Goodbye!");
                        
                            // update one last time so message propagates 
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
      
              public static void Run()
              {
                  var ta = Task.Run(() => WorkerA());
                  var tb = Task.Run(() => WorkerB());
                  var tc = Task.Run(() => QueueUpdater());
      
                  Task.WaitAll(ta, tb, tc);
      
              }
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
                  double timer = 5.0; // run for 5 seconds;
                  
                  while (true)
                  {
      
                      // wait 100 ms, then subtract the time delta from the countdown timer.
                      await Task.Delay(100);
                      DateTime cur = DateTime.Now;
                      t = (cur - last).TotalSeconds;
                      last = cur;
                      timer -= t;
                      
                      // once the timer is done, so are we.
                      if (timer < 0)
                      {
                          // send the shutdown messages and bail out.
                          MQ.Default.Enqueue(-1, ID_QUEUE_A, SHUTDOWN, null);
                          MQ.Default.Enqueue(-1, ID_QUEUE_B, SHUTDOWN, null);
                          // IMPORTANT:  update one last time so message propagagte 
                          MQ.Default.Update();
                          break;
                      }
                      
                      
                      MQ.Default.Update();
                  }
                  Console.WriteLine("Queue updater has shut down.");
      
              }
              
              
          }
      }

     








