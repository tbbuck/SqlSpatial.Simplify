using System;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;
using SqlSpatial.Simplify.Algorithms;

namespace SqlSpatial.Simplify;

public static class Simplifier
{
    // Handles MultiPolygon and LineString types by recursively iterating through them
    // and calling the same code as for individual Polygon and LineString types
    // That's the theory, at least.
    public static SqlGeography Simplify(SqlGeography sourceGeography, SimplifyMode mode, double tolerance)
    {
        var builder = new SqlGeographyBuilder();
        builder.SetSrid(4326);

        SimplifyGeography(builder, sourceGeography, mode, tolerance);

        return builder.ConstructedGeography;
    }

    private static void SimplifyGeography(SqlGeographyBuilder builder, SqlGeography innerGeography, SimplifyMode mode, double tolerance)
    {
        var geogType = innerGeography.STGeometryType().Value;
        if (geogType == "LineString" || geogType == "Polygon")
        {
            var visvalingam = new Visvalingam(builder, innerGeography, mode, tolerance);
            visvalingam.Simplify();
        }
        else if (geogType == "MultiPolygon" || geogType == "MultiLineString")
        {
            builder.BeginGeography(geogType == "MultiPolygon" ? OpenGisGeographyType.MultiPolygon : OpenGisGeographyType.MultiLineString);

            var nGeographies = innerGeography.STNumGeometries().Value;
            var limit = nGeographies + 1;
            for (var i = 1; i < limit; i++)
            {
                SimplifyGeography(builder, innerGeography.STGeometryN(i), mode, tolerance);
            }

            builder.EndGeography();
        }
        else
        {
            throw new NotImplementedException("Simplify() not implemented for SqlGeography type " + geogType);
        }
    }
}