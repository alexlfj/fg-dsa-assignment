using System;
using NUnit.Framework;
using FrontierDispatch.Core;

namespace FrontierDispatch.Tests
{
    /// <summary>
    /// Small contract examples for later assignment phases.
    ///
    /// These are not an exhaustive assignment test suite.
    /// Students are expected to enable the relevant tests as they reach each
    /// phase and add meaningful tests of their own, including edge cases.
    /// </summary>
    public sealed class AssignmentStarterTests
    {
        // These tests document later assignment contracts without making the
        // untouched baseline red. Remove Ignore when you reach the named phase.

        [Test]
        public void PriorityQueue_EnqueueAndDequeue_ReturnsLowestPriorityFirst()
        {
            var queue = new MinPriorityQueue<string>();
            queue.Enqueue("expensive", 9);
            queue.Enqueue("cheap", 2);
            queue.Enqueue("medium", 5);
            Assert.That(queue.Dequeue(), Is.EqualTo("cheap"));
        }
        
        [Test]
        public void PriorityQueue_DequeueHandlesOneElement()
        {
            var queue = new MinPriorityQueue<string>();
            queue.Enqueue("expensive", 9);
            Assert.That(queue.Dequeue(), Is.EqualTo("expensive"));
        }
        
        [Test]
        public void PriorityQueue_DequeueHandlesEmptyQueue()
        {
            var queue = new MinPriorityQueue<string>();
            Assert.That(queue.Dequeue, Throws.TypeOf<InvalidOperationException>());
        }
        
        [Test]
        public void PriorityQueue_MultipleDequeue_ReturnsLowestPriorityFirst()
        {
            var queue = new MinPriorityQueue<string>();
            queue.Enqueue("cheap", 2);
            queue.Enqueue("medium", 5);
            queue.Dequeue();
            queue.Enqueue("extremely expensive", 11);
            queue.Enqueue("expensive", 9);
            queue.Dequeue();
            Assert.That(queue.Dequeue(), Is.EqualTo("expensive"));
            Assert.That(queue.Dequeue(), Is.EqualTo("extremely expensive"));
        }
        
        [Test]
        public void PriorityQueue_HandlesDuplicatePriorities()
        {
            var queue = new MinPriorityQueue<string>();
            queue.Enqueue("cheap", 2);
            queue.Enqueue("also cheap", 2);
            queue.Enqueue("medium", 5);
            Assert.That(queue.Dequeue(), Is.EqualTo("cheap"));
            Assert.That(queue.Dequeue(), Is.EqualTo("also cheap"));
        }

        [Test]
        public void Dijkstra_StartEqualsGoal_ReturnsZeroCostRoute()
        {
            var finder = new DijkstraRouteFinder(WorldDatasetFactory.CreateSmall());
            RouteResult result = finder.FindRoute(0, 0);
            
            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.EqualTo(0));
            Assert.That(result.LocationIds, Is.EqualTo(new[] { 0 }));
        }
        
        [Test]
        public void Dijkstra_ReturnsLowestCostPath()
        {
            var finder = new DijkstraRouteFinder(SampleWorldFactory.CreateSmall());
            RouteResult result = finder.FindRoute(101, 734);
            
            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.EqualTo(14));
            Assert.That(result.LocationIds, Is.EqualTo(new[] {101, 310, 512, 734}));
        }
        
        [Test]
        public void Dijkstra_HandlesImpossiblePath()
        {
            var finder = new DijkstraRouteFinder(SampleWorldFactory.CreateSmall());
            RouteResult result = finder.FindRoute(101, 666);
            
            Assert.That(result.Found, Is.False);
        }
        

        [Test]
        public void Reachability_ZeroBudget_IncludesStartAtZeroCost()
        {
            var query = new ReachabilityQuery(WorldDatasetFactory.CreateSmall());
            var result = query.FindReachable(0, 0);
            Assert.That(result.ContainsKey(0), Is.True);
            Assert.That(result[0], Is.EqualTo(0));
        }
        
        [Test]
        public void Reachability_BudgetJustBelowPathCost()
        {
            var query = new ReachabilityQuery(SampleWorldFactory.CreateSmall());
            var result = query.FindReachable(101, 13);
            Assert.That(result.ContainsKey(734), Is.False);
        }
        
        [Test]
        public void Reachability_BudgetIsExactlyPathCost()
        {
            var query = new ReachabilityQuery(SampleWorldFactory.CreateSmall());
            var result = query.FindReachable(101, 14);
            Assert.That(result.ContainsKey(734), Is.True);
        }
        
        [Test]
        public void Reachability_HandlesDisconnectedNode()
        {
            var query = new ReachabilityQuery(SampleWorldFactory.CreateSmall());
            var result = query.FindReachable(666, 0);
            Assert.That(result.ContainsKey(666), Is.True);
            Assert.That(result.ContainsKey(0), Is.False);
        }
        
        [Test]
        public void Reachability_ZeroBudget_HandlesZeroEdgeConnections()
        {
            var query = new ReachabilityQuery(SampleWorldFactory.CreateSmall());
            var result = query.FindReachable(666, 0);
            Assert.That(result.ContainsKey(999), Is.True);
        }
    }
}
