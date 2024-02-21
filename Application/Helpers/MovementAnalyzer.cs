using SharedData.Data.Enums;

namespace Application.Helpers;

public static class MovementAnalyzer
{
    // Calculate bearing between two coordinates
    public static double CalculateBearing(double longFrom, double latFrom, double longTo, double latTo)
    {
        double dLon = longTo - longFrom;
        double y = Math.Sin(dLon) * Math.Cos(latTo);
        double x = Math.Cos(latFrom) * Math.Sin(latTo) -
                   Math.Sin(latFrom) * Math.Cos(latTo) * Math.Cos(dLon);
        double bearing = Math.Atan2(y, x);

        // Convert radians to degrees
        bearing = bearing * (180 / Math.PI);
        bearing = (bearing + 360) % 360;

        return bearing;
    }

    // Determine direction based on bearing angle
    public static Direction DetermineDirection(double bearing)
    {
        if (bearing >= 315 || bearing < 45)
            return Direction.Right;
        else if (bearing >= 45 && bearing < 135)
            return Direction.Straight;
        else if (bearing >= 135 && bearing < 225)
            return Direction.Left;
        else
            return Direction.Back;
    }
}