namespace SharedData.Data.Models;

public class VisitedSegement
{
    public string StartNodeNumber { get; set; } = "";
    public string EndNodeNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public int TimeAmount { get; set; }
    public DateTime Time { get; set; }
    public int RouteCounter { get; set; }
    public int RightDirection { get; set; } = 0;
    public int LeftDirection { get; set; } = 0;
    public int StraightDirection { get; set; } = 0;
    public int BackDirection { get; set; } = 0;
}
