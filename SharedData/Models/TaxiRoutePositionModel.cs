namespace SharedData.Models;

public class TaxiRoutePositionModel
{
    public double Longitute { get; set; }
    public double Latitude { get; set; }
    public bool IsActive { get; set; }
    public bool HasPassenger { get; set; }
    public bool IsTaximeterOn { get; set; }
    public DateTime Time { get; set; }
}