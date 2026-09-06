namespace FriendsHub.Models
{
    public class TeamsInputViewModel
    {
        public string NamesRaw { get; set; } = string.Empty;
    }

    public class TeamsResultViewModel
    {
        public List<string> TeamA { get; set; } = new();
        public List<string> TeamB { get; set; } = new();
    }
}
