namespace FrontierDispatch.Core
{
	public class TEMP_ASTAR
	{
		
	}
}
using System;
using System.Collections.Generic;

namespace DSA.Lab06
{
    /// <summary>
    /// Optional Lab 6B extension.
    /// Finish Dijkstra first.
    /// </summary>
    public sealed class AStarRouteFinder
    {
        public RouteResult FindRoute(WeightedGraph graph, string start, string goal, ISet<string>? blocked = null)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (!graph.ContainsNode(start) || !graph.ContainsNode(goal))
            {
                return RouteResult.NotFound(0);
            }

            if (start == goal)
            {
                return RouteResult.Success(new[] { start }, 0, 0);
            }

            Dictionary<string, int> bestCost = new();
            Dictionary<string, string> cameFrom = new();
            MinFrontier<string> frontier = new();

            bestCost[start] = 0;
            frontier.Enqueue(start, 0);

            int expandedNodes = 0;

            while (frontier.Count > 0)
            {
                MinFrontier<string>.Entry current = frontier.Dequeue();

                if (current.Priority > bestCost[current.Value] + Heuristics.Manhattan(graph.GetNode(current.Value),graph.GetNode(goal)))
                {
                    continue;
                }

                expandedNodes++;

                if (current.Value == goal)
                {
                    return RouteResult.Success(Path(), bestCost[goal], expandedNodes);
                }

                foreach (WeightedEdge edge in graph.GetOutgoing(current.Value))
                {
                    int currentCost = bestCost[edge.From] + edge.Cost;

                    if (blocked != null && blocked.Contains(edge.To))
                    {
                        break;
                    }

                    if ((!bestCost.ContainsKey(edge.To) || currentCost < bestCost[edge.To]) )
                    {
                        bestCost[edge.To] = currentCost;
                        cameFrom[edge.To] = edge.From;
                        frontier.Enqueue(edge.To, bestCost[edge.To] + Heuristics.Manhattan(graph.GetNode(edge.To),graph.GetNode(goal)));
                    }
                }
            }
            
            return RouteResult.NotFound(expandedNodes);
        
            IReadOnlyList<string> Path()
            {
                List<string> path = new();

                string current = goal;

                path.Add(current);

                while (current != start)
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
