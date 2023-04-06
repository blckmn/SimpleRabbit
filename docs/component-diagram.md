a grouping of related functionality encapsulated behind a well-defined interface. If you're using a language like Java or C#, the simplest way to think of a component is that it's a collection of implementation classes behind an interface.

```mermaid
    C4Component
    title Component diagram for Simple Rabbit - .NET Library for interacting with RabbitMQ 

    Container(rabbitmq, "RabbitMQ", "RabbitMQ Server", "Message broker system")
 
    Container_Boundary(simpleRabbit, "SimpleRabbit.NetCore") {
        Component(iMessageHandler, "IMessageHandler", "Interface", "Contract for handlers to use to process messages based on consumer tag")
        Component(basicRabbitService, "BasicRabbitService", "Class", "Wrapper over the Rabbit connection factory. Provides the basics to interact with a rabbit instance")
        Component(publishService, "PublishService", "Class", "Enables message publishing to exchanges")
        Component(queueService, "QueueService", "Class", "Enables queue subscriptions and the handling of messages")
        
        Rel(publishService, basicRabbitService, "Extends")
        Rel(queueService, basicRabbitService, "Extends")
    }

```