using System.Text.Json.Serialization;

// need to change these to use JsonPropertyNames so i follow C# class naming conventions
namespace ReactApp1.Server.Dtos
{
    public sealed class DirectionsResponseDto
    {
        public string code { get; init; } = default!;
        public ICollection<Route>? routes { get; init; }
        public string? uuid { get; init; }

        public List<Waypoint>? waypoints { get; init; }
    }

    // need to add the following to Route
    // distance_typical, duration_typical,
    public sealed class Route
    {
        public double? distance { get; init; }
        public double? duration { get; init; }
        public double? weight { get; init; }
        public string? weight_name { get; init; }
        public GeoJsonLineString? geometry { get; init; } = default!;
        public List<RouteLeg>? legs { get; init; }
    }

    public sealed class Waypoint
    {
        public string name { get; init; }

        public double[] location { get; init; }

        //public double longitude { get; init; }

        //public double latitude { get; init; }

        public double distance { get; init; }
    }

    // for annotations need to add annotations query parameters, see mapbox api docs
    // need to add the following to RouteLeg
    // distance_typical, duration_typical, summary, annotation.speed
    // annotation.duration, annotation.distance, annotation.congestion_numeric
    // and maybe add these
    // incidents, closures
    public sealed class RouteLeg
    {
        public string? summary { get; init; }
        public double distance { get; init; }
        public double duration { get; init; }
        public double? weight { get; init; }
    }

    // maybe add route step but idk

    // and maybe add a step object

    // possibly incidents too

    public sealed class GeoJsonLineString
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "LineString";

        // Each position is [longitude, latitude]
        [JsonPropertyName("coordinates")]
        public List<double[]> Coordinates { get; init; } = new();
    }
}
