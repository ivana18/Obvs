﻿
# Obvs: an observable microservice bus
## observable services, *obviously*

[![Join the chat at https://gitter.im/inter8ection/Obvs](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/inter8ection/Obvs?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![build](https://ci.appveyor.com/api/projects/status/hcv02eeulvfdr9de?svg=true)](https://ci.appveyor.com/project/inter8ection/obvs)

[![NuGet](https://img.shields.io/nuget/v/Obvs.svg)](https://www.nuget.org/packages/Obvs/)

### Features

* Obvs is just a library, not a framework - use as much or as little as you need.
* Leverage messaging and Reactive Extensions to quickly compose a system of decoupled microservices.
* Add new services and message contracts with a minimal amount of code and effort.
* Don't tie yourself to any one transport, migrate between transports with minimal code changes required
* Use a mix of transports and serialization formats, allowing you to pick what works for your services.
* Declare a new Obvs ServiceBus easily using the fluent code based configuration.
* Don't want to use Obvs message contract interfaces? Use the generic ServiceBus and supply your own.
* Standardize on messaging semantics throughout by wrapping integrations with external API's as custom endpoints.
* Don't distribute if you don't need to, Obvs ServiceBus includes a local in-memory bus.
* Use one of the many available serialization extensions, or even write your own.
* Easily debug and monitor your application using logging and performance counter extensions.

### Versions/Roadmap

* V5 - Next version, `System.Reactive 4.1`, supports `netstandard2.0` and `net46`
* V4 - Current version, `System.Reactive 3.1.1`, supports `netstandard1.6` and `net452` 

### More Details

* Convention based messaging over topics/queues/streams per service.
* Multiplexing of multiple message types over single topics/queues/streams.
* Dynamic creation of deserializers per type, auto-discovery of message contracts.
* Exceptions are caught and raised on an asynchronous error channel.

### Extensions

* Transports: ActiveMQ / RabbitMQ / NetMQ / AzureServiceBus / Kafka / EventStore
* Serialization: XML / JSON.Net / NetJson / ProtoBuf / MsgPack
* Logging: NLog / log4net
* Monitoring: Performance Counters / ElasticSearch
* Integrations: Slack

## Example

Define a root message type to identify messages as belonging to your service:

	public interface IMyServiceMessage : IMessage { }

Create command/event/request/response message types:

	public class MyCommand : IMyServiceMessage, ICommand { }

	public class MyEvent : IMyServiceMessage, IEvent { }

	public class MyRequest: IMyServiceMessage, IRequest { }
	
	public class MyResponse : IMyServiceMessage, IResponse { }

Create your service bus:

	IServiceBus serviceBus = ServiceBus.Configure()
        .WithActiveMQEndpoints<IMyServiceMessage>()
            .Named("MyService")
            .UsingQueueFor<ICommand>()
            .ConnectToBroker("tcp://localhost:61616")
            .SerializedAsJson()
            .AsClientAndServer()
        .Create();

Send commands:

	serviceBus.Commands.Subscribe(c => Console.WriteLine("Received a command!"));
	await serviceBus.SendAsync(new MyCommand());

Publish events:

	serviceBus.Events.Subscribe(e => Console.WriteLine("Received an event!"));
	await serviceBus.PublishAsync(new MyEvent());
	
Request/response:

	serviceBus.Requests
		  .OfType<MyRequest>()
		  .Subscribe(request => serviceBus.ReplyAsync(request, new MyResponse()));
	
	serviceBus.GetResponses(new MyRequest())
		  .OfType<MyResponse>()
		  .Take(1)
		  .Timeout(TimeSpan.FromSeconds(1))
		  .Subscribe(r => Console.WriteLine("Received a response!"), err => Console.WriteLine("Oh no!"));

Define custom endpoints that can wrap API calls or integrations with other systems:
	
	public class MyCustomEndpoint : IServiceEndpointClient
    	{
        	Type _serviceType = typeof(IMyCustomServiceMessage);

        	public IObservable<IEvent> Events
        	{
            		get
            		{
                		// subscribe to external MQ broker
            		}
        	}

        	public Task SendAsync(ICommand command)
        	{
            		// call external API
        	}

        	public IObservable<IResponse> GetResponses(IRequest request)
        	{
            		// call external API and wrap response in observable
        	}

        	public bool CanHandle(IMessage message)
        	{
            		return _serviceType.IsInstanceOfType(message);
        	}
    	}
		
	...

	IServiceBus serviceBus = ServiceBus.Configure()
          .WithActiveMQEndpoints<IMyServiceMessage>()
            .Named("MyService")
            .UsingQueueFor<ICommand>()
            .ConnectToBroker("tcp://localhost:61616")
            .SerializedAsJson()
            .AsClientAndServer()
	  .WithEndpoints(new MyCustomEndpoint())
        .Create();

