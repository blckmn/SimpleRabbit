using System;
using System.Collections.Generic;

namespace SimpleRabbit.NetCore
{
    public class RabbitConfiguration
    {
        public Uri Uri { get; set; }
        public List<string> Hostnames { get; set; }
    }
}
