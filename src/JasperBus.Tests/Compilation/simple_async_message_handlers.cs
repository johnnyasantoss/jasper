﻿using System;
using System.Threading.Tasks;
using JasperBus.Runtime;
using JasperBus.Runtime.Invocation;
using JasperBus.Tests.Runtime;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JasperBus.Tests.Compilation
{
    public class simple_async_message_handlers : CompilationContext<AsyncHandler>
    {
        private ITestOutputHelper _output;

        public simple_async_message_handlers(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void can_compile_all()
        {
            AllHandlersCompileSuccessfully();
        }

        [Fact]
        public async Task execute_the_simplest_possible_static_chain()
        {
            var message = new Message1();
            await Execute(message);

            AsyncHandler.LastMessage1.ShouldBeSameAs(message);
        }

        [Fact]
        public async Task execute_the_simplest_possible_instance_chain()
        {
            var message = new Message2();
            await Execute(message);

            AsyncHandler.LastMessage2.ShouldBeSameAs(message);
        }

        [Fact]
        public async Task can_pass_in_the_envelope()
        {
            var message = new Message3();
            await Execute(message);

            AsyncHandler.LastEnvelope.ShouldBeSameAs(theEnvelope);
        }

        [Fact]
        public async Task can_pass_in_the_invocation_context()
        {
            var message = new Message4();
            var context = await Execute(message);

            AsyncHandler.LastContext.ShouldBeSameAs(context);
        }
    }

    public class AsyncHandler
    {
        public static Message1 LastMessage1;
        public static Message2 LastMessage2;
        public static Envelope LastEnvelope;
        public static IInvocationContext LastContext;

        public static Task Simple1(Message1 message)
        {
            LastMessage1 = message;
            return Task.CompletedTask;
        }

        public Task Simple2(Message2 message)
        {
            LastMessage2 = message;
            return Task.CompletedTask;
        }

        public Task Simple3(Message3 message, Envelope envelope)
        {
            LastEnvelope = envelope;
            return Task.CompletedTask;
        }

        public static Task Simple4(Message4 message, IInvocationContext context)
        {
            LastContext = context;
            return Task.CompletedTask;
        }
    }



}