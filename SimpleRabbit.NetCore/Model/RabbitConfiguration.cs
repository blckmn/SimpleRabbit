using System;
using System.Collections.Generic;

namespace SimpleRabbit.NetCore.Model
{
    public class RabbitConfiguration
    {
        public Uri Uri { get; set; }
        public List<string> Hostnames { get; set; }
    }
}
