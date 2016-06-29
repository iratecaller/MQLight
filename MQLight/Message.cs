using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MQLight
{
    public class Message
    {
        public Message()
        {
        }

        public long dest
        {
            get;
            set;
        }

        public object message
        {
            get;
            set;
        }

        public long source
        {
            get;
            set;
        }
        public long type
        {
            get;
            set;
        }
    }
}
