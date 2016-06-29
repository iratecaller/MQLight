using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MQLight
{
    public class MQ
    {
        private static MQ _instance;
        public static MQ Default
        {
            get
            {
                if (_instance == null)
                    _instance = new MQ();
                return _instance;
            }
        }
        private Dictionary<long, List<Message>> queue;

        private List<Subscription> subscribers;

        private object syncInserts;

        private object syncQueue;

        private object syncSubscribers;

        private List<Message> toinsert;

        public MQ()
        {
            syncQueue = new object();
            syncInserts = new object();
            syncSubscribers = new object();
            queue = new Dictionary<long, List<Message>>();
            toinsert = new List<Message>();
            subscribers = new List<Subscription>();
        }
        public Message Dequeue(long destination_id)
        {
            Message ret = null;
            lock (syncQueue)
            {
                List<Message> Q = null;
                if (queue.TryGetValue(destination_id, out Q) && Q != null)
                {
                    if (Q.Count > 0)
                    {
                        ret = Q[0];
                        Q.RemoveAt(0);
                    }
                    if (Q.Count == 0)
                    {
                        queue.Remove(destination_id);
                    }
                }
            }
            return ret;
        }

        public void Enqueue(Message m)
        {
            lock (syncInserts)
            {
                toinsert.Add(m);
            }
        }

        public void Enqueue(long source, long dest, long type, object payload)
        {
            Enqueue(new Message()
            {
                source = source,
                dest = dest,
                type = type,
                message = payload
            });
        }

        public Message Peek(long destination_id)
        {
            Message ret = null;
            lock (syncQueue)
            {
                List<Message> Q = null;
                if (queue.TryGetValue(destination_id, out Q) && Q != null)
                {
                    if (Q.Count > 0)
                    {
                        ret = Q.First();
                    }
                }
            }
            return ret;
        }

        public void Purge()
        {
            lock (syncQueue)
            {
                queue.Clear();
            }
        }

        public void PurgeDestinationQueue(long destinationid)
        {
            lock (syncQueue)
            {
           
                if (queue.ContainsKey(destinationid))
                {
                    queue.Remove(destinationid);
                }
            }
        }

        public void PurgeMessagesOfType(long typeid)
        {
            lock (syncQueue)
            {
                List<long> queueids = queue.Keys.ToList();

                foreach (var qid in queueids)
                {
                    List<Message> Q;
                    if (queue.TryGetValue(qid, out Q))
                    {
                        
                        var F = from x in Q  where x.type == typeid select x;
                        foreach (var f in F)
                        {
                            Q.Remove(f);
                        }
                        if (Q.Count == 0)
                        {
                            queue.Remove(qid);
                        }
                    }
                }
            }
        }

        public void Subscribe(SubscriptionType type, long id, MessageAvailabilityEvent evt)
        {
            lock (syncSubscribers)
            {
                subscribers.Add(new Subscription()
                {
                    evt = evt,
                    id = id,
                    type = type
                });
            }
        }

        public void Update()
        {
            Message[] new_messages;
            Subscription[] _subscribers;
            lock (syncInserts)
            {
                new_messages = toinsert.ToArray();
                toinsert.Clear();
            }

            lock (syncQueue)
            {
                // check for subscriptions
                lock (syncSubscribers)
                {
                    _subscribers = subscribers.ToArray();
                }

                if (_subscribers.Count() > 0)
                {
                    var alls = (from x in subscribers where x.type == SubscriptionType.ALL select x);
                    var dests = (from x in subscribers where x.type == SubscriptionType.DESTINATION select x);
                    var types = (from x in subscribers where x.type == SubscriptionType.MESSAGE_TYPE select x);

                    for (int i = 0; i < new_messages.Length; i++)
                    {
                        var M = new_messages[i];

                        if (M != null)
                        {
                            // peek to the ALLS
                            bool remove = false;
                            foreach (var S in alls)
                            {
                                if (S.evt(M))
                                {
                                    remove = true;
                                }
                            }
                            if (remove)
                            {
                                new_messages[i] = null;
                                M = null;
                            }
                        }

                        if (M != null)
                        {
                            foreach (var S in types)
                            {
                                if ((M.type == S.id) && S.evt(M))
                                {
                                    new_messages[i] = null;
                                    M = null;
                                    break;
                                }
                            }
                        }

                        if (M != null)
                        {
                            foreach (var S in dests)
                            {
                                if ((M.dest == S.id) && S.evt(M))
                                {
                                    new_messages[i] = null;
                                    M = null;
                                    break;
                                }
                            }
                        }
                    }
                }
                // enqueue non-subscribed messages, or messages that have not been processed.
                foreach (var M in new_messages)
                {
                    // if M hasn't been removed.
                    if (M != null)
                    {
                        List<Message> Q = null;
                        if (!queue.TryGetValue(M.dest, out Q))
                        {
                            Q = new List<Message>();
                            queue[M.dest] = Q;
                        }

                        Q.Add(M);
                        
                    }
                    
                }
            }
        }
    }
}
