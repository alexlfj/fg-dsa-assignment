using System;

namespace FrontierDispatch.Core
{
	public static class Heuristics
	{
		public static int Euclidian(Location? from, Location goal)
		{
			var dX = from.Position.X - goal.Position.X;
			var dY = from.Position.Y - goal.Position.Y;
			
			return (int)(MathF.Sqrt(dX*dX + dY*dY));
		}
	}
}
