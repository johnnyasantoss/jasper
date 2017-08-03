﻿using System.Linq;
using Jasper.Bus.Runtime.Routing;
using Jasper.Testing.Bus.Runtime;
using Shouldly;
using StructureMap.TypeRules;
using TestMessages;
using Xunit;

namespace Jasper.Testing.Bus.Bootstrapping
{
    public class configuring_channels : BootstrappingContext
    {
        [Fact]
        public void listen_for_messages_on_a_channel_positive()
        {
            theRegistry.Channels.ListenForMessagesFrom(Uri1);

            // Send-only channel
            theRegistry.Messaging.Send<Message1>().To(Uri2);

            theChannels[Uri1].Incoming.ShouldBeTrue();
            theChannels[Uri2].Incoming.ShouldBeFalse();
        }

        [Fact]
        public void place_a_specific_type_routing_rule_on_a_channel()
        {
            theRegistry.Messaging.Send<Message1>().To(Uri2);

            theChannels[Uri2].Rules.Single().ShouldBeOfType<SingleTypeRoutingRule<Message1>>();
        }

        [Fact]
        public void configure_messages_in_namespace()
        {
            theRegistry.Messaging.SendFromNamespace("Foo")
                .To(Uri1);

            theRegistry.Messaging.SendFromNamespaceContaining<Message1>()
                .To(Uri1);

            theChannels[Uri1].Rules.OfType<NamespaceRule>().Select(x => x.Namespace)
                .ShouldHaveTheSameElementsAs("Foo", typeof(Message1).Namespace);
        }

        [Fact]
        public void configure_assembly_routing_rules()
        {
            theRegistry.Messaging.SendFromAssembly(typeof(NewUser).GetAssembly())
                .To(Uri1);

            theRegistry.Messaging.SendFromAssemblyContaining<Message1>()
                .To(Uri2);

            theChannels[Uri1].Rules.Single()
                .ShouldBeOfType<AssemblyRule>()
                .Assembly.ShouldBe(typeof(NewUser).GetAssembly());

            theChannels[Uri2].Rules.Single()
                .ShouldBeOfType<AssemblyRule>()
                .Assembly.ShouldBe(typeof(Message1).GetAssembly());
        }

    }
}
