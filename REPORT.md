# REPORT.md - Individual Assignment

Keep this concise and specific to your implementation.

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

Describe important changes/retained choices, operations affected, and time/memory trade-offs.

## 3. Algorithms and adaptation

Explain your route finder at a useful high level and how the travel-budget query adapts the same ideas.

## 4. Complexity

| Operation | Your representation/algorithm | Complexity | Short explanation |
| --- | --- | --- | --- |
| Find location by ID | | | |
| Get outgoing connections | | | |
| Priority queue enqueue | | | |
| Priority queue dequeue-min | | | |
| Route finding | | | |

## 5. Correctness and testing

Important tests/edge cases and what they demonstrate.

## 6. Performance investigation

Record reproducible before/after measurements for loading/preparation, repeated runtime work, and relevant allocation behavior. Explain what changed and why.

## 7. Development reflection

Describe one bug, wrong assumption, failed approach, or surprising result and how you investigated it.

## 8. Tools and AI assistance

Briefly describe relevant assistance, if any.

## 9. VG extension

Only if attempting VG: heuristic, traversal constraints, correctness, and Dijkstra/A* comparison.
