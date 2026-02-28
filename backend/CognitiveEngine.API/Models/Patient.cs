namespace CognitiveEngine.API.Models;

public class Patient
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime BirthDate { get; set; }
    public string? Gender { get; set; }
    public float Height { get; set; }
    public float Weight { get; set; }

    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public List<TrainingResult> TrainingResults { get; set; } = new();
}