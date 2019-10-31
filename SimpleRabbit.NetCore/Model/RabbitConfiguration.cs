using System.Collections.Generic;

namespace SimpleRabbit.NetCore
{
    public class RabbitConfiguration
    {
        public List<string> Hostnames { get; set; }
        
        public string Protocol { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
    }
}
