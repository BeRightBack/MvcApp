namespace MvcApp.Core
{
    public class UserBoost
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public UserDetails? User { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
