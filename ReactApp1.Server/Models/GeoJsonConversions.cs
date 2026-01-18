using System.Text.Json;
using System.Text.Json.Serialization;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using ReactApp1.Server.Dtos;

namespace ReactApp1.Server.Models
{
    public static class GeoJsonConversions
    {
        public static FeatureCollection ConvertToFeatureCollection(List<OutageDataDto> dto)
        {
            var geoFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            //var json = JsonSerializer.Serialize<OutageDataDto>(dto);

            var featureCollection = new FeatureCollection();

            //  remember that Geojson looks like this:
            //  Type: (Feature or FeatureCollection)
            //  Geometry: (Point, Linestring....Mult-point)
            //  Properties: (Anything not a geometry)
            // we have a Type key, Geometry key, and a Properties key on the lhs.

            foreach (var d in dto)
            {
                // this corresponds to the Geometry key above
                var geom = geoFactory.CreatePoint(new Coordinate(d.Longitude, d.Latitude));

                // this corresponds to the Properties key above
                var properties = new AttributesTable
                {
                    { "id", d.Id },
                    { "startTime", d.StartTime },
                    { "lastUpdatedTime", d.LastUpdatedTime },
                    { "etrTime", d.EtrTime },
                    { "numPeople", d.NumPeople },
                    { "status", d.Status },
                    { "cause", d.Status },
                    { "identifier", d.identifier },
                    {"additionalPRoperties", d.AdditionalProperties }
                };

                featureCollection.Add(new Feature(geom, properties));

            }

            return featureCollection;
        }
    }
}
