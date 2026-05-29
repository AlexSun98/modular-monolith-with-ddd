using CompanyName.MyMeetings.Modules.Meetings.Application.Configuration.Commands;
using Newtonsoft.Json;

namespace CompanyName.MyMeetings.Modules.Meetings.Application.Meetings.MarkMeetingAttendeeFeeAsPaid
{
    public class MarkMeetingAttendeeFeeAsPaidCommand : InternalCommandBase
    {
        [JsonConstructor]
        public MarkMeetingAttendeeFeeAsPaidCommand(Guid id, Guid memberId, Guid meetingId)
            : base(id)
        {
            MemberId = memberId;

            MeetingId = meetingId;
        }

        public Guid MemberId { get; }

        public Guid MeetingId { get; }
    }
}
