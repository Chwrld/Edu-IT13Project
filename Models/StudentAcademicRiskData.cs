namespace MauiAppIT13.Models;

public class StudentAcademicRiskData
{
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public double Grade { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // "Low", "At Risk", "Critical"
}
