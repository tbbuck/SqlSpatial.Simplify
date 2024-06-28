# Polygon + LineString Simplification SqlClr for SQL Server Spatial Geographies

## Install
```sql
DROP ASSEMBLY IF EXISTS SqlSpatialClr; 

CREATE ASSEMBLY SqlSpatialClr FROM '/where/uploaded/to/mssql/server/SqlSpatial.Simplify.dll' WITH PERMISSION_SET = SAFE; 

CREATE FUNCTION dbo.SimplifyByArea(@polygon GEOGRAPHY, @tolerance FLOAT) RETURNS GEOGRAPHY AS EXTERNAL NAME SqlSpatialClr.[SqlSpatial.Simplify.SqlClrWrapper].SimplifyByMinimumArea; GO
CREATE FUNCTION dbo.SimplifyByPercentage(@polygon GEOGRAPHY, @perentage FLOAT) RETURNS GEOGRAPHY AS EXTERNAL NAME SqlSpatialClr.[SqlSpatial.Simplify.SqlClrWrapper].SimplifyByPercentagePointsRetained; GO
 
GO


````

## Update
You may need to DROP the functions first if the code's C# signatures have changed.

```sql
ALTER ASSEMBLY SqlSpatialClr FROM '/where/uploaded/to/mssql/server/SqlSpatial.Simplify.dll'; 
````

## Uninnstall
```sql
DROP FUNCTION IF EXISTS dbo.SimplifyByArea; GO
DROP FUNCTION IF EXISTS dbo.SimplifyByPercentage; GO

DROP ASSEMBLY IF EXISTS SqlSpatialClr; 
````

## Example usage
```sql

-- simplify by retaining 5% of the largest-area points
DECLARE @percentage FLOAT = 5.0;
DECLARE @geography GEOGRAPHY = (SELECT SomeHugeBoundary FROM lovely_big_city_table WHERE id = 31415);
SELECT dbo.SimplifyByPercentage(@geography, @percentage) AS simplified; 

-- simplify by dropping points with a triangular area under 200m
DECLARE @tolerance FLOAT = 200.0;
DECLARE @geography GEOGRAPHY = (SELECT SomeHugeBoundary FROM lovely_big_city_table WHERE id = 31415);
SELECT dbo.SimplifyByArea(@geography, @tolerance) AS simplified;
```