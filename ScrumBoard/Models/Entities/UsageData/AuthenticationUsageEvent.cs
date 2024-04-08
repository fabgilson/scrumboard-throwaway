namespace ScrumBoard.Models.Entities.UsageData
{
    public enum AuthenticationUsageEventType : int
    {
        LogIn = 1,
        LogOut = 2,
    }

    public class AuthenticationUsageEvent : BaseUsageDataEvent
    {
        public AuthenticationUsageEventType AuthenticationEventType { get; set; }

        // Default constructor to keep EF happy
        public AuthenticationUsageEvent(long userId) : base(userId) { }

        public AuthenticationUsageEvent(long userId, AuthenticationUsageEventType eventType) : base(userId)
        {
            AuthenticationEventType = eventType;
        }
    }
}