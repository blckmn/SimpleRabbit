using FluentAssertions;
using NUnit.Framework;
using RabbitMQ.Client;
using SimpleRabbit.NetCore.Tests.implementations;
using System;
using System.Collections.Generic;
using System.Security.Authentication;

namespace SimpleRabbit.NetCore.Tests
{
    [TestFixture]
    public class BasicRabbitServiceTests
    {

        [Test]
        public void SingletonAttributes()
        {
            var service = new ExposedRabbitService(ExposedRabbitService.validConfig);

            var factory1 = service.ExposedFactory;
            var connection1 = service.ExposedConnection;
            var channel1 = service.ExposedChannel;


            var factory2 = service.ExposedFactory;
            var connection2 = service.ExposedConnection;
            var channel2 = service.ExposedChannel;

            service.Dispose();

            factory1.Should().BeSameAs(factory2);
            connection1.Should().BeSameAs(connection2);
            channel1.Should().BeSameAs(channel2);

        }

        [Test]
        public void ClearConnection()
        {
            var service = new ExposedRabbitService(ExposedRabbitService.validConfig);
            var factory1 = service.ExposedFactory;
            var connection1 = service.ExposedConnection;
            var channel1 = service.ExposedChannel;

            service.ClearConnection();
            var factory2 = service.ExposedFactory;
            var connection2 = service.ExposedConnection;
            var channel2 = service.ExposedChannel;

            connection1.IsOpen.Should().BeFalse();
            channel1.IsClosed.Should().BeTrue();

            factory1.Should().BeSameAs(factory2);
            connection1.Should().NotBeSameAs(connection2);
            channel1.Should().NotBeSameAs(channel2);

            service.Dispose();
        }

        [Test]
        public void CloseConnection()
        {
            var service = new ExposedRabbitService(ExposedRabbitService.validConfig);
            var factory1 = service.ExposedFactory;
            var connection1 = service.ExposedConnection;
            var channel1 = service.ExposedChannel;

            service.Close();
            var factory2 = service.ExposedFactory;
            var connection2 = service.ExposedConnection;
            var channel2 = service.ExposedChannel;

            connection1.IsOpen.Should().BeFalse();
            channel1.IsClosed.Should().BeTrue();

            factory1.Should().NotBeSameAs(factory2);
            connection1.Should().NotBeSameAs(connection2);
            channel1.Should().NotBeSameAs(channel2);

            service.Dispose();
        }

        [Test]
        public void DefaultFactoryValues()
        {
            var service = new ExposedRabbitService(ExposedRabbitService.validConfig);
            var factory1 = service.ExposedFactory;

            factory1.UserName.Should().Be(ExposedRabbitService.validConfig.Username);
            factory1.Password.Should().Be(ExposedRabbitService.validConfig.Password);

            factory1.VirtualHost.Should().Be(ConnectionFactory.DefaultVHost);
            factory1.AutomaticRecoveryEnabled.Should().BeTrue();
            factory1.NetworkRecoveryInterval.Should().Be(TimeSpan.FromSeconds(10));
            factory1.TopologyRecoveryEnabled.Should().BeTrue();
            factory1.RequestedHeartbeat.Should().Be(5);

            service.Dispose();
        }

        [Test]
        public void OverrideFactoryValues()
        {
            var config = new RabbitConfiguration
            {
                Username = "guest",
                Password = "guest",
                Hostnames = new List<string> { "localhost" },
                VirtualHost = "/ex",
                AutomaticRecoveryEnabled = false,
                NetworkRecoveryIntervalInSeconds = 5,
                TopologyRecoveryEnabled = false,
                RequestedHeartBeat = 20
            };
            var service = new ExposedRabbitService(config);
            var factory1 = service.ExposedFactory;

            factory1.UserName.Should().Be(config.Username);
            factory1.Password.Should().Be(config.Password);

            factory1.VirtualHost.Should().Be("/ex");
            factory1.AutomaticRecoveryEnabled.Should().BeFalse();
            factory1.NetworkRecoveryInterval.Should().Be(TimeSpan.FromSeconds(5));
            factory1.TopologyRecoveryEnabled.Should().BeFalse();
            factory1.RequestedHeartbeat.Should().Be(20);

            service.Dispose();
        }

        [Test]
        public void NoUsernameProvided()
        {
            var config = new RabbitConfiguration
            {
                Password = "guest",
                Hostnames = new List<string> { "localhost" }
            };
            var service = new ExposedRabbitService(config);
            service.Invoking(y => y.ExposedFactory).Should().Throw<InvalidCredentialException>();

            service.Dispose();
        }

        [Test]
        public void NoPasswordProvided()
        {
            var config = new RabbitConfiguration
            {
                Username = "guest",
                Hostnames = new List<string> { "localhost" }
            };
            var service = new ExposedRabbitService(config);
            service.Invoking(y => y.ExposedFactory).Should().Throw<InvalidCredentialException>();

            service.Dispose();
        }

        [Test]
        public void NoHostnamesProvided()
        {
            var config = new RabbitConfiguration
            {
                Password = "guest",
                Username = "guest",
            };
            var service = new ExposedRabbitService(config);
            service.Invoking(y => y.ExposedFactory).Should().Throw<ArgumentNullException>();

            service.Dispose();
        }

        [Test]
        public void EmptyHostnamesProvided()
        {
            var config = new RabbitConfiguration
            {
                Password = "guest",
                Username = "guest",
                Hostnames = new List<string>()
            };
            var service = new ExposedRabbitService(config);
            service.Invoking(y => y.ExposedFactory).Should().Throw<ArgumentNullException>();

            service.Dispose();
        }
    }






}
