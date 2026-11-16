using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    /// <summary>
    /// Describes the result of a route-search operation.
    /// </summary>
    public sealed class RouteResult
    {
        public RouteResult(bool found, IReadOnlyList<int> locationIds, int totalCost, int expandedCount)
        {
            Found = found;
            LocationIds = locationIds ?? throw new ArgumentNullException(nameof(locationIds));
            TotalCost = totalCost;
            ExpandedCount = expandedCount;
        }
        
        public bool Found { get; }
        public IReadOnlyList<int> LocationIds { get; }
        public int TotalCost { get; }
        public int ExpandedCount { get; }
        
        public static RouteResult Success(IReadOnlyList<int> path, int totalCost, int expandedNodes)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return new RouteResult(true, path, totalCost, expandedNodes);
        }

        public static RouteResult NotFound(int expandedNodes)
        {
            return new RouteResult(false, Array.Empty<int>(), 0, expandedNodes);
        }
    }
}
