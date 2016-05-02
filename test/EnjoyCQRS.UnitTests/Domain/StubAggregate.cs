﻿using System;
using EnjoyCQRS.EventSource;
using EnjoyCQRS.UnitTests.Domain.Events;

namespace EnjoyCQRS.UnitTests.Domain
{
    public class StubAggregate : Aggregate
    {
        public string Name { get; private set; }

        public StubAggregate()
        {
            On<SomeEvent>(x =>  { Name = x.Name; });
            On<TestAggregateCreatedEvent>(x => { Id = x.AggregateId; });
        }

        private StubAggregate(Guid newGuid) : this()
        {
            Raise(new TestAggregateCreatedEvent(newGuid));
        }

        public static StubAggregate Create()
        {
            return new StubAggregate(Guid.NewGuid());
        }
        
        public void DoSomething(string name)
        {
            Raise(new SomeEvent(Id, name));
        }

        public void DoSomethingWithoutEvent()
        {
            Raise(new NotRegisteredEvent(Id));
        }
    }
}