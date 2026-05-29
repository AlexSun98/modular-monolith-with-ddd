using CompanyName.MyMeetings.BuildingBlocks.Domain;
using CompanyName.MyMeetings.Modules.Meetings.Domain.SharedKernel;

namespace CompanyName.MyMeetings.Modules.Meetings.Domain.MeetingGroups.Rules
{
    public class MeetingCanBeOrganizedOnlyByPaidGroupRule : IBusinessRule
    {
        private readonly DateTime? _paymentDateTo;

        internal MeetingCanBeOrganizedOnlyByPaidGroupRule(DateTime? paymentDateTo)
        {
            _paymentDateTo = paymentDateTo;
        }

        public bool IsBroken() => !_paymentDateTo.HasValue || _paymentDateTo < SystemClock.Now;

        public string Message => "Meeting can be organized only by paid group";
    }
}
