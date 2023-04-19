using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;

namespace Subscriber.Service.Service
{
    public record TestDeserialisedMessage(int Key, string Value);
    
    public class OrderedMessageHandler : OrderedMessageHandler<int, TestDeserialisedMessage>
    {
        public OrderedMessageHandler(ILogger<OrderedMessageHandler<int, TestDeserialisedMessage>> logger) : base(logger)
        {
        }
        
        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override ParseResult Parse(BasicMessage message)
        {
            try
            {
                var deserialised = JsonSerializer.Deserialize<TestDeserialisedMessage>(message.Body);
                return ParseResult.Success(deserialised.Key, deserialised);
            }
            catch (JsonException)
            {
                // If message cannot be deserialized, you can specify the acknowledgement behaviour for more granularity.
                return ParseResult.Fail(Acknowledgement.Ack);
            }
        }

        protected override async Task<Acknowledgement> ProcessAsync(TestDeserialisedMessage body)
        {
            // Process the message here.
            
            // Return the acknowledgement.
            return Acknowledgement.Ack;
        }
    }
}