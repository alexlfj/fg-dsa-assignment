using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{

    public sealed class ReachabilityQuery
    {
        private readonly WorldQueries _worldQuery;

        public ReachabilityQuery(WorldData? world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            
            _worldQuery = new WorldQueries(world);
        }
        
        public IReadOnlyDictionary<int, int> FindReachable(int startLocationId, int maxTravelCost)
        {
            Dictionary<int, int> bestCost = new(); // Key: Location ID, Value: Cost 
            MinPriorityQueue<int> frontier = new();
            
            if (_worldQuery.FindLocationById(startLocationId) == null)
            {
                throw new NullReferenceException(nameof(startLocationId));
            }

            bestCost[startLocationId] = 0;
            frontier.Enqueue(startLocationId, 0);

            while (frontier.Count > 0)
            {
                MinPriorityQueue<int>.Entry current = frontier.Peek();
                frontier.Dequeue();
                
                if (current.Priority > bestCost[current.Item])
                {
                    continue;
                }

                foreach (Connection connection in _worldQuery.FindOutgoingConnections(current.Item))
                {
                    int currentCost = bestCost[connection.FromLocationId] + connection.TravelCost;

                    if ((!bestCost.ContainsKey(connection.ToLocationId) || currentCost < bestCost[connection.ToLocationId]) && currentCost <= maxTravelCost)
                    {
                        bestCost[connection.ToLocationId] = currentCost;
                        frontier.Enqueue(connection.ToLocationId, bestCost[connection.ToLocationId]);
                    }
                }
            }
            
            return bestCost;
            
        }
    }
}
