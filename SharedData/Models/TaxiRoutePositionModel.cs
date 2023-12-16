namespace SharedData.Models;

public class TaxiRoutePositionModel
{
    public DateTime DeviceDateTime { get; set; }
    public double Longitute { get; set; }
    public double Latitude { get; set; }
    public int Speed { get; set; }
    public bool IsActive { get; set; }
    public bool HasPassenger { get; set; }
    public bool IsTaximeterOn { get; set; }
}