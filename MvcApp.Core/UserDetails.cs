namespace MvcApp.Core
{
    public class UserDetails : UserProfile
    {
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; } = 18;
        public int[] AgeRange { get; set; } = [18, 100];
        public string[] Interests { get; set; } = [];
        public string Gender { get; set; } = string.Empty;
        public float Score { get; set; } = 8;
        public string Sexuality { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;        
        public string State { get; set; } = string.Empty;
        public string KnownAs { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        public List<Guid> ReportedUsers { get; set; } = [];
        public DateTime LastTimeOnline { get; set; } = DateTime.UtcNow;
        public DateTime PremiumExpiryDate { get; set; } = DateTime.MinValue;
        public DateTime MatchedWithUser { get; set; } = DateTime.MinValue;
        public bool IsAvailable { get; set; }
        public Guid ChatGroupID { get; set; } = Guid.Empty;
        public string Introduction { get; set; } = "";
        public string LookingFor { get; set; } = "";     
        public ICollection<Photo> Photos { get; set; } = [];
        public ICollection<UserLike> LikedUsers { get; set; } = [];
        public ICollection<UserLike> LikedByUsers { get; set; } = [];
        public ICollection<SuperLike> SuperLikesSent { get; set; } = [];
        public ICollection<SuperLike> SuperLikesReceived { get; set; } = [];
        public ICollection<UserBoost> Boosts { get; set; } = [];
        public ICollection<Message> MessagesSent { get; set; } = [];
        public ICollection<Message> MessagesReceived { get; set; } = [];
        public bool IsProfileComplete { get; set; }
        public bool IsIncognito { get; set; }
        public MessagingPermission MessagingPermission { get; set; } = MessagingPermission.Everyone;
        public bool IsVip => PremiumExpiryDate > DateTime.UtcNow;
        public bool IsVerified { get; set; }
        public int TotalPoints { get; set; }
        public ICollection<UserInterestTag> UserInterestTags { get; set; } = [];
        public ICollection<UserBadge> UserBadges { get; set; } = [];
        public ICollection<PointTransaction> PointTransactions { get; set; } = [];

        //public ICollection<Subscription> Subscriptions { get; set; } = [];
    }
}
