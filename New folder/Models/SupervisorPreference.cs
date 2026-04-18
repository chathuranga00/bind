namespace BlindMatchAPI.Models
{
    public class SupervisorPreference
    {
        public int Id { get; set; }
        public string SupervisorId { get; set; } = string.Empty;
        public ApplicationUser Supervisor { get; set; } = null!;
        public int ResearchAreaId { get; set; }
        public ResearchArea ResearchArea { get; set; } = null!;
    }
}