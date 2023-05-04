using System.Collections.Generic;

namespace SimpleRabbit.NetCore
{
    public class RabbitConfiguration
    {
        public List<string> Hostnames { get; set; }
        /// <summary>
        /// Name of the connection to RabbitMQ. 
        /// </summary>
        /// <remarks>This is only used to provide a user-friendly name on the RabbbitMQ end</remarks>
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// The end part after a host name, defaulted to "/"
        /// </summary>
        public string VirtualHost { get; set; }
        /// <summary>
        /// Amount of time before re-trying to recover a connection
        /// defaults to 10 seconds
        /// </summary>
        public int? NetworkRecoveryIntervalInSeconds { get; set; }
        /// <summary>
        /// maximum time between heartbeats, (two failed hearts will cause a connection to be invalidated)
        /// defaults to 5 seconds
        /// </summary>
        public ushort? RequestedHeartBeat { get; set; }
        /// <summary>
        /// Flag to indicate whether a connection should be automatically recovered, true by default
        /// </summary>
        public bool? AutomaticRecoveryEnabled { get; set; }
        /// <summary>
        /// Flag to indicate whether a connection recovery should also include topology (exchanges, queues, bindings) made in the connection. this is default to true
        /// </summary>
        public bool? TopologyRecoveryEnabled { get; set; }
        /// <summary>
        /// You should set this flag to false when you are using non-async message handler, default: true
        /// </summary>
        public bool? UseAsyncDispatch { get; set; }
    }
}
