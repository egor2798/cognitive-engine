namespace CognitiveEngine.API.Models;

public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Inn { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public List<User> Users { get; set; } = new();
    public List<Patient> Patients { get; set; } = new();
}