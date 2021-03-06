﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Baseline.Dates;
using Jasper.Bus.Logging;
using Jasper.Bus.Runtime;
using Jasper.Bus.Transports;
using Jasper.Bus.Transports.Configuration;
using Jasper.Bus.WorkerQueues;
using Jasper.Marten.Persistence;
using Jasper.Marten.Persistence.Resiliency;
using Jasper.Marten.Tests.Setup;
using Jasper.Testing.Bus;
using Marten;
using Marten.Schema;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Jasper.Marten.Tests.Persistence
{
    public class MartenCallbackTests : IDisposable
    {
        private JasperRuntime theRuntime;
        private IDocumentStore theStore;
        private Envelope theEnvelope;
        private MartenCallback theCallback;
        private MartenRetries theRetries;

        public MartenCallbackTests()
        {
            theRuntime = JasperRuntime.For(_ =>
            {
                _.MartenConnectionStringIs(ConnectionSource.ConnectionString);

                _.ConfigureMarten(x =>
                {
                    x.Storage.Add<PostgresqlEnvelopeStorage>();
                    x.PLV8Enabled = false;
                });
            });

            theStore = theRuntime.Get<IDocumentStore>();

            theStore.Advanced.Clean.CompletelyRemoveAll();
            theStore.Schema.ApplyAllConfiguredChangesToDatabase();

            theEnvelope = ObjectMother.Envelope();
            theEnvelope.Status = TransportConstants.Incoming;

            var marker = new EnvelopeTables(new BusSettings(), new StoreOptions());

            using (var session = theStore.OpenSession())
            {
                session.StoreIncoming(marker, theEnvelope);
                session.SaveChanges();
            }


            var logger = CompositeTransportLogger.Empty();
            theRetries = new MartenRetries(theStore, marker, logger, new BusSettings());

            theCallback = new MartenCallback(theEnvelope, Substitute.For<IWorkerQueue>(), theStore, marker, theRetries, logger);
        }

        public void Dispose()
        {
            theRuntime?.Dispose();
        }

        [Fact]
        public async Task mark_complete_deletes_the_envelope()
        {
            await theCallback.MarkComplete();

            theRetries.IncomingDeleted.WaitOne(500);

            using (var session = theStore.QuerySession())
            {
                var persisted = session.AllIncomingEnvelopes().FirstOrDefault(x => x.Id == theEnvelope.Id);


                persisted.ShouldBeNull();
            }


        }

        [Fact]
        public async Task move_to_errors_persists_the_error_report()
        {
            await theCallback.MoveToErrors(theEnvelope, new Exception("Boom!"));

            theRetries.ErrorReportLogged.WaitOne(500);

            using (var session = theStore.QuerySession())
            {
                var persisted = session.AllIncomingEnvelopes().FirstOrDefault(x => x.Id == theEnvelope.Id);


                persisted.ShouldBeNull();


                var report = await session.LoadAsync<ErrorReport>(theEnvelope.Id);

                report.ExceptionMessage.ShouldBe("Boom!");
            }


        }

        [Fact]
        public async Task can_reload_the_error_report()
        {
            await theCallback.MoveToErrors(theEnvelope, new Exception("Boom!"));

            theRetries.ErrorReportLogged.WaitOne(500);

            var persistence = theRuntime.Get<MartenBackedMessagePersistence>();

            var report = await persistence.LoadDeadLetterEnvelope(theEnvelope.Id);

            report.ExceptionMessage.ShouldBe("Boom!");
        }

        [Fact]
        public async Task requeue()
        {
            await theCallback.Requeue(theEnvelope);

            using (var session = theStore.QuerySession())
            {
                var persisted = session.AllIncomingEnvelopes().FirstOrDefault(x => x.Id == theEnvelope.Id);

                persisted.Attempts.ShouldBe(1);
            }
        }

        [Fact]
        public async Task move_to_delayed_until()
        {
            var time = DateTime.Today.ToUniversalTime().AddDays(1);

            await theCallback.MoveToDelayedUntil(time, theEnvelope);

            theRetries.Scheduled.WaitOne(1.Seconds());

            using (var session = theStore.QuerySession())
            {
                var persisted = session.AllIncomingEnvelopes().FirstOrDefault(x => x.Id == theEnvelope.Id);
                persisted.Status.ShouldBe(TransportConstants.Scheduled);
                persisted.OwnerId.ShouldBe(TransportConstants.AnyNode);
                persisted.ExecutionTime.ShouldBe(time);
            }
        }
    }
}
