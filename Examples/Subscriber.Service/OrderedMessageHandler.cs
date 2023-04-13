using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;

namespace Subscriber.Service
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

        protected override DeserializedMessage TryDeserializeMessage(BasicMessage msg)
        {
            try
            {
                var deserialised = JsonSerializer.Deserialize<TestDeserialisedMessage>(msg.Body);
                return DeserializedMessage.Success(deserialised.Key, deserialised);
            }
            catch (JsonException e)
            {
                return DeserializedMessage.Fail(Acknowledgement.Ack);
                // If message cannot be deserialized, you can specify the acknowledgement behaviour for more granularity.
            }
        }

        protected override async Task<Acknowledgement> ProcessAsync(TestDeserialisedMessage item)
        {
            // Process the message here.
            
            // Return the acknowledgement.
            return Acknowledgement.Ack;
        }
    }
}