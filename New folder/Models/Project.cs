using System.Text.RegularExpressions;

namespace BlindMatchAPI.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string TechStack { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int ResearchAreaId { get; set; }
        public ResearchArea ResearchArea { get; set; } = null!;
        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser Student { get; set; } = null!;
        public Match? Match { get; set; }
    }
}