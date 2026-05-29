using CompanyName.MyMeetings.Modules.Meetings.Application.Configuration.Commands;
using CompanyName.MyMeetings.Modules.Meetings.Domain.Meetings;
using CompanyName.MyMeetings.Modules.Meetings.Domain.Members;

namespace CompanyName.MyMeetings.Modules.Meetings.Application.Meetings.MarkMeetingAttendeeFeeAsPaid
{
    internal class MarkMeetingAttendeeFeeAsPaidCommandHandler : ICommandHandler<MarkMeetingAttendeeFeeAsPaidCommand>
    {
        private readonly IMeetingRepository _meetingRepository;

        public MarkMeetingAttendeeFeeAsPaidCommandHandler(IMeetingRepository meetingRepository)
        {
            _meetingRepository = meetingRepository;
        }

        public async Task Handle(MarkMeetingAttendeeFeeAsPaidCommand command, CancellationToken cancellationToken)
        {
            var meeting = await _meetingRepository.GetByIdAsync(new MeetingId(command.MeetingId));

            meeting.MarkAttendeeFeeAsPaid(new MemberId(command.MemberId));
        }
    }
}
