﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyCQRS.Core;
using EnjoyCQRS.Events;
using EnjoyCQRS.EventSource;
using EnjoyCQRS.EventSource.Exceptions;
using EnjoyCQRS.EventSource.Projections;
using EnjoyCQRS.EventSource.Snapshots;
using EnjoyCQRS.EventSource.Storage;
using EnjoyCQRS.UnitTests.Shared;
using EnjoyCQRS.MessageBus;
using EnjoyCQRS.UnitTests.Domain.Stubs;
using EnjoyCQRS.UnitTests.Domain.Stubs.Events;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnjoyCQRS.UnitTests.Storage
{
    [Trait("Unit", "Session")]
    public class SessionTests
    {
        private readonly Func<IEventStore, IEventPublisher, ISnapshotStrategy, EventsMetadataService, Session> _sessionFactory = (eventStore, eventPublisher, snapshotStrategy, eventsMetadataService) =>
        {
            var session = new Session(MockHelper.CreateLoggerFactory(MockHelper.GetMockLogger().Object), eventStore, eventPublisher, null, null, null, snapshotStrategy, eventsMetadataService);

            return session;
        };

        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly JsonTextSerializer _textSerializer;

        public SessionTests()
        {
            _eventPublisherMock = new Mock<IEventPublisher>();

            _textSerializer = new JsonTextSerializer();
        }

        
        [Fact]
        public void Cannot_pass_null_instance_of_LoggerFactory()
        {
            var eventPublisher = Mock.Of<IEventPublisher>();
            var eventStore = Mock.Of<IEventStore>();
            var projectionProviderScanner = Mock.Of<IProjectionProviderScanner>();
            var eventUpdateManager = Mock.Of<IEventUpdateManager>();
            var metadataProviders = Mock.Of<IEnumerable<IMetadataProvider>>();

            Action act = () => new Session(null, eventStore, eventPublisher, projectionProviderScanner, eventUpdateManager, metadataProviders);

            act.ShouldThrowExactly<ArgumentNullException>();
        }
        
        [Fact]
        public void Cannot_pass_null_instance_of_EventStore()
        {
            var projectionProviderScanner = Mock.Of<IProjectionProviderScanner>();
            var eventPublisher = Mock.Of<IEventPublisher>();
            var eventUpdateManager = Mock.Of<IEventUpdateManager>();
            var metadataProviders = Mock.Of<IEnumerable<IMetadataProvider>>();

            Action act = () => new Session(MockHelper.CreateLoggerFactory(MockHelper.GetMockLogger().Object), null, eventPublisher, projectionProviderScanner, eventUpdateManager, metadataProviders);

            act.ShouldThrowExactly<ArgumentNullException>();
        }
        
        [Fact]
        public void Cannot_pass_null_instance_of_EventPublisher()
        {
            var eventStore = Mock.Of<IEventStore>();
            var projectionProviderScanner = Mock.Of<IProjectionProviderScanner>();
            var eventUpdateManager = Mock.Of<IEventUpdateManager>();
            var metadataProviders = Mock.Of<IEnumerable<IMetadataProvider>>();

            Action act = () => new Session(MockHelper.CreateLoggerFactory(MockHelper.GetMockLogger().Object), eventStore, null, projectionProviderScanner, eventUpdateManager, metadataProviders);

            act.ShouldThrowExactly<ArgumentNullException>();
        }
        
        [Fact]
        public async Task Should_throws_exception_When_aggregate_version_is_wrong()
        {
            var eventStore = new InMemoryEventStore();

            // create first session instance
            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, null, null);

            var stubAggregate1 = StubAggregate.Create("Walter White");

            await session.AddAsync(stubAggregate1).ConfigureAwait(false);

            await session.SaveChangesAsync().ConfigureAwait(false);

            stubAggregate1.ChangeName("Going to Version 2. Expected Version 1.");

            // create second session instance to getting clear tracking
            session = _sessionFactory(eventStore, _eventPublisherMock.Object, null, null);

            var stubAggregate2 = await session.GetByIdAsync<StubAggregate>(stubAggregate1.Id).ConfigureAwait(false);

            stubAggregate2.ChangeName("Going to Version 2");

            await session.AddAsync(stubAggregate2).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            Func<Task> wrongVersion = async () => await session.AddAsync(stubAggregate1);

            wrongVersion.ShouldThrowExactly<ExpectedVersionException<StubAggregate>>().And.Aggregate.Should().Be(stubAggregate1);
        }
        
        [Fact]
        public async Task Should_retrieve_the_aggregate_from_tracking()
        {
            var eventStore = new InMemoryEventStore();

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, null, null);

            var stubAggregate1 = StubAggregate.Create("Walter White");

            await session.AddAsync(stubAggregate1).ConfigureAwait(false);

            await session.SaveChangesAsync().ConfigureAwait(false);

            stubAggregate1.ChangeName("Changes");

            var stubAggregate2 = await session.GetByIdAsync<StubAggregate>(stubAggregate1.Id).ConfigureAwait(false);

            stubAggregate2.ChangeName("More changes");

            stubAggregate1.Should().BeSameAs(stubAggregate2);
        }
        
        [Fact]
        public async Task Should_publish_in_correct_order()
        {
            var events = new List<IDomainEvent>();

            var eventStore = new InMemoryEventStore();

            _eventPublisherMock.Setup(e => e.PublishAsync(It.IsAny<IEnumerable<IDomainEvent>>())).Callback<IEnumerable<IDomainEvent>>(evts => events.AddRange(evts)).Returns(Task.CompletedTask);

            _eventPublisherMock.Setup(e => e.CommitAsync()).Returns(Task.CompletedTask);

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, null, null);

            var stubAggregate1 = StubAggregate.Create("Walter White");
            var stubAggregate2 = StubAggregate.Create("Heinsenberg");

            stubAggregate1.ChangeName("Saul Goodman");

            stubAggregate2.Relationship(stubAggregate1.Id);

            stubAggregate1.ChangeName("Jesse Pinkman");

            await session.AddAsync(stubAggregate1).ConfigureAwait(false);
            await session.AddAsync(stubAggregate2).ConfigureAwait(false);

            await session.SaveChangesAsync().ConfigureAwait(false);

            events[0].Should().BeOfType<StubAggregateCreatedEvent>().Which.AggregateId.Should().Be(stubAggregate1.Id);
            events[0].Should().BeOfType<StubAggregateCreatedEvent>().Which.Name.Should().Be("Walter White");

            events[1].Should().BeOfType<StubAggregateCreatedEvent>().Which.AggregateId.Should().Be(stubAggregate2.Id);
            events[1].Should().BeOfType<StubAggregateCreatedEvent>().Which.Name.Should().Be("Heinsenberg");

            events[2].Should().BeOfType<NameChangedEvent>().Which.AggregateId.Should().Be(stubAggregate1.Id);
            events[2].Should().BeOfType<NameChangedEvent>().Which.Name.Should().Be("Saul Goodman");

            events[3].Should().BeOfType<StubAggregateRelatedEvent>().Which.AggregateId.Should().Be(stubAggregate2.Id);
            events[3].Should().BeOfType<StubAggregateRelatedEvent>().Which.StubAggregateId.Should().Be(stubAggregate1.Id);

            events[4].Should().BeOfType<NameChangedEvent>().Which.AggregateId.Should().Be(stubAggregate1.Id);
            events[4].Should().BeOfType<NameChangedEvent>().Which.Name.Should().Be("Jesse Pinkman");
        }
        
        [Fact]
        public async Task When_call_SaveChanges_Should_store_the_snapshot()
        {
            // Arrange

            var snapshotStrategy = CreateSnapshotStrategy();

            var eventStore = new StubEventStore();
            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate = StubSnapshotAggregate.Create("Snap");

            stubAggregate.AddEntity("Child 1");
            stubAggregate.AddEntity("Child 2");

            await session.AddAsync(stubAggregate).ConfigureAwait(false);

            // Act

            await session.SaveChangesAsync().ConfigureAwait(false);

            // Assert

            eventStore.SaveSnapshotMethodCalled.Should().BeTrue();

            var commitedSnapshot = eventStore.Snapshots.First(e => e.AggregateId == stubAggregate.Id);

            commitedSnapshot.Should().NotBeNull();
            
            var snapshotClrType = commitedSnapshot.SerializedMetadata.GetValue(MetadataKeys.SnapshotClrType, value => value.ToString());

            Type.GetType(snapshotClrType).Name.Should().Be(typeof(StubSnapshotAggregateSnapshot).Name);

            var snapshot = (StubSnapshotAggregateSnapshot) commitedSnapshot.SerializedData;

            snapshot.Name.Should().Be(stubAggregate.Name);
            snapshot.SimpleEntities.Count.Should().Be(stubAggregate.Entities.Count);
        }
        
        [Fact]
        public async Task Should_restore_aggregate_using_snapshot()
        {
            var snapshotStrategy = CreateSnapshotStrategy();

            var eventStore = new StubEventStore();

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate = StubSnapshotAggregate.Create("Snap");

            stubAggregate.AddEntity("Child 1");
            stubAggregate.AddEntity("Child 2");

            await session.AddAsync(stubAggregate).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var aggregate = await session.GetByIdAsync<StubSnapshotAggregate>(stubAggregate.Id).ConfigureAwait(false);

            eventStore.GetSnapshotMethodCalled.Should().BeTrue();

            aggregate.Version.Should().Be(3);
            aggregate.Id.Should().Be(stubAggregate.Id);
        }

        [Fact]
        public async Task When_not_exists_snapshot_yet_Then_aggregate_should_be_constructed_using_your_events()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            var eventStore = new StubEventStore();

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate = StubSnapshotAggregate.Create("Snap");

            stubAggregate.AddEntity("Child 1");
            stubAggregate.AddEntity("Child 2");

            await session.AddAsync(stubAggregate).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var aggregate = await session.GetByIdAsync<StubSnapshotAggregate>(stubAggregate.Id).ConfigureAwait(false);

            aggregate.Name.Should().Be(stubAggregate.Name);
            aggregate.Entities.Count.Should().Be(stubAggregate.Entities.Count);
        }

        [Fact]
        public async Task Getting_snapshot_and_forward_events()
        {
            var snapshotStrategy = CreateSnapshotStrategy();

            var eventStore = new StubEventStore();

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate = StubSnapshotAggregate.Create("Snap");

            await session.AddAsync(stubAggregate).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false); // Version 1

            stubAggregate.ChangeName("Renamed");
            stubAggregate.ChangeName("Renamed again");

            // dont make snapshot
            snapshotStrategy = CreateSnapshotStrategy(false);

            session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            await session.AddAsync(stubAggregate).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false); // Version 3

            var stubAggregateFromSnapshot = await session.GetByIdAsync<StubSnapshotAggregate>(stubAggregate.Id).ConfigureAwait(false);

            stubAggregateFromSnapshot.Version.Should().Be(3);
        }

        [Fact]
        public Task Should_throws_exception_When_aggregate_was_not_found()
        {
            var snapshotStrategy = CreateSnapshotStrategy();

            var eventStore = new StubEventStore();

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var newId = Guid.NewGuid();

            Func<Task> act = async () =>
            {
                await session.GetByIdAsync<StubAggregate>(newId).ConfigureAwait(false);
            };

            var assertion = act.ShouldThrowExactly<AggregateNotFoundException>();

            assertion.Which.AggregateName.Should().Be(typeof(StubAggregate).Name);
            assertion.Which.AggregateId.Should().Be(newId);

            return Task.CompletedTask;
        }

        [Fact]
        public async Task Should_internal_rollback_When_exception_was_throw_on_saving()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            var eventStoreMock = new Mock<IEventStore>();
            eventStoreMock.Setup(e => e.SaveAsync(It.IsAny<IEnumerable<ISerializedEvent>>()))
                .Callback(DoThrowExcetion)
                .Returns(Task.CompletedTask);

            var session = _sessionFactory(eventStoreMock.Object, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate = StubAggregate.Create("Guilty");

            await session.AddAsync(stubAggregate).ConfigureAwait(false);

            try
            {
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                eventStoreMock.Verify(e => e.Rollback(), Times.Once);
                _eventPublisherMock.Verify(e => e.Rollback(), Times.Once);
            }
        }

        [Fact]
        public async Task Should_manual_rollback_When_exception_was_throw_on_saving()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            var eventStoreMock = new Mock<IEventStore>();
            eventStoreMock.Setup(e => e.SaveAsync(It.IsAny<IEnumerable<ISerializedEvent>>()))
                .Callback(DoThrowExcetion)
                .Returns(Task.CompletedTask);

            eventStoreMock.Setup(e => e.BeginTransaction());
            eventStoreMock.Setup(e => e.Rollback());

            var session = _sessionFactory(eventStoreMock.Object, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate = StubAggregate.Create("Guilty");

            session.BeginTransaction();

            eventStoreMock.Verify(e => e.BeginTransaction(), Times.Once);

            await session.AddAsync(stubAggregate).ConfigureAwait(false);

            try
            {
                await session.SaveChangesAsync().ConfigureAwait(false);
            }

            catch (Exception)
            {
                session.Rollback();
            }

            eventStoreMock.Verify(e => e.Rollback(), Times.Once);
            _eventPublisherMock.Verify(e => e.Rollback(), Times.Once);
        }

        [Fact]
        public void Cannot_BeginTransaction_twice()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            var eventStoreMock = new Mock<IEventStore>();
            eventStoreMock.SetupAllProperties();

            var session = _sessionFactory(eventStoreMock.Object, _eventPublisherMock.Object, snapshotStrategy, null);

            session.BeginTransaction();

            Action act = () => session.BeginTransaction();

            act.ShouldThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task When_occur_error_on_publishing_Then_rollback_should_be_called()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            _eventPublisherMock.Setup(e => e.PublishAsync(It.IsAny<IDomainEvent>()))
                .Callback(DoThrowExcetion)
                .Returns(Task.CompletedTask);

            _eventPublisherMock.Setup(e => e.PublishAsync(It.IsAny<IEnumerable<IDomainEvent>>()))
                .Callback(DoThrowExcetion)
                .Returns(Task.CompletedTask);

            var eventStoreMock = new Mock<IEventStore>();
            eventStoreMock.SetupAllProperties();

            var session = _sessionFactory(eventStoreMock.Object, _eventPublisherMock.Object, snapshotStrategy, null);

            await session.AddAsync(StubAggregate.Create("Test"));

            try
            {
                await session.SaveChangesAsync();
            }
            catch (Exception)
            {
                eventStoreMock.Verify(e => e.Rollback(), Times.Once);
                _eventPublisherMock.Verify(e => e.Rollback(), Times.Once);
            }
        }

        [Fact]
        public async Task When_occur_error_on_Save_in_EventStore_Then_rollback_should_be_called()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            var eventStoreMock = new Mock<IEventStore>();
            eventStoreMock.SetupAllProperties();
            eventStoreMock.Setup(e => e.SaveAsync(It.IsAny<IEnumerable<ISerializedEvent>>()))
                .Callback(DoThrowExcetion)
                .Returns(Task.CompletedTask);

            var session = _sessionFactory(eventStoreMock.Object, _eventPublisherMock.Object, snapshotStrategy, null);

            await session.AddAsync(StubAggregate.Create("Test")).ConfigureAwait(false);

            try
            {
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                eventStoreMock.Verify(e => e.Rollback(), Times.Once);
                _eventPublisherMock.Verify(e => e.Rollback(), Times.Once);
            }
        }

        [Fact]
        public async Task When_Save_events_Then_aggregate_projection_should_be_created()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            var eventStore = new StubEventStore();

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate1 = StubAggregate.Create("Walter White");

            stubAggregate1.ChangeName("Saul Goodman");
            stubAggregate1.ChangeName("Jesse Pinkman");

            await session.AddAsync(stubAggregate1).ConfigureAwait(false);

            await session.SaveChangesAsync().ConfigureAwait(false);

            var projectionKey = new InMemoryEventStore.ProjectionKey(stubAggregate1.Id, typeof(StubAggregateProjection).Name);

            eventStore.Projections.ContainsKey(projectionKey).Should().BeTrue();

            var projection = (StubAggregateProjection) eventStore.Projections[projectionKey];

            projection.Id.Should().Be(stubAggregate1.Id);
            projection.Name.Should().Be(stubAggregate1.Name);
        }

        [Fact]
        public async Task When_Save_events_Then_aggregate_projection_should_be_updated()
        {
            var snapshotStrategy = CreateSnapshotStrategy(false);

            var eventStore = new StubEventStore();

            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, null);

            var stubAggregate1 = StubAggregate.Create("Walter White");

            await session.AddAsync(stubAggregate1).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            stubAggregate1 = await session.GetByIdAsync<StubAggregate>(stubAggregate1.Id).ConfigureAwait(false);

            stubAggregate1.ChangeName("Jesse Pinkman");

            await session.SaveChangesAsync().ConfigureAwait(false);

            eventStore.Projections.Count.Should().Be(1);

            var projectionKey = new InMemoryEventStore.ProjectionKey(stubAggregate1.Id, typeof(StubAggregateProjection).Name);
            eventStore.Projections.ContainsKey(projectionKey).Should().BeTrue();

            var projection = (StubAggregateProjection) eventStore.Projections[projectionKey];

            projection.Id.Should().Be(stubAggregate1.Id);
            projection.Name.Should().Be(stubAggregate1.Name);
        }

        [Fact]
        public async Task Access_metadata_from_all_emitted_events()
        {
            // Arrange

            var snapshotStrategy = CreateSnapshotStrategy();

            var eventsMetadataService = new EventsMetadataService();

            var eventStore = new StubEventStore();
            var session = _sessionFactory(eventStore, _eventPublisherMock.Object, snapshotStrategy, eventsMetadataService);

            var stubAggregate = StubSnapshotAggregate.Create("Snap");

            stubAggregate.AddEntity("Child 1");
            stubAggregate.AddEntity("Child 2");

            await session.AddAsync(stubAggregate).ConfigureAwait(false);

            // Act

            await session.SaveChangesAsync().ConfigureAwait(false);

            // Assert

            var eventsWithMetadata = eventsMetadataService.GetEvents().ToList();

            eventsWithMetadata.Count().Should().Be(3);

            eventsWithMetadata[0].Metadata.GetValue(MetadataKeys.EventName).Should().Be("StubCreated");
            eventsWithMetadata[1].Metadata.GetValue(MetadataKeys.EventName).Should().Be(nameof(ChildCreatedEvent));
            eventsWithMetadata[2].Metadata.GetValue(MetadataKeys.EventName).Should().Be(nameof(ChildCreatedEvent));
        }

        private static ISnapshotStrategy CreateSnapshotStrategy(bool makeSnapshot = true)
        {
            var snapshotStrategyMock = new Mock<ISnapshotStrategy>();
            snapshotStrategyMock.Setup(e => e.CheckSnapshotSupport(It.IsAny<Type>())).Returns(true);
            snapshotStrategyMock.Setup(e => e.ShouldMakeSnapshot(It.IsAny<IAggregate>())).Returns(makeSnapshot);

            return snapshotStrategyMock.Object;
        }
        
        private static void DoThrowExcetion()
        {
            throw new Exception("Sorry, this is my fault.");
        }
    }
}