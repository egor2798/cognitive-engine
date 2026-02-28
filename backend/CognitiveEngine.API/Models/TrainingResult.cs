namespace CognitiveEngine.API.Models;

public class TrainingResult
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public DateTime Date { get; set; }
    public float TimeSeconds { get; set; }
    public int Errors { get; set; }
    public float AvgDeviation { get; set; }
    public string? MetricsJson { get; set; }
}