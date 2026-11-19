using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    public static class SampleWorldFactory
    {
        public static WorldData CreateSmall()
        {
            var locations = new[]
            {
                new Location(101, "Harbor", new WorldPosition(-4, -1), "Port"),
                new Location(205, "Relay", new WorldPosition(-1, 2), "Relay"),
                new Location(310, "Crossroads", new WorldPosition(1, 0), "Junction"),
                new Location(512, "Old Mine", new WorldPosition(4, 2), "Industry"),
                new Location(734, "North Gate", new WorldPosition(3, 5), "Gate"),
                new Location(901, "Farm", new WorldPosition(-3, 5), "Farm"),
                new Location(666, "Hell", new WorldPosition(-4, 1), "Gate"),
                new Location(999, "Persephone's Garden", new WorldPosition(-4, 2), "Farm")
            };

            var connections = new[]
            {
                new Connection(101, 205, 4), new Connection(205, 101, 4),
                new Connection(101, 310, 7), new Connection(310, 101, 7),
                new Connection(205, 310, 3), new Connection(310, 205, 3),
                new Connection(205, 901, 5), new Connection(901, 205, 5),
                new Connection(310, 512, 4), new Connection(512, 310, 4),
                new Connection(310, 734, 8), new Connection(734, 310, 8),
                new Connection(512, 734, 3), new Connection(734, 512, 3),
                new Connection(901, 734, 7), new Connection(734, 901, 7),
                new Connection(666, 999, 0), new Connection(999, 666, 0)
            };

            return new WorldData(locations, connections);
        }
        
        public static WorldData CreateMedium()
        {
            var locations = new[]
            {
                new Location(101, "Harbor", new WorldPosition(-18, -14), "Port"),
                new Location(102, "Lighthouse", new WorldPosition(-20, -6), "Port"),
                new Location(120, "Salt Flats", new WorldPosition(-20, -18), "Wilderness"),
                new Location(130, "Ruins", new WorldPosition(-10, -20), "Wilderness"),
                new Location(205, "Relay", new WorldPosition(-10, -8), "Relay"),
                new Location(210, "Fishing Camp", new WorldPosition(-14, 2), "Camp"),
                new Location(310, "Crossroads", new WorldPosition(-2, -4), "Junction"),
                new Location(315, "Trading Post", new WorldPosition(-6, 6), "Market"),
                new Location(320, "Sawmill", new WorldPosition(4, -12), "Industry"),
                new Location(410, "Ferry Landing", new WorldPosition(8, -4), "Port"),
                new Location(415, "Watchtower", new WorldPosition(0, 8), "Outpost"),
                new Location(512, "Old Mine", new WorldPosition(14, 4), "Industry"),
                new Location(520, "Quarry", new WorldPosition(18, -8), "Industry"),
                new Location(601, "Ranger Station", new WorldPosition(-8, 14), "Outpost"),
                new Location(610, "Chapel", new WorldPosition(6, 14), "Settlement"),
                new Location(620, "Smelter", new WorldPosition(12, 10), "Industry"),
                new Location(901, "Farm", new WorldPosition(-16, 12), "Farm"),
                new Location(905, "Orchard", new WorldPosition(-12, 18), "Farm"),
                new Location(910, "Pasture", new WorldPosition(2, 20), "Farm"),
                new Location(734, "North Gate", new WorldPosition(16, 18), "Gate")
            };

            var locationsById = new Dictionary<int, Location>();
            foreach (Location location in locations)
            {
                locationsById.Add(location.Id, location);
            }

            var connections = new List<Connection>();

            void Road(int a, int b, int extraCost = 0) =>
                AddRoad(connections, locationsById[a], locationsById[b], extraCost);

            Road(101, 102);
            Road(101, 205);
            Road(101, 120);
            Road(120, 130);
            Road(130, 205);
            Road(130, 320);
            Road(102, 210);
            Road(205, 210);
            Road(205, 310);
            Road(205, 320);
            Road(210, 315);
            Road(210, 901);
            Road(310, 315);
            Road(310, 410);
            Road(310, 415, extraCost: 3);  // river ford
            Road(320, 410);
            Road(320, 520);
            Road(410, 512);
            Road(410, 520);
            Road(415, 315);
            Road(415, 610);
            Road(315, 601);
            Road(512, 620);
            Road(512, 520);
            Road(512, 734, extraCost: 10); // mountain pass
            Road(620, 734);
            Road(610, 620);
            Road(610, 910);
            Road(910, 734);
            Road(601, 901);
            Road(601, 905);
            Road(905, 910);
            Road(901, 905);

            return new WorldData(locations, connections);
        }

        public static WorldData CreateLarge()
        {
            const int width = 50;
            const int height = 50;
            const int spacing = 10;

            var locations = new List<Location>(width * height);
            var grid = new Location[width, height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    // Fixed offset between -3 and +3 so the outposts don't sit on a perfect grid.
                    int offsetX = (x * 73 + y * 151) % 7 - 3;
                    int offsetY = (x * 37 + y * 89) % 7 - 3;
                    var position = new WorldPosition(x * spacing + offsetX, y * spacing + offsetY);

                    Location location;
                    if (x == 2 && y == 2)
                    {
                        location = new Location(101, "Harbor", position, "Port");
                    }
                    else if (x == width - 3 && y == height - 3)
                    {
                        location = new Location(734, "North Gate", position, "Gate");
                    }
                    else
                    {
                        int id = 1000 + y * width + x;
                        location = new Location(id, $"Outpost {id}", position, "Outpost");
                    }

                    locations.Add(location);
                    grid[x, y] = location;
                }
            }

            // Right, down, down-right and down-left. AddRoad adds both directions,
            // so this covers all eight neighbours without adding any road twice.
            int[] stepX = { 1, 0, 1, -1 };
            int[] stepY = { 0, 1, 1, 1 };

            var connections = new List<Connection>();

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    for (int i = 0; i < stepX.Length; ++i)
                    {
                        int neighbourX = x + stepX[i];
                        int neighbourY = y + stepY[i];

                        if (neighbourX < 0 || neighbourX >= width || neighbourY >= height)
                        {
                            continue;
                        }

                        int roughGround = (x * 31 + y * 17 + stepX[i] * 7 + stepY[i] * 13) % 3;
                        AddRoad(connections, grid[x, y], grid[neighbourX, neighbourY], roughGround);
                    }
                }
            }

            return new WorldData(locations, connections);
        }

        // Adds a two-way road. The cost is the straight-line length rounded up, plus any extra cost.
        private static void AddRoad(List<Connection> connections, Location a, Location b, int extraCost)
        {
            int dx = a.Position.X - b.Position.X;
            int dy = a.Position.Y - b.Position.Y;
            int cost = (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy)) + extraCost;

            connections.Add(new Connection(a.Id, b.Id, cost));
            connections.Add(new Connection(b.Id, a.Id, cost));
        }
    }
}
