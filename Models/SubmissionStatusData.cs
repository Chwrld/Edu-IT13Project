namespace MauiAppIT13.Models;

public class SubmissionStatusData
{
    public string ClassName { get; set; } = string.Empty;
    public int Submitted { get; set; }
    public int Total { get; set; }
    public double SubmissionRate => Total > 0 ? (Submitted / (double)Total) * 100 : 0;
}
