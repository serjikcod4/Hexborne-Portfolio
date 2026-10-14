using Hexborne.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hexborne.Tests
{
    public sealed class SpatialGridTests
    {
        [Test]
        public void Query_ReturnsOnlyEntitiesInsideRadius()
        {
            var grid = new SpatialGrid(2f, 32, 16);
            var positions = new[] { new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), new Vector3(3.1f, 0f, 0f), new Vector3(0f, 0f, -2.9f) };
            grid.Rebuild(Vector3.zero, positions, positions.Length);

            var results = new int[8];
            int n = grid.Query(Vector3.zero, 3f, results);

            CollectionAssert.AreEquivalent(new[] { 0, 1, 3 }, Slice(results, n));
        }

        [Test]
        public void Query_IgnoresYAxis()
        {
            var grid = new SpatialGrid(2f, 32, 4);
            var positions = new[] { new Vector3(1f, 50f, 0f) };
            grid.Rebuild(Vector3.zero, positions, 1);

            Assert.AreEqual(1, grid.Query(Vector3.zero, 1.5f, new int[4]));
        }

        [Test]
        public void FindNearest_PicksClosestWithinRange()
        {
            var grid = new SpatialGrid(2f, 32, 8);
            var positions = new[] { new Vector3(5f, 0f, 0f), new Vector3(-2f, 0f, 1f), new Vector3(0f, 0f, 9f) };
            grid.Rebuild(Vector3.zero, positions, positions.Length);

            Assert.AreEqual(1, grid.FindNearest(Vector3.zero, 10f));
            Assert.AreEqual(-1, grid.FindNearest(Vector3.zero, 2f));
        }

        [Test]
        public void EntitiesOutsideGridArea_AreIgnored()
        {
            var grid = new SpatialGrid(2f, 8, 4); // covers 16 × 16 m around the centre
            var positions = new[] { new Vector3(100f, 0f, 0f), new Vector3(1f, 0f, 1f) };
            grid.Rebuild(Vector3.zero, positions, positions.Length);

            Assert.AreEqual(1, grid.FindNearest(Vector3.zero, 500f));
        }

        [Test]
        public void Rebuild_FollowsNewCentreAndCount()
        {
            var grid = new SpatialGrid(2f, 8, 4);
            var positions = new[] { new Vector3(200f, 0f, 200f), new Vector3(0f, 0f, 0f) };

            grid.Rebuild(Vector3.zero, positions, 2);
            Assert.AreEqual(-1, grid.FindNearest(new Vector3(200f, 0f, 200f), 1f));

            grid.Rebuild(new Vector3(200f, 0f, 200f), positions, 1);
            Assert.AreEqual(0, grid.FindNearest(new Vector3(200f, 0f, 200f), 1f));
        }

        [Test]
        public void Query_StopsAtBufferSize()
        {
            var grid = new SpatialGrid(2f, 16, 10);
            var positions = new Vector3[10];
            grid.Rebuild(Vector3.zero, positions, 10);

            Assert.AreEqual(3, grid.Query(Vector3.zero, 1f, new int[3]));
        }

        [Test]
        public void DensestPoint_FindsTheBiggestCluster()
        {
            var positions = new[]
            {
                new Vector3(5f, 0f, 5f), new Vector3(5.4f, 0f, 5.2f), new Vector3(4.7f, 0f, 5.5f),
                new Vector3(5.2f, 0f, 4.6f), new Vector3(4.8f, 0f, 4.9f),
                new Vector3(-3f, 0f, 0f), new Vector3(-3.3f, 0f, 0.4f)
            };
            var grid = new SpatialGrid(2f, 32, positions.Length);
            grid.Rebuild(Vector3.zero, positions, positions.Length);

            Assert.IsTrue(grid.DensestPoint(Vector3.zero, 10f, out Vector3 point));
            Assert.Less(Vector3.Distance(point, new Vector3(5f, 0f, 5f)), 1f);
        }

        [Test]
        public void DensestPoint_IgnoresEntitiesOutOfRange()
        {
            var positions = new[] { new Vector3(20f, 0f, 0f), new Vector3(20.5f, 0f, 0f), new Vector3(2f, 0f, 0f) };
            var grid = new SpatialGrid(2f, 32, positions.Length);
            grid.Rebuild(Vector3.zero, positions, positions.Length);

            Assert.IsTrue(grid.DensestPoint(Vector3.zero, 5f, out Vector3 point));
            Assert.Less(Vector3.Distance(point, new Vector3(2f, 0f, 0f)), 0.5f);
            Assert.IsFalse(grid.DensestPoint(new Vector3(-20f, 0f, -20f), 3f, out _));
        }

        private static int[] Slice(int[] source, int n)
        {
            var result = new int[n];
            System.Array.Copy(source, result, n);
            return result;
        }
    }
}
