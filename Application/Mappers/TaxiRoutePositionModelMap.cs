using CsvHelper.Configuration;
using SharedData.Models;

namespace Application.Mappers;

public class TaxiRoutePositionModelMap : ClassMap<TaxiRoutePositionModel>
{
	public TaxiRoutePositionModelMap()
	{
        Map(x => x.DeviceDateTime).Name("DeviceDateTime");
        Map(x => x.Longitute).Name("Longitute");
        Map(x => x.Latitude).Name("Latitude");
        Map(x => x.Speed).Name("Speed");
        Map(x => x.IsActive).Name("Di1");
        Map(x => x.HasPassenger).Name("Di2");
        Map(x => x.IsTaximeterOn).Name("Di3");
    }
}
