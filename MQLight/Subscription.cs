using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MQLight
{

    public class Subscription
    {
        public Subscription()
        {
        }

        public MessageAvailabilityEvent evt
        {
            get;
            set;
        }

        public long id
        {
            get;
            set;
        }

        public SubscriptionType type
        {
            get;
            set;
        }
    }
}
