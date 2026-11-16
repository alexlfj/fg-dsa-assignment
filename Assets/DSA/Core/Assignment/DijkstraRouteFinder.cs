using System;
using System.Collections.Generic;
using System.IO;

namespace FrontierDispatch.Core
{
    public sealed class DijkstraRouteFinder
    {
        private readonly WorldData _world;

        private readonly WorldQueries _worldQuery;

        public DijkstraRouteFinder(WorldData world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _worldQuery = new WorldQueries(world);
        }

        public RouteResult FindRoute(int startLocationId, int goalLocationId)
        {
            if (_worldQuery.FindLocationById(startLocationId) == null || _worldQuery.FindLocationById(goalLocationId) == null)
            {
                return RouteResult.NotFound(0);
            }

            if (startLocationId == goalLocationId)
            {
                return RouteResult.Success(new[] { startLocationId }, 0, 0);
            }
            
            Dictionary<int, int> bestCost = new(); // Key: Location ID, Value: Cost 
            Dictionary<int, int> cameFrom = new(); // Key: Location ID from, Location ID to
            MinPriorityQueue<int> frontier = new();

            bestCost[startLocationId] = 0;
            frontier.Enqueue(startLocationId, 0);

            int expandedNodes = 0;

            while (frontier.Count > 0)
            {
                MinPriorityQueue<int>.Entry current = frontier.Peek();
                frontier.Dequeue();
                
                if (current.Priority > bestCost[current.Item])
                {
                    continue;
                }

                expandedNodes++;

                if (current.Item == goalLocationId)
                {
                    return RouteResult.Success(Path(), bestCost[goalLocationId], expandedNodes);
                }

                foreach (Connection edge in _worldQuery.FindOutgoingConnections(current.Item))
                {
                    int currentCost = bestCost[edge.FromLocationId] + edge.TravelCost;

                    if (!bestCost.ContainsKey(edge.ToLocationId) || currentCost < bestCost[edge.ToLocationId])
                    {
                        bestCost[edge.ToLocationId] = currentCost;
                        cameFrom[edge.ToLocationId] = edge.FromLocationId;
                        frontier.Enqueue(edge.ToLocationId, bestCost[edge.ToLocationId]);
                    }
                }
            }
            
            return RouteResult.NotFound(expandedNodes);
        
            IReadOnlyList<int> Path()
            {
                List<int> path = new();

                int current = goalLocationId;

                path.Add(current);

                while (current != startLocationId)
                {
                    current = cameFrom[current];
                    path.Add(current);
                }

                path.Reverse();
                return path;
            }
        }
    }
}
