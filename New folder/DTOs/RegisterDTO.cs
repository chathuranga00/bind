namespace BlindMatchAPI.DTOs
{
    public class RegisternowDTO
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // student, supervisor, module_leader
    }
}