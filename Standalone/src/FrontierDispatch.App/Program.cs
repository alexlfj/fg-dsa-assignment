using System;
using System.Diagnostics;
using FrontierDispatch.Core;

if (args.Length > 0 && args[0] == "--baseline")
{
    RunBaseline();
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