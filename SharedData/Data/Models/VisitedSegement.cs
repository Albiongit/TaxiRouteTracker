namespace SharedData.Data.Models;

public class VisitedSegement
{
    public string StartNodeNumber { get; set; } = "";
    public string EndNodeNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public int TimeAmount { get; set; }
    public DateTime Time { get; set; }
    public int RouteCounter { get; set; }
}
