using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace ReactApp1.Server.Models;

// need to add a proper id again to DirectionsResponseEntity
// that's the way we would be able to grab the first 100, 200, or whatever we wanted
// replace the parts of this that say `null!` and replace them with actual delineated foreign keys

public class DirectionsResponseEntity
{
    [Key]
    public int Id { get; set; }

    public string Uuid { get; set; }

    public ICollection<RouteEntity> RouteEntities { get; set; } = new List<RouteEntity>();

    public ICollection<WaypointEntity>? WaypointEntities { get; set; }
}

public class RouteEntity
{
    [Key]
    public int Id { get; set; }

    public int RouteId { get; set; }

    public int DirectionsResponseId { get; set; }

    public DirectionsResponseEntity DirectionsResponse { get; set; }

    public double? Distance { get; set; }
    public double? Duration { get; set; }
    public double? Weight { get; set; }
    public string? WeightName { get; set; }

    public LineString? Geometry { get; set; }

    public ICollection<RouteLegEntity>? Legs { get; set; } = new List<RouteLegEntity>();
}

public class WaypointEntity
{
    [Key]
    public int Id { get; set; }

    public int DirectionsResponseEntityId { get; set; }

    public RouteEntity RouteEntity { get; set; }

    public string Name { get; set; }

    public double[] Location { get; set; }

    //public double Latitude { get; set; }

    //public double Longitude { get; set; }

    public double Distance { get; set; }
}

public class RouteLegEntity
{
    [Key]
    public int Id { get; set; }

    public int RouteEntityRouteId { get; set; } // should be RouteEntityId, apparently as it should match in the Id and Entity way like in RouteEntity for
                                                // its containing entity DirectionsResponseEntity

    public RouteEntity RouteEntity { get; set; }

    public string? Summary { get; set; }
    public double Distance { get; set; }
    public double Duration { get; set; }
    public double? Weight { get; set; }
}