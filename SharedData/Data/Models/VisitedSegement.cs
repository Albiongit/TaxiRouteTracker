namespace SharedData.Data.Models;

public class VisitedSegement
{
    public int StartNodeNumber { get; set; }
    public int EndNodeNumber { get; set; }
    public string Name { get; set; }
    public int TimeAmount { get; set; }
    public DateTime Time { get; set; }
    public int RouteCounter { get; set; }
}
