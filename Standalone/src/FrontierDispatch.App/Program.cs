using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FrontierDispatch.Core;

if (args.Length > 0 && args[0] == "--baseline")
{
    RunBaseline();
    return;
}

if (args.Length > 0 && args[0] == "--pathfinding")
{
    RunPathFinding();
    return;
}

// Small sanity-check application.
// This is separate from the larger performance workload below.
WorldData world = SampleWorldFactory.CreateSmall();
var queries = new WorldQueries(world);

Console.WriteLine("Frontier Dispatch - assignment starter");
Console.WriteLine($"Small demo locations: {world.Locations.Count}");
Console.WriteLine($"Find 734: {queries.FindLocationById(734)?.Name ?? "missing"}");
Console.WriteLine();
Console.WriteLine("Run with --baseline to record the supplied baseline workload.");

static void RunBaseline()
{
    // A. World generation
    var worldSw = Stopwatch.StartNew();
    WorldData world = WorldDatasetFactory.CreateMedium();
    worldSw.Stop();

    // A. Index construction (WorldQueries constructor)
    var buildSw = Stopwatch.StartNew();
    var beforeBuildAllocation = GC.GetAllocatedBytesForCurrentThread();
    var queries = new WorldQueries(world);
    var afterBuildAllocation = GC.GetAllocatedBytesForCurrentThread();
    buildSw.Stop();

    int checksum = 0;
    const int iterations = 5000;

    // B + C. Repeated query workload
    var querySw = Stopwatch.StartNew();
    var beforeQueryAllocation = GC.GetAllocatedBytesForCurrentThread();

    for (int i = 0; i < iterations; ++i)
    {
        int id = (i * 997) % world.Locations.Count;

        Location? location = queries.FindLocationById(id);
        checksum += location?.Id ?? 0;
        checksum += queries.FindOutgoingConnections(id).Count;
    }

    querySw.Stop();
    var afterQueryAllocation = GC.GetAllocatedBytesForCurrentThread();

    // Building results table
    double worldMs = worldSw.Elapsed.TotalMilliseconds;
    double buildMs = buildSw.Elapsed.TotalMilliseconds;
    double queryMs = querySw.Elapsed.TotalMilliseconds;
    double preparationMs = worldMs + buildMs;
    long buildAllocatedBytes = afterBuildAllocation - beforeBuildAllocation;
    long queryAllocatedBytes = afterQueryAllocation - beforeQueryAllocation;

    // Box-drawing characters need UTF-8 output on Windows consoles.
    Console.OutputEncoding = System.Text.Encoding.UTF8;

    const int labelWidth = 28;
    const int valueWidth = 14;
    string labelLine = new string('─', labelWidth + 2);
    string valueLine = new string('─', valueWidth + 2);

    Console.WriteLine("");
    Console.WriteLine("");
    Console.WriteLine($"┌{labelLine}┬{valueLine}┐");
    Row("Baseline workload", "Value");
    Console.WriteLine($"├{labelLine}┼{valueLine}┤");
    Row("Locations", world.Locations.Count.ToString());
    Row("Connections", world.Connections.Count.ToString());
    Row("Iterations", iterations.ToString());
    Console.WriteLine($"├{labelLine}┼{valueLine}┤");
    Row("World (CreateMedium)", $"{worldMs:F3} ms");
    Row("Build (WorldQueries ctor)", $"{buildMs:F3} ms");
    Row("Preparation (world + build)", $"{preparationMs:F3} ms");
    Row("Allocated (build)*", $"{buildAllocatedBytes:N0} B");
    Console.WriteLine($"├{labelLine}┼{valueLine}┤");
    Row("Queries", $"{queryMs:F3} ms");
    Row("Allocated (query loop)", $"{queryAllocatedBytes:N0} B");
    Console.WriteLine($"├{labelLine}┼{valueLine}┤");
    Row("Total (prep + queries)", $"{preparationMs + queryMs:F3} ms");
    Console.WriteLine($"├{labelLine}┼{valueLine}┤");
    Row("Checksum", checksum.ToString());
    Console.WriteLine($"└{labelLine}┴{valueLine}┘");
    Console.WriteLine("* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.");
    Console.WriteLine("Absolute time varies by machine. Keep this result for before/after comparison.");
    Console.WriteLine("");

    // Prints one row: label left-aligned, value right-aligned.
    void Row(string label, string value) =>
        Console.WriteLine($"│ {label.PadRight(labelWidth)} │ {value.PadLeft(valueWidth)} │");
}

static void RunPathFinding()
{
    // Three cases per world in SampleWorldFactory: no blocking, a detour, and an unreachable goal.
    // Every world has Harbor (101) and North Gate (734).
    const int origin = 101;
    const int goal = 734;
    const int iterations = 5000;

    var rows = new List<string[]>();

    WorldData small = SampleWorldFactory.CreateSmall();
    rows.Add(RunPathFindingCase("Small", small, origin, goal, "None", null, iterations));
    rows.Add(RunPathFindingCase("Small", small, origin, goal, "{512}", [512], iterations));
    rows.Add(RunPathFindingCase("Small", small, origin, goal, "{205, 310}", [205, 310], iterations));

    WorldData medium = SampleWorldFactory.CreateMedium();
    rows.Add(RunPathFindingCase("Medium", medium, origin, goal, "None", null, iterations));
    rows.Add(RunPathFindingCase("Medium", medium, origin, goal, "{205}", [205], iterations));
    rows.Add(RunPathFindingCase("Medium", medium, origin, goal, "{512, 620, 910}", [512, 620, 910], iterations));

    WorldData large = SampleWorldFactory.CreateLarge();
    rows.Add(RunPathFindingCase("Large", large, origin, goal, "None", null, iterations));
    rows.Add(RunPathFindingCase("Large", large, origin, goal, "Wall with gap", Wall(25, 44), iterations));
    rows.Add(RunPathFindingCase("Large", large, origin, goal, "Full wall", Wall(25, 49), iterations));

    // Box-drawing characters need UTF-8 output on Windows consoles.
    Console.OutputEncoding = System.Text.Encoding.UTF8;

    Console.WriteLine("");
    Console.WriteLine($"Dijkstra (D) vs A*, route {origin} → {goal}, {iterations} runs per algorithm per scenario.");
    Console.WriteLine($"World sizes: Small {small.Locations.Count}, Medium {medium.Locations.Count}, Large {large.Locations.Count:N0} locations.");
    Console.WriteLine("");

    string[] header = { "World", "Blocked", "Found D / A*", "Cost D / A*", "Expanded D / A*", "Avg ms D / A*", "Agree" };
    PrintTable(header, rows);

    Console.WriteLine("Expanded = nodes expanded per route. Agree = same Found, same cost, and same summed cost over all runs.");
    Console.WriteLine("Absolute time varies by machine.");
    Console.WriteLine("");
}

// Blocks column x from row 0 to lastRow in the 50 x 50 CreateLarge world (id = 1000 + y * 50 + x).
static List<int> Wall(int x, int lastRow)
{
    var blocked = new List<int>();
    for (int y = 0; y <= lastRow; ++y)
    {
        blocked.Add(1000 + y * 50 + x);
    }
    return blocked;
}

// Runs both algorithms on one scenario and returns one table row.
static string[] RunPathFindingCase(string worldName, WorldData world, int origin, int goal, string blockedName, List<int>? blocked, int iterations)
{
    var dijkstraStopwatch = Stopwatch.StartNew();
    int dijkstraExpandedNodes = 0;
    int dijkstraChecksum = 0;
    RouteResult dijkstraResult = null!;
    for (int i = 0; i < iterations; ++i)
    {
        var finder = new DijkstraRouteFinder(world);
        dijkstraResult = finder.FindRoute(origin, goal, blocked);
        dijkstraExpandedNodes += dijkstraResult.ExpandedCount;
        dijkstraChecksum += dijkstraResult.TotalCost;
    }
    dijkstraStopwatch.Stop();

    var astarStopwatch = Stopwatch.StartNew();
    int astarExpandedNodes = 0;
    int astarChecksum = 0;
    RouteResult astarResult = null!;
    for (int i = 0; i < iterations; ++i)
    {
        var finder = new AStarRouteFinder(world);
        astarResult = finder.FindRoute(origin, goal, blocked);
        astarExpandedNodes += astarResult.ExpandedCount;
        astarChecksum += astarResult.TotalCost;
    }
    astarStopwatch.Stop();

    double dijkstraAvgMs = dijkstraStopwatch.Elapsed.TotalMilliseconds / iterations;
    double astarAvgMs = astarStopwatch.Elapsed.TotalMilliseconds / iterations;

    bool agree = dijkstraResult.Found == astarResult.Found
                 && dijkstraResult.TotalCost == astarResult.TotalCost
                 && dijkstraChecksum == astarChecksum;

    return new[]
    {
        worldName,
        blockedName,
        $"{(dijkstraResult.Found ? "Yes" : "No")} / {(astarResult.Found ? "Yes" : "No")}",
        $"{dijkstraResult.TotalCost} / {astarResult.TotalCost}",
        $"{dijkstraExpandedNodes / iterations} / {astarExpandedNodes / iterations}",
        $"{dijkstraAvgMs:F4} / {astarAvgMs:F4}",
        agree ? "Yes" : "NO"
    };
}

// Prints a box-drawn table. Column widths fit the longest value.
// The first two columns are left-aligned, the rest right-aligned.
static void PrintTable(string[] header, List<string[]> rows)
{
    int[] widths = new int[header.Length];
    for (int c = 0; c < header.Length; ++c)
    {
        widths[c] = header[c].Length;
        foreach (string[] row in rows)
        {
            widths[c] = Math.Max(widths[c], row[c].Length);
        }
    }

    string Line(char left, char middle, char right) =>
        left + string.Join(middle.ToString(), widths.Select(w => new string('─', w + 2))) + right;

    string Cells(string[] cells) =>
        "│" + string.Join("│", cells.Select((cell, c) =>
            " " + (c < 2 ? cell.PadRight(widths[c]) : cell.PadLeft(widths[c])) + " ")) + "│";

    Console.WriteLine(Line('┌', '┬', '┐'));
    Console.WriteLine(Cells(header));
    Console.WriteLine(Line('├', '┼', '┤'));
    for (int r = 0; r < rows.Count; ++r)
    {
        // A separator between worlds makes the groups easier to read.
        if (r > 0 && rows[r][0] != rows[r - 1][0])
        {
            Console.WriteLine(Line('├', '┼', '┤'));
        }
        Console.WriteLine(Cells(rows[r]));
    }
    Console.WriteLine(Line('└', '┴', '┘'));
}