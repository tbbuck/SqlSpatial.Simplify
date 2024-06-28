using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace SqlSpatial.Simplify;

public class SqlClrWrapper
{
        [SqlFunction( DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true, IsPrecise = true)]
        public static SqlGeography SimplifyByMinimumArea(SqlGeography sourceGeography, SqlDouble minimumArea)
        {
            return Simplifier.Simplify(sourceGeography, SimplifyMode.MinimumArea, minimumArea.Value);
        }

        [SqlFunction( DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true, IsPrecise = true)]
        public static SqlGeography SimplifyByPercentagePointsRetained(SqlGeography sourceGeography, SqlDouble percentage)
        {
            return Simplifier.Simplify(sourceGeography, SimplifyMode.PercentagePointsRetained, percentage.Value);
        }
}