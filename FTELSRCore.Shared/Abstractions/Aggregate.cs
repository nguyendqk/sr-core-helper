using System.ComponentModel.DataAnnotations.Schema;

namespace FTELSRCore.Abstractions
{
    public interface IAggregate
    {
        List<IDomainEvent> DomainEvents { get; }

        IDomainEvent[] ClearDomainEvents();
    }

    public abstract class Aggregate : IAggregate
    {
        [NotMapped]
        private readonly List<IDomainEvent> _domainEvents = [];

        [NotMapped]
        public List<IDomainEvent> DomainEvents => _domainEvents;

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void AddRangeDomainEvent(List<IDomainEvent> domainEvents)
        {
            _domainEvents.AddRange(domainEvents);
        }

        public IDomainEvent[] ClearDomainEvents()
        {
            IDomainEvent[] dequeuedEvents = [.. _domainEvents];

            _domainEvents.Clear();

            return dequeuedEvents;
        }
    }
}