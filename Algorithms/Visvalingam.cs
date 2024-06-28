using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Types;

namespace SqlSpatial.Simplify.Algorithms;

public class Visvalingam
{
	private struct CoordinatesContainer
	{
		public readonly int OriginalIndex;
		public readonly double Lat;
		public readonly double Long;

		// ReSharper disable once ConvertToPrimaryConstructor
		public CoordinatesContainer(int originalIndex, double lat, double longVal)
		{
			OriginalIndex = originalIndex;
			Lat = lat;
			Long = longVal;
		}
	}

	private int _coordinateContainerIdCounter = 0;
	private SqlGeographyBuilder Builder { get; set; }
    private SqlGeography SourceGeography { get; set; }
    private bool IsPolygon { get; set; }
    private OpenGisGeographyType GeographyType { get; set; }
    private SimplifyMode Mode { get; set; }
    private double Tolerance { get; set; }

    // ReSharper disable once ConvertToPrimaryConstructor
    public Visvalingam(SqlGeographyBuilder builder, SqlGeography sourceGeography, SimplifyMode mode,  double tolerance)
    {
	    Builder = builder;
        SourceGeography = sourceGeography;
        Mode = mode;
        IsPolygon = SourceGeography.STGeometryType().Value == "Polygon";
        GeographyType = IsPolygon ? OpenGisGeographyType.Polygon : OpenGisGeographyType.LineString;
        Tolerance = tolerance;

    }

    private readonly Dictionary<int, Dictionary<int, Dictionary<int, double>>> _areaCache = new Dictionary<int, Dictionary<int, Dictionary<int, double>>>();
    private double GetCachedPolygonArea(CoordinatesContainer p1, CoordinatesContainer p2, CoordinatesContainer p3)
    {
	    if (_areaCache.ContainsKey(p1.OriginalIndex))
	    {
		    if (_areaCache[p1.OriginalIndex].ContainsKey(p2.OriginalIndex))
		    {
			    if (_areaCache[p1.OriginalIndex][p2.OriginalIndex].ContainsKey(p3.OriginalIndex))
			    {
				    return _areaCache[p1.OriginalIndex][p2.OriginalIndex][p3.OriginalIndex];
			    }
		    }
		    else
		    {
			    _areaCache[p1.OriginalIndex][p2.OriginalIndex] = new Dictionary<int, double>();
		    }
	    }
	    else
	    {
		    _areaCache[p1.OriginalIndex] = new Dictionary<int, Dictionary<int, double>>
		    {
			    [p2.OriginalIndex] = new Dictionary<int, double>()
		    };
	    }

	    var builder = new SqlGeographyBuilder();
	    builder.SetSrid(4326);
	    builder.BeginGeography(OpenGisGeographyType.Polygon);
	    builder.BeginFigure(p1.Lat, p1.Long);
	    builder.AddLine(p2.Lat, p2.Long);
	    builder.AddLine(p3.Lat, p3.Long);
	    builder.AddLine(p1.Lat, p1.Long); //polygons must start where they begin
	    builder.EndFigure();
	    builder.EndGeography();

	    _areaCache[p1.OriginalIndex][p2.OriginalIndex][p3.OriginalIndex] = builder.ConstructedGeography.STArea().Value;

	    return _areaCache[p1.OriginalIndex][p2.OriginalIndex][p3.OriginalIndex];
    }

    public void Simplify()
    {
	    if (IsPolygon)
	    {
		    var nRings = SourceGeography.NumRings().Value;
		    var limit = nRings + 1;
		    for (var i = 1; i < limit; i++)
		    {
			    SimplifyRing(SourceGeography.RingN(i));
		    }
	    }
	    else
	    {
		    SimplifyRing(SourceGeography);
	    }
    }

    private void SimplifyRing(SqlGeography ring)
    {
	    var points = GetRingAsVectorPointsList(ring);
	    if ( IsPolygon && points.Count > 3 || !IsPolygon && points.Count > 2)
	    {
		    if (Mode == SimplifyMode.MinimumArea)
		    {
			    points = SimplifyPointsByMinimumArea(points);
		    }
		    else if (Mode == SimplifyMode.PercentagePointsRetained)
		    {
			    points = SimplifyPointsByPercentageRetained(points);
		    }
	    }

	    BuildGeographyFromPoints(points);
    }


    private List<CoordinatesContainer> SimplifyPointsByPercentageRetained(List<CoordinatesContainer> points)
    {
	    var targetNumberOfPoints = (Tolerance / 100.0) * points.Count;
	    if (targetNumberOfPoints < 4) return points;

	    while (points.Count > targetNumberOfPoints)
	    {
		    var minArea = double.MaxValue;
		    var minPointIdx = -1;

		    var limit = points.Count - 1;
		    for (var i = 1; i < limit; i++)
		    {
			    var nextArea = GetCachedPolygonArea(points[i - 1], points[i], points[i + 1]);
			    if (nextArea < minArea)
			    {
				    minArea = nextArea;
				    minPointIdx = i;
			    }
		    }

		    points.RemoveAt(minPointIdx);
	    }

	    return points;
    }

    private List<CoordinatesContainer> SimplifyPointsByMinimumArea(List<CoordinatesContainer> points)
    {
	    //SqlGeography type is 1-based, so natural limit would be NPoints + 1
	    //BUT, we need to stop 2 points early because I'm too stupid to figure out the maths on this properly
	    while (true)
	    {
		    var limit = points.Count - 1;
		    if (limit < 3)
		    {
			    break;
		    }

		    var minArea = double.MaxValue;
		    var minPointIdx = -1;
		    for (var i = 1; i < limit; i++)
		    {
			    var nextArea = GetCachedPolygonArea(points[i - 1], points[i], points[i + 1]);
			    if (nextArea < minArea)
			    {
				    minArea = nextArea;
				    minPointIdx = i;
			    }
		    }

		    if (minArea > Tolerance)
		    {
			    break;
		    }

		    points.RemoveAt(minPointIdx);
	    }

	    return points;
    }

    private List<CoordinatesContainer> GetRingAsVectorPointsList(SqlGeography ring)
    {
	    var points = new List<CoordinatesContainer>();

	    //populate points with all the SqlGeography's points
	    var nPoints = ring.STNumPoints().Value;
	    var limit = IsPolygon ? nPoints : nPoints + 1; //stop early for polygons cos the last point will be a duplicate of the first

	    for (var i = 1; i < limit; i++)
	    {
		    var sqlGeographyPoint = SourceGeography.STPointN(i);
		    points.Add(new CoordinatesContainer(_coordinateContainerIdCounter++, sqlGeographyPoint.Lat.Value, sqlGeographyPoint.Long.Value));
	    }

	    return points;
    }

    private void BuildGeographyFromPoints(List<CoordinatesContainer> points)
    {
	    Builder.BeginGeography(GeographyType);
	    Builder.BeginFigure(points[0].Lat, points[0].Long);

	    var limit = points.Count;
	    for (var i = 1; i < limit; i++)
	    {
		    Builder.AddLine(points[i].Lat, points[i].Long);
	    }

	    if (IsPolygon)
	    {
			Builder.AddLine(points[0].Lat, points[0].Long); //complete the polygon loop by finishing where we started
	    }

	    Builder.EndFigure();
	    Builder.EndGeography();
    }
}