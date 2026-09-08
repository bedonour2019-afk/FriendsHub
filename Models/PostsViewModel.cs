namespace FriendsHub.Models
{
    public class PostItemViewModel
    {
        public Post Post { get; set; } = null!;
        public List<PostComment> Comments { get; set; } = new();
        public List<PostReaction> Reactions { get; set; } = new();
        public string? CurrentUserReaction { get; set; }
        public Dictionary<string, int> ReactionCounts { get; set; } = new();
        public bool CanDelete { get; set; }
    }

    public class StoryItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TimeFormatted => CreatedAt.ToString("HH:mm");
        public bool CanDelete { get; set; }
        public List<StoryComment> Comments { get; set; } = new();
    }

    public class UserStoryGroupDto
    {
        public string Username { get; set; } = string.Empty;
        public string LatestImage => Stories.LastOrDefault()?.ImagePath ?? "";
        public List<StoryItemDto> Stories { get; set; } = new();
    }

    public class PostsFeedViewModel
    {
        public List<PostItemViewModel> Posts { get; set; } = new();
        public List<UserStoryGroupDto> GroupedStories { get; set; } = new();
    }
}
