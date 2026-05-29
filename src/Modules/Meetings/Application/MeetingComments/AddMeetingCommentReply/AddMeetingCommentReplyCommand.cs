using CompanyName.MyMeetings.Modules.Meetings.Application.Contracts;

namespace CompanyName.MyMeetings.Modules.Meetings.Application.MeetingComments.AddMeetingCommentReply
{
    public class AddMeetingCommentReplyCommand : CommandBase<Guid>
    {
        public Guid InReplyToCommentId { get; }

        public string Reply { get; }

        public AddMeetingCommentReplyCommand(Guid inReplyToCommentId, string reply)
        {
            InReplyToCommentId = inReplyToCommentId;
            Reply = reply;
        }
    }
}