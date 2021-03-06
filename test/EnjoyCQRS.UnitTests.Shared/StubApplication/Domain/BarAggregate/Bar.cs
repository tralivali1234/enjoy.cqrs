using System;
using EnjoyCQRS.EventSource;
using System.Collections.Generic;
using EnjoyCQRS.Attributes;
using EnjoyCQRS.UnitTests.Shared.StubApplication.Domain.BarAggregate.Projections;

namespace EnjoyCQRS.UnitTests.Shared.StubApplication.Domain.BarAggregate
{
    [ProjectionProvider(typeof(BarOnlyIdProjectionProvider))]
    [ProjectionProvider(typeof(BarWithoutMessagesProjectionProvider))]
    [ProjectionProvider(typeof(BarProjectionProvider))]
    [ProjectionProvider(typeof(BarProjectionProvider))]
    public class Bar : Aggregate
    {
        private List<string> _messages = new List<string>();

        public string LastText { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyList<string> Messages
        {
            get { return _messages.AsReadOnly(); }

            private set { _messages = new List<string>(value); }
        }
        
        public Bar()
        {
        }

        private Bar(Guid id)
        {
            Emit(new BarCreated(id));
        }

        public static Bar Create(Guid id)
        {
            return new Bar(id);
        }

        public void Speak(string text)
        {
            Emit(new SpokeSomething(text));
        }

        protected override void RegisterEvents()
        {
            SubscribeTo<BarCreated>(e =>
            {
                Id = e.AggregateId;
                UpdatedAt = DateTime.Now;
            });

            SubscribeTo<SpokeSomething>(e =>
            {
                LastText = e.Text;
                UpdatedAt = DateTime.Now;

                _messages.Add(e.Text);
            });
        }
    }
}