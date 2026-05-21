namespace DeliveryBot.RobotSimulator.Core.Simulation;

public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double DistanceMeters(GeoLocation from, GeoLocation to)
    {
        var fromLat = DegreesToRadians(from.Latitude);
        var toLat = DegreesToRadians(to.Latitude);
        var deltaLat = DegreesToRadians(to.Latitude - from.Latitude);
        var deltaLon = DegreesToRadians(to.Longitude - from.Longitude);

        var a =
            Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
            Math.Cos(fromLat) * Math.Cos(toLat) *
            Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    public static GeoLocation MoveToward(
        GeoLocation from,
        GeoLocation to,
        double distanceMeters)
    {
        var totalDistance = DistanceMeters(from, to);

        if (totalDistance <= 0 || distanceMeters >= totalDistance)
        {
            return to;
        }

        var ratio = distanceMeters / totalDistance;

        var latitude = from.Latitude + ((to.Latitude - from.Latitude) * ratio);
        var longitude = from.Longitude + ((to.Longitude - from.Longitude) * ratio);

        return new GeoLocation(latitude, longitude);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}