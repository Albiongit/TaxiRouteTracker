namespace SharedData.Data.Models;

public class RoadSegment
{
    public string Name { get; set; } = "";
    public Node StartNode { get; set; } = null!;
    public Node EndNode { get; set; } = null!;
    public int StartNodeNumber { get; set; }
    public int EndNodeNumber { get; set; }
}
