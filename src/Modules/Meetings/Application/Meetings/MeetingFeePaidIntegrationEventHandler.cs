using CompanyName.MyMeetings.Modules.Meetings.Application.Configuration.Commands;
using CompanyName.MyMeetings.Modules.Meetings.Application.Meetings.MarkMeetingAttendeeFeeAsPaid;
using CompanyName.MyMeetings.Modules.Payments.IntegrationEvents;
using MediatR;

namespace CompanyName.MyMeetings.Modules.Meetings.Application.Meetings
{
    public class MeetingFeePaidIntegrationEventHandler : INotificationHandler<MeetingFeePaidIntegrationEvent>
    {
        private readonly ICommandsScheduler _commandsScheduler;

        public MeetingFeePaidIntegrationEventHandler(ICommandsScheduler commandsScheduler)
        {
            _commandsScheduler = commandsScheduler;
        }

        public async Task Handle(MeetingFeePaidIntegrationEvent @event, CancellationToken cancellationToken)
        {
            await _commandsScheduler.EnqueueAsync(new MarkMeetingAttendeeFeeAsPaidCommand(
                Guid.NewGuid(),
                @event.PayerId,
                @event.MeetingId));
        }
    }
}
