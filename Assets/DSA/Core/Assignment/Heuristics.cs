using System;

namespace FrontierDispatch.Core
{
	public static class Heuristics
	{
		public static int Manhattan(Location? from, Location goal)
		{
			return (int)(MathF.Sqrt((from.Position.X - goal.Position.X)^2 + (from.Position.Y - goal.Position.Y)^2));
		}
	}
}
