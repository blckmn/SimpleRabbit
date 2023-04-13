using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;

namespace Subscriber.Service
{
    public record TestDeserialisedMessage(int Key, string Value);
    
    public class ParallelMessageHandler : ParallelMessageHandler<int, TestDeserialisedMessage>
    {
        public ParallelMessageHandler(ILogger<ParallelMessageHandler<int, TestDeserialisedMessage>> logger) : base(logger)
        {
        }
        
        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override bool TryDeserializeMessage(BasicMessage msg, out DeserializedMessage<int, TestDeserialisedMessage> value)
        {
            try
            {
                var deserialised = JsonSerializer.Deserialize<TestDeserialisedMessage>(msg.Body);
                value = DeserializedMessage<int, TestDeserialisedMessage>.Success(deserialised.Key, deserialised);
                return true;
            }
            catch (JsonException e)
            {
                // If message cannot be deserialized, you can specify the acknowledgement behaviour for more granularity.
                value = DeserializedMessage<int, TestDeserialisedMessage>.Fail(Acknowledgement.NackDeadLetter);
                return false;
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