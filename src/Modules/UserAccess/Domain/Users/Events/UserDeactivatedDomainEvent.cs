using CompanyName.MyMeetings.BuildingBlocks.Domain;

namespace CompanyName.MyMeetings.Modules.UserAccess.Domain.Users.Events
{
    public class UserDeactivatedDomainEvent : DomainEventBase
    {
        public UserDeactivatedDomainEvent(UserId id)
        {
            Id = id;
        }

        public new UserId Id { get; }
    }
}
