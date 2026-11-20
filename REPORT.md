# REPORT.md - Individual Assignment

## 1. Initial analysis

### How are locations and connections represented in the baseline?

They are represented as unsorted arrays, and are constructed with the following properties:
- Location: ID (int), Name (string), Position (WorldPosition, which is a Vector2), and Type (string)
- Connection: FromLocationId (int), ToLocationId (int), TravelCost (int).

The `WorldData` then takes those unsorted arrays and creates `ReadOnlyList`.

### Which operations appear frequent?
The operation that appears the most frequently is linear search, both in `FindLocationByID` and in `FindOutgoingConnections`.

### Which operation(s) do you predict may scale poorly?
I predict that the linear search operation will scale poorly as it follows O(n) on average.

### What will you investigate first?
I will investigate how the data is created and represented to see if I can improve the efficiency of the search operation from something better than O(n).

## 2. Data representation

### Representation choice

In the baseline, `FindLocationById` is a linear search, so we have a time complexity of O(n) on average. Since this is a find by ID operation, a common approach to this is using hash tables. Specifically, a `Dictionary<TKey, TValue>`, as we need the ID and the location to which the ID points.

So I defined a new `Dictionary<int, Location>` variable, created a helper function to populate that dictionary using our `_world` WorldData, and called the method in the constructor. At first I only passed the test of finding a key that actually exists and failed the one that had a missing key. The test wanted to return null, so I needed to allow it. Thus we added `?` when defining the `_locations` variable and in the return of `FindLocationById` to cover those bases.

For `FindOutgoingConnections`, the operation that we need is to find all outgoing connections from a specific location. As far as my knowledge goes (and it is not that far), there are two ways of doing this:

1. Linear search. Has a time complexity of O(n), on average, and it loops through all the nodes unnecessarily.
2. Dictionary. Virtually an instant lookup, however it takes upfront work to map all the connections first, uses more memory, and is static, meaning we would have to create a new dictionary every time our map is updated.

I decided to go for the dictionary as I would like to get the instant lookup times at the cost of more memory. The connections dictionary stores a list of all connections for each node.

`FindOutgoingConnections` was returning `null` for our disconnected node because it had no connections, so we needed to modify the method to allow that. It now returns an empty list.

### Operations it improves

| Version | Lookups | Queries (5000 iterations) |
| --- | --- | --- |
| V0: Starter | Linear search in both methods | 451.986 ms |
| V1: Location dictionary | `FindLocationById` uses a `Dictionary` | 400.142 ms |
| Current | Both methods use a `Dictionary` | 0.419 ms |

*Run set C (Debug), averages. All runs are in Appendix A.*

This means that we are saving a lot of time for pathfinding algorithms, as we can instantly look up nodes instead of waiting for a linear search through everything for every expanded node.

- **Dijkstra:** `FindLocationById` twice at the start (checking start and goal), then `FindOutgoingConnections` once for every expanded node.
- **Reachability:** `FindLocationById` once at the start, then `FindOutgoingConnections` once for every expanded node.
- **A\*:** same as Dijkstra, plus `FindLocationById` for every heuristic call, which means once per dequeued entry and once per neighbour added to the frontier.

### Time/memory trade-off

Our implementation takes longer to build what we need once, and saves the time on the repeated lookups. I'd say it was worth it. The time reduction is extremely significant, which makes the increase in memory allocation justifiable. However, if the situation requires keeping memory usage to a minimum, then the starter is preferable.

### Retained representations

I kept `WorldData`'s arrays and the `Location` and `Connection` classes as they were, because I didn't need to change them for my implementation. I don't want to refactor code if it is not necessary.

### Building the indexes

`DijkstraRouteFinder`, `AStarRouteFinder` and `ReachabilityQuery` each build their own `WorldQueries` in their constructor. Each finder builds its `WorldQueries` once and reuses it for every `FindRoute` / `FindReachable` call on that finder. I included building the indexes in each pathfinder to allow for the introduction of different worlds in the algorithm in the future, if needed.

## 3. Algorithms and adaptation

### Priority queue

Since we already did this in Lab 4B, I basically just copied and pasted my own code from the lab and tweaked it for the assignment. The only changes were:

- Changing `PriorityEntry` to be the given `Entry` struct, and adjusting the methods accordingly.
- For `Dequeue` and `PeekNext`, return the item and not the entry itself.

The heap is stored in a `List<Entry>`. For every parent of index `i`, the children will be `2i + 1` and `2i + 2`.

The invariant is that each parent node should never be higher than its children. `Enqueue` and `Dequeue` do it through the `SiftUp`/`SiftDown` operations to re-organize the heap.

`Enqueue`:
1. Create an entry from the item and priority.
2. Add the entry to the end of the heap.
3. Get indexes for comparison.
4. Check for parent priority and swap if invariant is violated.

`Dequeue`:
1. Store the original root.
2. Get the last entry and its index.
3. Move the final entry to the root position.
4. Remove the slot where that final entry was.
5. Restore the invariant via Sift Down.
6. Return the original root.

### Route finding (Dijkstra)

Right from the get-go we have three important variables:

- `bestCost`: dictionary that keeps track of what is the lowest cost (value) to reach a certain node (key).
- `cameFrom`: dictionary that keeps track of what node (value) we came from to arrive at a specific node (key).
- `frontier`: binary min-heap where we visit our nodes in priority order (in this case, the lowest cost).

We also make sure that we initialize `frontier` with our starter node, and that the `bestCost` of our starter node is 0, since we started at it.

So here is the logic:

- We will search until we don't have any more nodes to search in `frontier`, or if we reach the goal node.
- We will skip any path to nodes in which we already know there is a shorter path from. We don't remove stale entries from the heap, we skip them.
- For each node that we are searching, we will go through all its edges and calculate the cost of each edge. When we find the lowest cost edge to a node, we will add it to our overall best cost, mark that the current node is the one with the shortest path to the next node, and add the next node to `frontier` to be searched in the future.
- If there is no path to the goal, we `return RouteResult.NotFound`, but if we do, we `return RouteResult.Success`, with the path to the goal, how much it costs, and how many nodes we went through to get there.
- Path is calculated by creating a list, adding the goal node as our first element, and iteratively going through our `cameFrom` nodes until we reach our start node, then reversing it to get the actual path.

When the goal is dequeued, it means that you found the best-cost path to it, or else it wouldn't be at the first index of the frontier.

`ExpandedCount` counts dequeued entries that are not stale (stale being higher cost than the best cost we know of the current node).

To adapt it to the assignment, we modified `MinPriorityQueue` in two ways:

- Created a method called `Peek` that allows us to check the first value of the heap, and used that to find our current node. This naturally led to putting the `frontier.Dequeue()` in its own separate line. I changed `Peek()` to allow Dijkstra and A\*, because I needed a method to return an entry, instead of an item T.
- Changed the accessibility level of the `Entry` from `private` to `public`, so we could access the items and priorities of the heap.

We also modified `RouteResult` to include the `Success()` and `NotFound()` methods.

In `SampleWorldFactory.CreateSmall()`, the correct least-cost path between `Harbor` and `North Gate` should be:

```
Harbor (101) -> Crossroads (310) -> Old Mine (512) -> North Gate (734)
```

You might have noticed that this one is also valid:

```
Harbor (101) -> Relay (205) -> Crossroads (310) -> Old Mine (512) -> North Gate (734)
```

But our algorithm will not return that because we first check the neighbors of `Harbor`, and assign the neighbors' `cameFrom` to `Harbor`. Since the cost to `Crossroads` is the same in the other route, then we do not update `cameFrom`.

If the start equals the goal, `FindRoute` returns `RouteResult.Success` with a route of just the start, a cost of 0 and 0 expanded nodes.

For an unreachable goal, our route finders return `RouteResult.NotFound`: `Found == false`, an empty route and a `TotalCost` of 0. It's 0 because if there is no route, there is no cost. However, a successful route where the start equals the goal also has a `TotalCost` of 0. So a 0 alone doesn't tell you whether a route was found, and code that calls `FindRoute` has to check `Found` first.

### Travel-budget query

We return the whole `bestCost` dictionary instead of the `RouteResult`, as that will be the list of all the nodes and the lowest cost to reach each of them. We only need a list of all possible nodes we can travel to, not where they came from, so we only need the `bestCost` dictionary.

A neighbour is only added to `bestCost` and the frontier if `currentCost <= maxTravelCost`. We sum all the travel costs along the route so far, plus the cost of reaching a neighbor. If the total cost to get to the neighbor is at or below `maxTravelCost`, it gets added to the frontier so we can check its neighbors.

When the query finds a route to a node, it stores the `bestCost` and puts it in the frontier. If the query later finds a lower cost path to it, it overwrites the cost to that node, and the previous frontier entry will get skipped in the stale check. A more expensive route will not even overwrite the cost.

With a budget of zero, it handles the current node and any neighbor with zero travel cost.

Since our `666` node is disconnected, if we start there, we should see that the result contains itself, and no other key.

A start ID that doesn't exist is a null reference, so `FindReachable` throws a `NullReferenceException`. `FindRoute` might have nodes that exist; it's just that there is no path between nodes that connect the start node and the goal node.

## 4. Complexity

| Operation | Your representation/algorithm | Complexity | Short explanation |
| --- | --- | --- | --- |
| Find location by ID | `Dictionary<int, Location?>` | O(1) on average | Dictionary lookup. Doesn't grow with the world size, at the cost of keeping the dictionary in memory. |
| Get outgoing connections | `Dictionary<int, List<Connection>>` | O(1) on average to find the list, then linear in the connections for that one location | Finding the list of neighbors is a dictionary lookup. Going through all the neighbors is linear. |
| Priority queue enqueue | Binary min-heap stored in a `List<Entry>` | O(log n) | `SiftUp` can move a new smallest entry all the way up to the root. |
| Priority queue dequeue-min | Binary min-heap stored in a `List<Entry>` | O(log n) | `SiftDown` can move the swapped entry from the root all the way down. |
| Route finding | Dijkstra with my `MinPriorityQueue`, skipping stale entries | O(E log V) | Up to about E entries go through the heap, each costing O(log) of the heap size. |
| Reachability query | Same loop as Dijkstra, limited by the travel budget | O(E log V) | Same as Dijkstra, but only for locations + neighbors within the budget. |

**Find location by ID / Get outgoing connections**

On average it will be O(1). When the hashing has put more than one element in the same bucket, we need to get the right element within that bucket. O(1) is not the cost; that is just how the cost of the operation will grow as we scale, which basically means the cost will be the same (but not zero). Same for `FindOutgoingConnections` here: there is a constant cost to building, keeping, hashing and other similar stuff.

**Priority queue**

- `Enqueue` is O(log n). Adding the entry to the end of the list is O(1), but then `SiftUp` runs. In the worst case a new smallest entry travels all the way up to the root, and a complete binary tree with n entries is about log₂ n levels tall.
- `Dequeue` is O(log n), because `SiftDown` can move the swapped entry from the root all the way down.
- `Peek` is O(1), since the smallest entry is always at index 0.

**Dijkstra**

With V locations and E connections: we don't remove stale entries from the heap, we skip them. Each improvement on a connection can trigger one `Enqueue`, so there can be up to about E entries over the whole search, and each of them is later dequeued (expanded or skipped as stale). E enqueues and E dequeues, each costing O(log of the heap size), gives O(E log V).

Strictly, the heap can hold up to E entries, so each operation is O(log E). But E is at most V², so log E ≤ 2 log V, which is the same in Big-O. It's often written as O((V + E) log V).

**Reachability query**

Same as Dijkstra, O(E log V), with the caveat that it is only for locations + neighbors within the budget.

## 5. Correctness and testing

### Supplied tests

| Test | Expected behaviour | Observed result |
| --- | --- | --- |
| `FindLocationById_ReturnsMatchingLocation` | ID 734 returns `North Gate`. | Pass |
| `FindLocationById_ReturnsNullWhenMissing` | Unknown ID 9999 returns `null`. | Pass |
| `FindOutgoingConnections_ReturnsOnlyMatchingOrigin` | 734 has 3 outgoing connections, all from 734. | Pass |
| `PriorityQueue_EnqueueAndDequeue_ReturnsLowestPriorityFirst` | After enqueueing 9, 2 and 5, `Dequeue` returns the priority-2 item. | Pass |
| `Dijkstra_StartEqualsGoal_ReturnsZeroCostRoute` | `FindRoute(0, 0)` is found, costs 0, and the route is `[0]`. | Pass |
| `Reachability_ZeroBudget_IncludesStartAtZeroCost` | `FindReachable(0, 0)` contains the start at cost 0. | Pass |

### My tests

| Test | Expected behaviour | Observed result |
| --- | --- | --- |
| `PriorityQueue_DequeueHandlesOneElement` | Dequeue can handle a single element in the queue. | Pass |
| `PriorityQueue_DequeueHandlesEmptyQueue` | Dequeue can handle an empty queue (throws `InvalidOperationException`). | Pass |
| `PriorityQueue_MultipleDequeue_ReturnsLowestPriorityFirst` | Enqueueing and dequeueing mixed together, to check that the heap still gives back the lowest priority after it has been changed a few times. | Pass |
| `PriorityQueue_HandlesDuplicatePriorities` | Two items with the same priority, to check that the heap handles a tie. | Pass |
| `Dijkstra_ReturnsLowestCostPath` | `101 → 310 → 512 → 734`, cost 14. This test demonstrates the lowest cost path doesn't necessarily mean the one with fewest nodes. | Pass |
| `Dijkstra_HandlesImpossiblePath` | `Hell (666)` is not connected to the rest of the world, so `101 → 666` is not found. | Pass |
| `Reachability_BudgetJustBelowPathCost` | With a budget of 13 from `Harbor (101)`, `North Gate (734)` is not included. | Pass |
| `Reachability_BudgetIsExactlyPathCost` | With a budget of 14 from `Harbor (101)`, `North Gate (734)` is included. | Pass |
| `Reachability_HandlesDisconnectedNode` | Since our `666` node is disconnected, if we start there, the result contains itself and not `Harbor (101)`. | Pass |
| `Reachability_ZeroBudget_HandlesZeroEdgeConnections` | Starting from `Hell (666)` with a budget of 0, we should still reach `Persephone's Garden (999)`. | Pass |

### Several ways of reaching the same location

`Dijkstra_ReturnsLowestCostPath` is the test where a location is reached in more than one way. `North Gate (734)` is first found through `310` at cost 15, then improved to 14 through `512`. Our code overwrites `bestCost`, and the old entry is skipped as stale.

In `Reachability_BudgetIsExactlyPathCost` (budget 14), the 15 route to `734` is rejected by the budget check before it's ever stored. `734` only gets in through the 14 route. `Crossroads (310)` is reached two ways, `101 → 310` and `101 → 205 → 310`, but both cost 7, so the second route doesn't change anything.

So the "several ways of reaching the same location" requirement for reachability is covered by the tests through the budget check, not through a cheaper route replacing a more expensive one.

## 6. Performance investigation

### Workload and method

`RunBaseline()` runs 5000 iterations. Each one calls `FindLocationById` and `FindOutgoingConnections` for the ID `(i * 997) % 2500` and adds the results to a checksum.

Back in Phase 3, I revamped the results table so it timed the `WorldQueries` constructor and the queries separately. To cover everything Phase 6 asks for, I added a few more measurements to it:

- **World (`CreateMedium`)**: a timer around `WorldDatasetFactory.CreateMedium()` to check how long it takes to build our world.
- **Build (`WorldQueries` ctor)**: to see how much of the preparation comes from our indexes.
- **Preparation (world + build)**: the sum of building the world and building our indexes.
- **Allocated (build)**: an allocation reading around the constructor, to see how much memory it costs to keep our indexes.
- **Allocated (query loop)**: an allocation reading around the query loop, to know how much memory it costs to do the queries.
- **Total (prep + queries)**: to verify which implementation is quicker at achieving what we want.

To have a fair comparison, the "before" needs the exact same measurement code as the "after". If we don't have the same measurement code, then we can't trust that they are measuring the same thing. For the most effective measurements, you must keep everything other than what you measure constant.

So I used Git to make clean copies of the starter (`6f9e529`) and of V1 (`235260b`), and put the new `Program.cs` in both. I measured V1 as well to see the effects of a partial implementation (relative to ours, nothing wrong with that implementation), as V1 only has the location dictionary.

All runs used only the command given in the assignment's `ASSIGNMENT_README.md`, with the same world (`CreateMedium`), the same 5000 iterations and on the same machine:

```
dotnet run --project Standalone/src/FrontierDispatch.App -- --baseline
```

The command has no `-c` flag, so `dotnet run` builds the default Debug configuration.

### Results

| Measurement | V0: Starter | V1: Location dictionary | Current |
| --- | --- | --- | --- |
| World (CreateMedium) | 1.476 ms | 1.353 ms | 1.344 ms |
| Build (WorldQueries ctor) | 0.060 ms | 0.351 ms | 0.819 ms |
| Preparation (world + build) | 1.535 ms | 1.705 ms | 2.163 ms |
| Allocated (build) | 24 B | 215 704 B | 651 384 B |
| Queries | 451.986 ms | 400.142 ms | 0.419 ms |
| Allocated (query loop) | 440 000 B | 440 000 B | 0 B |
| Total (prep + queries) | 453.521 ms | 401.846 ms | 2.583 ms |
| Checksum | 6267100 | 6267100 | 6267100 |

The query loop number is memory used again and again on every query, while the build number is paid once. The build number also counts every byte allocated while the constructor runs, including the old arrays that get thrown away when a dictionary or list has to grow. So it is an upper bound on how much memory our indexes take, not the exact amount.

The values are averages of three runs for the starter and V1, and five runs for ours. I excluded the very first run of ours, as it looked like an outlier (3.546 ms of preparation against ~2.16 ms). All of the raw runs, and the measurements from earlier phases, are in Appendix A.

Looking at the results, this is exactly what I expected to happen.

The World (`CreateMedium`) differences are normal variance, I assume, since `CreateMedium` hasn't changed. It would require a deeper check to fully confirm.

### Before/after summary

| Operation / workload | Before | After | Expected complexity | Reason / interpretation |
| --- | --- | --- | --- | --- |
| Find location by ID | 451.986 ms (V0) | 400.142 ms (V1) | O(n) → O(1) on average | Dictionary lookup. Doesn't grow with the world size, at the cost of keeping the dictionary in memory. |
| Get outgoing connections | 400.142 ms (V1) | 0.419 ms (Current) | O(n) → O(1) on average to find the list, then linear in the connections for that one location | Finding the list of neighbors is a dictionary lookup. No new object is created during the queries. |
| Load / prepare world (Medium, supplied workload) | 1.535 ms | 2.163 ms | O(E + V) | Our implementation takes longer to build what we need once, and saves the time on the repeated lookups. |
| Supplied route workload | — | Dijkstra per route: Small 0.0033 ms, Medium 0.0075 ms, Large 1.8958 ms | O(E log V) | No before because there was no route finder. The times include building the index, not just the search. |

The query times include both lookups. V1 only changed `FindLocationById`, so V0 → V1 shows the location dictionary, and V1 → Current shows the connections dictionary.

### Allocations

The starter's `FindOutgoingConnections` created a new `List<Connection>` on every call (440 000 B ÷ 5000 calls = 88 B each). No new object is created during the queries, so that is why it shows 0 B.

V1 having the same query allocation as the starter makes sense, because V1's connection lookup is the same as the starter's. It is also interesting that in our implementation the allocation moved from the query loop to the build.

The allocation drop was just a side effect of the dictionary change. I focused on reducing the time it took to do the operations.

### Section 11 questions

**Which change had the largest measured effect, and why do you think it helped?**

Changing `FindOutgoingConnections` to a dictionary (400.142 ms → 0.419 ms), because we already had a list of all the connections for each node. Biggest time saver.

**Did any improvement trade additional memory for execution time, or vice versa?**

Yes. The tables show that dictionaries are using more memory (Allocated (build): 24 B → 651 384 B), but the reduction in time (Queries: 451.986 ms → 0.419 ms) is worth it in my opinion. The memory is allocated once during the constructor instead of spread over all the queries in the starter lookup (Allocated (query loop): 440 000 B → 0 B).

**Why is an asymptotically better approach not guaranteed to be faster for every tiny input?**

We can see this in our small worlds, where A\* isn't always faster than Dijkstra, and in the build time of `WorldQueries` (about 0.8 ms with our dictionaries against 0.06 ms for the starter).

Big-O describes how the cost grows as n gets large, so it ignores two kinds of cost that don't grow with n:

- **Constant factors**: the extra work per step, like A\*'s lookups and square root.
- **Setup costs**: one-time work, like building the dictionaries.

With small inputs, building the world and the extra calculations eat up most of the time, so the asymptotically better approach can lose.

**What would you investigate next if the world became much larger?**

In this order:

1. **Unnecessary repeated operations**, like rebuilding the indexes. Every `DijkstraRouteFinder`, `AStarRouteFinder` and `ReachabilityQuery` builds its own `WorldQueries`.
2. **Search cost.** Even A\* expands thousands of nodes in Large.
3. **Memory.** The index already allocates about 650 KB for 2500 locations.

I believe those are the most expensive things for our program at the moment, in that order.

## 7. Development reflection

I started with Manhattan distance for the A\* heuristic, but it wasn't working with `SampleWorldFactory`. The heuristic is supposed to be an estimate of the shortest path possible to the goal, but there were edges that were shorter than the Manhattan distance between their two ends. So the estimate was higher than the real cost, and that is not allowed. This was caught by my `AStar_ReturnsLowestCostPath` test: instead of `101 → 310 → 512 → 734` (cost 14), A\* skipped `Old Mine (512)` and returned `101 → 310 → 734` (cost 15).

So I changed the formula to linear distance:

```c#
return (int)(MathF.Sqrt(from.Position.X * goal.Position.X + from.Position.Y * goal.Position.Y));
```

While reviewing my devlog with Claude, it pointed out that my heuristic was not actually computing the linear distance. For `Harbor (-4, -1)` → `North Gate (3, 5)`, the code takes `MathF.Sqrt(-4 * 3)`, which gives `NaN`. My formula multiplied the Xs instead of squaring their difference, added the Ys instead of squaring their difference, and the square root only covered the X part.

My first fix used `(dX)^2 + (dY)^2`. The tests still passed, but this was still wrong! When I reviewed my devlog again with Claude, I found out that in C#, `^` is not "to the power of". It is bitwise XOR. So I multiplied the deltas by themselves instead.

All tests passing with the broken heuristic was a problem. It happened because the A\* tests were only checking the route and its cost, and the search still found the same routes even when the estimate was garbage. So I added `AStar_EuclidianDistanceIsValid`, which checks a known pair: `Harbor (101)` → `North Gate (734)` is √85 ≈ 9.22, and the `(int)` cast rounds it down to 9.

## 8. Tools and AI assistance

I used Claude for:

- Extensively asking theoretical and syntax questions about C# and DSA.
- Formatting the baseline results table in `Program.cs`. I wrote the code that obtains the data.
- Generating the `SampleWorldFactory` Medium and Large worlds.
- Extending my `--pathfinding` comparison: adding the blocked-location scenarios, the check that both algorithms agree, and a single condensed results table.
- Reviewing my code and my devlogs to point out mistakes and ask me questions, so I understand my code better and how to fix it.
- Putting this report together. Claude read the assignment PDF, the clarifications, the report guidance, my code, my Git history and my devlogs, then asked me questions one section at a time. The report is made of my devlog text and my answers, with grammar fixes and some facts read directly from my code.

## 9. VG extension

### A\* and the heuristic

A\* adjusts the costs to bias the direction from the current node towards the goal node, using a heuristic. The heuristic is supposed to be an estimate of the shortest path possible to the goal. A heuristic that never overestimates the real remaining cost is called **admissible**.

Our A\* is mostly the same as our Dijkstra, with the heuristic added on top. The main difference is in the stale-entry check. In A\*, the heuristic is part of the cost, so the priority in the queue is `bestCost + heuristic`. That means the check has to compare against `bestCost + heuristic` too. If it only compared against `bestCost`, every fresh entry would look stale and get skipped.

The heuristic is the Euclidean distance between the location and the goal:

```c#
public static int Euclidian(Location? from, Location goal)
{
    var dX = from.Position.X - goal.Position.X;
    var dY = from.Position.Y - goal.Position.Y;

    return (int)(MathF.Sqrt(dX*dX + dY*dY));
}
```

Since the heuristic works with the location coordinates, I also changed `WorldPosition` from `float` to `int`, to match the type in my code, and because `SampleWorldFactory` didn't need floats anyway.

### Is the heuristic safe?

Squaring any number makes it non-negative, and the square and square root of zero is zero. So the heuristic is never negative, and it is 0 at the goal.

The `(int)` cast rounds down, so it can only make the estimate smaller, never bigger. So the rounding itself can never make the heuristic overestimate. But rounding down alone doesn't make the heuristic admissible: that depends on the travel costs in each world.

In `CreateMedium` and `CreateLarge`, `AddRoad` sets each road's cost to its straight-line length rounded up, plus any extra cost. Our Euclidean distance *is* the straight-line length, and the straight line between two points is always shorter than (or at best equal to) any other path. So for any route to the goal:

`route cost ≥ total length of its roads ≥ straight-line distance to the goal ≥ our rounded-down heuristic`

That means the heuristic never overestimates in those two worlds.

`CreateSmall` is different, because I set its costs by hand, and some roads cost less than their straight-line length:

| Road | Cost | Straight-line length |
| --- | --- | --- |
| 101–205 | 4 | 4.24 |
| 512–734 | 3 | 3.16 |
| 666–999 | 0 | 1 |

So the argument above doesn't really apply to this world. Instead, the heuristic was checked for the routes we test there (Claude checked every pair of locations): it never overestimates for any goal in the main group of locations, including `North Gate (734)`. The only place it overestimates is between `Hell (666)` and `Persephone's Garden (999)`, where the heuristic says 1 and the real cost is 0. The only A\* test that uses one of them is `AStar_HandlesImpossiblePath`, which goes from `Harbor (101)` to `Hell (666)`. 666 can't be reached from 101 at all, so the overestimate never affects a result.

### Traversal constraint

The traversal constraint is a list of locations that the query is not allowed to go through. I added it as an optional `blocked` parameter to both A\* and Dijkstra:

```c#
public RouteResult FindRoute(int startLocationId, int goalLocationId, List<int> blocked = null)
```

It is checked in two places:

- **Right after dequeuing a node**: this is for the start node, in case the start itself is blocked.
- **When looking at each neighbour**: if the neighbour is blocked, we skip it and never add it to the frontier.

### Correctness

| Test | Expected behaviour | Observed result |
| --- | --- | --- |
| `Dijkstra_BlockedFirstNeighbour_StillTriesLaterNeighbours` | If `Old Mine (512)` is blocked, the cheapest route from `Harbor (101)` to `North Gate (734)` should be `101 → 310 → 734`, with a cost of 15. | Pass |
| `Dijkstra_BlockedAllStartNeighbours_ReturnsNotFound` | With `205` and `310` blocked, `101 → 734` is not found and the route is empty. | Pass |
| `AStar_StartEqualsGoal_ReturnsZeroCostRoute` | `FindRoute(0, 0)` is found, costs 0, and the route is `[0]`. | Pass |
| `AStar_ReturnsLowestCostPath` | `101 → 310 → 512 → 734`, cost 14. The one that caught the Manhattan problem. | Pass |
| `AStar_HandlesImpossiblePath` | `Hell (666)` is not connected to the rest of the world, so `101 → 666` is not found. | Pass |
| `AStar_BlockedFirstNeighbour_StillTriesLaterNeighbours` | Same as Dijkstra: with `512` blocked, `101 → 310 → 734`, cost 15. | Pass |
| `AStar_EuclidianDistanceIsValid` | `Harbor (101)` → `North Gate (734)` is √85 ≈ 9.22, and the `(int)` cast rounds it down to 9. | Pass |
| `AStar_BlockedAllStartNeighbours_ReturnsNotFound` | Same as Dijkstra: with `205` and `310` blocked, `101 → 734` is not found and the route is empty. | Pass |

### Scenarios

I report my own scenarios because I had already implemented them before the clarification to use `WorldDatasetFactory`. My Small scenarios line up with the shared ones on `WorldDatasetFactory.CreateSmall()`:

| Shared scenario (`WorldDatasetFactory.CreateSmall()`, 0 → 4) | My scenario (`SampleWorldFactory.CreateSmall()`, 101 → 734) | What it checks |
| --- | --- | --- |
| None | None | Both algorithms find a cheapest route. |
| `{2}` | `{512}` | Blocks a location on the cheapest route. |
| `{1, 5}` | `{205, 310}` | Blocks every neighbour of the start. |

I added the other worlds because I wanted a more extensive picture of the difference between algorithms, and it was easy to extend it. In Large, `Harbor (101)` sits at column 2, row 2, and `North Gate (734)` at column 47, row 47. The wall runs down column 25 from row 0 to row 44, and the only gap is at rows 45–49. The full wall runs from row 0 to row 49.

### Dijkstra vs A\*

Route 101 → 734, 5000 runs per algorithm per scenario. World sizes: Small 8, Medium 20, Large 2 500 locations. Run with `dotnet run --project Standalone/src/FrontierDispatch.App -- --pathfinding`.

| World | Blocked | Found D / A\* | Cost D / A\* | Expanded D / A\* | Avg ms D / A\* | Agree |
| --- | --- | :-: | --: | --: | --: | :-: |
| Small | None | Yes / Yes | 14 / 14 | 6 / 5 | 0.0033 / 0.0026 | Yes |
| Small | {512} | Yes / Yes | 15 / 15 | 5 / 5 | 0.0021 / 0.0022 | Yes |
| Small | {205, 310} | No / No | 0 / 0 | 1 / 1 | 0.0011 / 0.0011 | Yes |
| Medium | None | Yes / Yes | 55 / 55 | 20 / 13 | 0.0075 / 0.0080 | Yes |
| Medium | {205} | Yes / Yes | 61 / 61 | 19 / 10 | 0.0074 / 0.0075 | Yes |
| Medium | {512, 620, 910} | No / No | 0 / 0 | 16 / 16 | 0.0097 / 0.0123 | Yes |
| Large | None | Yes / Yes | 686 / 686 | 2482 / 554 | 1.8958 / 0.9593 | Yes |
| Large | Wall with gap | Yes / Yes | 856 / 856 | 1770 / 1227 | 1.5400 / 1.5690 | Yes |
| Large | Full wall | No / No | 0 / 0 | 1250 / 1250 | 1.2630 / 1.5683 | Yes |

Expanded = nodes expanded per route. Agree = same Found, same cost, and same summed cost over all runs. Absolute time varies by machine. Unreachable routes report a total cost of 0.

This matches our expectation that A\* will cut on unnecessary node searches and be directed to the goal at a faster and more efficient pace than Dijkstra would, which also explains the lower quantity of nodes expanded.

In Medium with no blocking, A\* expands 13 nodes against Dijkstra's 20, but it takes longer per route (0.0080 ms against 0.0075 ms). That's because A\* calculates the heuristic for each node, which Dijkstra doesn't do at all. `Heuristics.Euclidian` is called in two places: in the stale check after each dequeue, and when adding a neighbour to the frontier with its priority. Each call first needs the node's `Location` from `FindLocationById`. A\* does up to four of these lookups plus a square root for every node it expands. In a small world where A\* only saves a few nodes, that small extra cost per node adds up to more than it saves.

Inside each timed loop, we create a new `DijkstraRouteFinder` or `AStarRouteFinder` on every run, and its constructor runs `new WorldQueries(world)`, which builds both dictionaries again from scratch. So "Avg ms" includes building the index, not just the search. This shouldn't make one algorithm better than the other, because both rebuild the same index for the same world. But it does hide the difference between them in small worlds, where the rebuild is a big part of every run.

In all three unreachable cases, both algorithms expand exactly the same number of nodes (1 / 1, 16 / 16 and 1250 / 1250). When there is no route, A\* keeps trying paths until there is no node left to search, just like Dijkstra. Only an empty frontier proves that there is no route, and the heuristic only changes the *order* in which nodes are expanded, not *which* nodes. So both end up expanding the same set.

The heuristic only knows the straight-line distance to North Gate. It knows nothing about blocked locations. So it pulls A\* towards North Gate, and when it gets to column 25, it has to search around the wall until it finds the gap. All those nodes along the wall look promising but lead nowhere. That's why A\* goes from 554 to 1227 expanded nodes with the wall, while Dijkstra goes from 2482 to 1770.

## Appendix A – Baseline measurement history

All runs were made on the same machine with the supplied `--baseline` workload: the Medium world (`WorldDatasetFactory.CreateMedium()`, 2500 locations, 9800 connections) and 5000 query iterations. Every run produced the same checksum, **6267100**.

### Versions

- **V0: Starter** (`6f9e529`): linear search in both `FindLocationById` and `FindOutgoingConnections`.
- **V1: Location dictionary** (`235260b`): `FindLocationById` uses a `Dictionary`; `FindOutgoingConnections` is still a linear search.
- **V2: Both dictionaries** (`63d5edc`): both methods use a `Dictionary`.
- **V3: MinPriorityQueue implemented** (`7e91664`): same `WorldQueries` as V2.
- **Current**: the submitted version (Phases 1–5).

### Run sets

|Run set|Date|Build|Command|What is measured|
|---|---|---|---|---|
|A|2026-11-04|Debug|`dotnet run --project src/FrontierDispatch.App -- --baseline` (from `Standalone/`)|Query loop only, whole milliseconds (starter `Program.cs`)|
|B|2026-11-05|Debug|same as A|`WorldQueries` constructor ("Build") and query loop, with decimals (`Program.cs` from `4dfdf87`)|
|**C (main)**|2026-11-18|Debug|`dotnet run --project Standalone/src/FrontierDispatch.App -- --baseline` (from repo root; the command in `ASSIGNMENT_README.md`)|World generation, constructor, query loop, and allocated bytes for constructor and query loop (Phase 6 `Program.cs`, identical in all versions)|

The commands used for A, B and C have no `-c` flag, so `dotnet run` builds the default Debug configuration. Run set C is the main before/after comparison used in the report. Times should only be compared within the same run set, because the run sets use different measurement code.

### Summary

Values are averages, with the range in brackets. The column headers say how many runs each average uses.

**Run set A (Debug)**

|Version|Queries|
|---|---|
|V0|424 ms (419–428)|
|V1|373 ms (372–373)|
|V2|0 ms (below the 1 ms resolution)|

**Run set B (Debug)**

|Version|Build (WorldQueries ctor)|Queries|Total (build + queries)|
|---|---|---|---|
|V0|0.062 ms (0.057–0.067)|422.950 ms (420.791–424.707)|423.011 ms (420.858–424.767)|
|V1|0.376 ms (0.363–0.391)|378.915 ms (376.570–383.135)|379.292 ms (376.934–383.509)|
|V2|0.845 ms (0.838–0.852)|0.404 ms (0.378–0.421)|1.249 ms (1.230–1.259)|
|V3|0.842 ms (0.802–0.897)|0.390 ms (0.380–0.403)|1.232 ms (1.190–1.300)|

**Run set C (Debug, README command) – main comparison**

|Measurement|V0: Starter (3 runs)|V1: Location dictionary (3 runs)|Current (5 runs, used in report)|Current (all 6 runs, for reference)|
|---|---|---|---|---|
|World (CreateMedium)|1.476 ms (1.385–1.561)|1.353 ms (1.333–1.386)|1.344 ms (1.321–1.358)|1.493 ms (1.321–2.239)|
|Build (WorldQueries ctor)|0.060 ms (0.056–0.065)|0.351 ms (0.334–0.372)|0.819 ms (0.801–0.848)|0.900 ms (0.801–1.306)|
|Preparation (world + build)|1.535 ms (1.441–1.625)|1.705 ms (1.675–1.759)|2.163 ms (2.149–2.184)|2.394 ms (2.149–3.546)|
|Allocated (build)*|24 B|215 704 B|651 384 B|651 384 B|
|Queries|451.986 ms (448.474–457.298)|400.142 ms (399.914–400.467)|0.419 ms (0.410–0.432)|0.445 ms (0.410–0.573)|
|Allocated (query loop)|440 000 B|440 000 B|0 B|0 B|
|Total (prep + queries)|453.521 ms (450.014–458.739)|401.846 ms (401.593–402.226)|2.583 ms (2.559–2.616)|2.839 ms (2.559–4.119)|
|Checksum|6267100|6267100|6267100|6267100|

The first of the six runs of the current version was much slower than the other five (preparation 3.546 ms against 2.149–2.184 ms), which is typical of the first run directly after a build. It is excluded from the "used in report" column. The column with all six runs, and the raw output of every run (including the excluded one), are kept for transparency.

* All bytes allocated during the `WorldQueries` constructor, including discarded resize arrays: an upper bound on index memory.

### Raw output

#### Run set A

##### V0: Starter (`6f9e529`)

```
Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 428 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.

Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 419 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.

Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 424 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.
```

##### V1: Location dictionary (`235260b`)

```
Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 373 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.

Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 372 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.

Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 373 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.
```

##### V2: Both dictionaries (`63d5edc`)

```
Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 0 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.

Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 0 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.

Baseline world-query workload
Locations: 2500
Connections: 9800
Iterations: 5000
Elapsed: 0 ms
Checksum: 6267100
Absolute time varies by machine. Keep this result for before/after comparison.
```

#### Run set B

##### V0: Starter (`6f9e529`)

```
┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,067 ms │
│ Queries                      │     420,791 ms │
│ Total (build + queries)      │     420,858 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,061 ms │
│ Queries                      │     424,707 ms │
│ Total (build + queries)      │     424,767 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,057 ms │
│ Queries                      │     423,352 ms │
│ Total (build + queries)      │     423,408 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
```

##### V1: Location dictionary (`235260b`)

```
┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,375 ms │
│ Queries                      │     383,135 ms │
│ Total (build + queries)      │     383,509 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,391 ms │
│ Queries                      │     377,041 ms │
│ Total (build + queries)      │     377,432 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,363 ms │
│ Queries                      │     376,570 ms │
│ Total (build + queries)      │     376,934 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
```

##### V2: Both dictionaries (`63d5edc`)

```
┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,852 ms │
│ Queries                      │       0,378 ms │
│ Total (build + queries)      │       1,230 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,845 ms │
│ Queries                      │       0,414 ms │
│ Total (build + queries)      │       1,259 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ Build (WorldQueries ctor)    │       0,838 ms │
│ Queries                      │       0,421 ms │
│ Total (build + queries)      │       1,258 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
```

##### V3: MinPriorityQueue Implemented (`7e91664`)

```
┌──────────────────────────────┬────────────────┐  
│ Baseline workload            │          Value │  
├──────────────────────────────┼────────────────┤  
│ Locations                    │           2500 │  
│ Connections                  │           9800 │  
│ Iterations                   │           5000 │  
├──────────────────────────────┼────────────────┤  
│ Build (WorldQueries ctor)    │       0,802 ms │  
│ Queries                      │       0,388 ms │  
│ Total (build + queries)      │       1,190 ms │  
├──────────────────────────────┼────────────────┤  
│ Checksum                     │        6267100 │  
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐  
│ Baseline workload            │          Value │  
├──────────────────────────────┼────────────────┤  
│ Locations                    │           2500 │  
│ Connections                  │           9800 │  
│ Iterations                   │           5000 │  
├──────────────────────────────┼────────────────┤  
│ Build (WorldQueries ctor)    │       0,828 ms │  
│ Queries                      │       0,380 ms │  
│ Total (build + queries)      │       1,207 ms │  
├──────────────────────────────┼────────────────┤  
│ Checksum                     │        6267100 │  
└──────────────────────────────┴────────────────┘

┌──────────────────────────────┬────────────────┐  
│ Baseline workload            │          Value │  
├──────────────────────────────┼────────────────┤  
│ Locations                    │           2500 │  
│ Connections                  │           9800 │  
│ Iterations                   │           5000 │  
├──────────────────────────────┼────────────────┤  
│ Build (WorldQueries ctor)    │       0,897 ms │  
│ Queries                      │       0,403 ms │  
│ Total (build + queries)      │       1,300 ms │  
├──────────────────────────────┼────────────────┤  
│ Checksum                     │        6267100 │  
└──────────────────────────────┴────────────────┘
```

#### Run set C (Debug, README command)

##### V0: Starter (`6f9e529`)

```
┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,385 ms │
│ Build (WorldQueries ctor)    │       0,056 ms │
│ Preparation (world + build)  │       1,441 ms │
│ Allocated (build)*           │           24 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │     457,298 ms │
│ Allocated (query loop)       │      440 000 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │     458,739 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,482 ms │
│ Build (WorldQueries ctor)    │       0,058 ms │
│ Preparation (world + build)  │       1,539 ms │
│ Allocated (build)*           │           24 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │     448,474 ms │
│ Allocated (query loop)       │      440 000 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │     450,014 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,561 ms │
│ Build (WorldQueries ctor)    │       0,065 ms │
│ Preparation (world + build)  │       1,625 ms │
│ Allocated (build)*           │           24 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │     450,185 ms │
│ Allocated (query loop)       │      440 000 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │     451,810 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.
```

##### V1: Location dictionary (`235260b`)

```
┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,386 ms │
│ Build (WorldQueries ctor)    │       0,372 ms │
│ Preparation (world + build)  │       1,759 ms │
│ Allocated (build)*           │      215 704 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │     400,467 ms │
│ Allocated (query loop)       │      440 000 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │     402,226 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,341 ms │
│ Build (WorldQueries ctor)    │       0,334 ms │
│ Preparation (world + build)  │       1,675 ms │
│ Allocated (build)*           │      215 704 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │     400,045 ms │
│ Allocated (query loop)       │      440 000 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │     401,720 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,333 ms │
│ Build (WorldQueries ctor)    │       0,346 ms │
│ Preparation (world + build)  │       1,680 ms │
│ Allocated (build)*           │      215 704 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │     399,914 ms │
│ Allocated (query loop)       │      440 000 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │     401,593 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.
```

##### Current

```
// Run 1 – excluded from the averages used in the report (first run after a build)
┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       2,239 ms │
│ Build (WorldQueries ctor)    │       1,306 ms │
│ Preparation (world + build)  │       3,546 ms │
│ Allocated (build)*           │      651 384 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │       0,573 ms │
│ Allocated (query loop)       │            0 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │       4,119 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,358 ms │
│ Build (WorldQueries ctor)    │       0,801 ms │
│ Preparation (world + build)  │       2,159 ms │
│ Allocated (build)*           │      651 384 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │       0,419 ms │
│ Allocated (query loop)       │            0 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │       2,578 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,350 ms │
│ Build (WorldQueries ctor)    │       0,817 ms │
│ Preparation (world + build)  │       2,167 ms │
│ Allocated (build)*           │      651 384 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │       0,416 ms │
│ Allocated (query loop)       │            0 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │       2,583 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,355 ms │
│ Build (WorldQueries ctor)    │       0,802 ms │
│ Preparation (world + build)  │       2,157 ms │
│ Allocated (build)*           │      651 384 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │       0,420 ms │
│ Allocated (query loop)       │            0 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │       2,577 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,321 ms │
│ Build (WorldQueries ctor)    │       0,828 ms │
│ Preparation (world + build)  │       2,149 ms │
│ Allocated (build)*           │      651 384 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │       0,410 ms │
│ Allocated (query loop)       │            0 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │       2,559 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.

┌──────────────────────────────┬────────────────┐
│ Baseline workload            │          Value │
├──────────────────────────────┼────────────────┤
│ Locations                    │           2500 │
│ Connections                  │           9800 │
│ Iterations                   │           5000 │
├──────────────────────────────┼────────────────┤
│ World (CreateMedium)         │       1,336 ms │
│ Build (WorldQueries ctor)    │       0,848 ms │
│ Preparation (world + build)  │       2,184 ms │
│ Allocated (build)*           │      651 384 B │
├──────────────────────────────┼────────────────┤
│ Queries                      │       0,432 ms │
│ Allocated (query loop)       │            0 B │
├──────────────────────────────┼────────────────┤
│ Total (prep + queries)       │       2,616 ms │
├──────────────────────────────┼────────────────┤
│ Checksum                     │        6267100 │
└──────────────────────────────┴────────────────┘
* All bytes allocated during the WorldQueries constructor, incl. discarded resize arrays: an upper bound on index memory.
Absolute time varies by machine. Keep this result for before/after comparison.
```