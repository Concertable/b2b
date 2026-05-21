namespace Concertable.User.Domain.Events;

public record UserCreatedDomainEvent(UserEntity User) : IDomainEvent;
