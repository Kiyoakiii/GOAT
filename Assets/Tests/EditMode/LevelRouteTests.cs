using System;
using GoatDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace GoatDescent.Tests
{
    public sealed class LevelRouteTests
    {
        private LevelRecipe recipe;
        private BiomeProfile biome;
        [SetUp]
        public void SetUp()
        {
            recipe = ScriptableObject.CreateInstance<LevelRecipe>();
            biome = ScriptableObject.CreateInstance<BiomeProfile>();
            recipe.Configure(7, biome);
        }
        [TearDown]
        public void TearDown() { UnityEngine.Object.DestroyImmediate(recipe); UnityEngine.Object.DestroyImmediate(biome); }

        [TestCase(7)] [TestCase(21)] [TestCase(33)] [TestCase(28471)] [TestCase(847291)]
        public void RouteHasReachableTransitionsAndCheckpoints(int seed)
        {
            recipe.Configure(seed, biome);
            var route = LevelRouteGenerator.Generate(recipe);
            Assert.That(LevelRouteValidator.Validate(route), Is.Empty);
            Assert.That(route.Platforms.Length, Is.GreaterThan(100));
            Assert.That(route.Platforms[0].IsCheckpoint, Is.True);
            Assert.That(route.Platforms[route.Platforms.Length - 1].Progress, Is.EqualTo(1f));
            Assert.That(route.TargetSeconds, Is.InRange(300f, 480f));
            int lastCheckpoint = 0, jumps = 0;
            for (int i = 0; i < route.Platforms.Length; i++)
            {
                if (route.Platforms[i].IsCheckpoint) lastCheckpoint = i;
                Assert.That(i - lastCheckpoint, Is.LessThanOrEqualTo(10));
                if (i < route.Connections.Length && route.Connections[i] == RouteConnection.Jump) jumps++;
            }
            Assert.That(jumps, Is.GreaterThan(20));
        }

        [Test]
        public void RouteChangesDirectionAndTraversesSeveralRockFaces()
        {
            var route = LevelRouteGenerator.Generate(recipe);
            int turns = 0;
            float previousDirection = 0f;
            for (int i = 1; i < route.Platforms.Length; i++)
            {
                float dx = route.Platforms[i].Center.x - route.Platforms[i - 1].Center.x;
                if (Mathf.Abs(dx) < .01f) continue;
                if (previousDirection != 0f && Mathf.Sign(dx) != previousDirection) turns++;
                previousDirection = Mathf.Sign(dx);
            }
            Assert.That(turns, Is.GreaterThanOrEqualTo(4));
            Assert.That(route.Sections[route.Sections.Length - 1], Is.GreaterThanOrEqualTo(4));
            Assert.That(route.Finish.z, Is.LessThan(route.Spawn.z - 80f));
        }

        [Test]
        public void ClimbingEpisodeContainsShortAscents()
        {
            var route = LevelRouteGenerator.Generate(recipe);
            int ascents = 0;
            for (int i = 1; i < route.Platforms.Length; i++)
                if (recipe.Episodes[route.Episodes[i]].Challenge == LevelChallenge.Climbing
                    && route.Platforms[i].Center.y > route.Platforms[i - 1].Center.y) ascents++;
            Assert.That(ascents, Is.GreaterThanOrEqualTo(4));
        }

        [TestCase(80f, 100f)]
        [TestCase(300f, 400f)]
        public void RecipeSizeExtremesKeepTransitionsReachable(float radius, float height)
        {
            JsonUtility.FromJsonOverwrite("{\"radius\":" + radius + ",\"summitHeight\":" + height + "}", recipe);
            Assert.That(LevelRouteValidator.Validate(LevelRouteGenerator.Generate(recipe)), Is.Empty);
        }

        [Test]
        public void EquivalentAssetsProduceSameNetworkSignature()
        {
            var otherRecipe = ScriptableObject.CreateInstance<LevelRecipe>();
            var otherBiome = ScriptableObject.CreateInstance<BiomeProfile>();
            try
            {
                otherRecipe.Configure(7, otherBiome);
                var first = LevelRouteGenerator.Generate(recipe); var second = LevelRouteGenerator.Generate(otherRecipe);
                Assert.That(second.Signature, Is.EqualTo(first.Signature));
                for (int i = 0; i < first.Platforms.Length; i++)
                    Assert.That(second.Platforms[i].Center, Is.EqualTo(first.Platforms[i].Center));
            }
            finally { UnityEngine.Object.DestroyImmediate(otherRecipe); UnityEngine.Object.DestroyImmediate(otherBiome); }
        }

        [Test]
        public void BiomeDensityDoesNotMoveRoute()
        {
            var first = LevelRouteGenerator.Generate(recipe);
            JsonUtility.FromJsonOverwrite("{\"treeDensity\":1.5}", biome);
            var second = LevelRouteGenerator.Generate(recipe);
            for (int i = 0; i < first.Platforms.Length; i++) Assert.That(second.Platforms[i].Center, Is.EqualTo(first.Platforms[i].Center));
            Assert.That(second.Signature, Is.Not.EqualTo(first.Signature));
        }

        [Test]
        public void ChangingSeedChangesRouteAndSignature()
        {
            var first = LevelRouteGenerator.Generate(recipe);
            recipe.Configure(21, biome);
            var second = LevelRouteGenerator.Generate(recipe);
            Assert.That(first.Signature, Is.Not.EqualTo(second.Signature));
            Assert.That(first.Platforms[0].Center, Is.Not.EqualTo(second.Platforms[0].Center));
        }

        [Test]
        public void MissingEpisodesFailExplicitly()
        {
            JsonUtility.FromJsonOverwrite("{\"episodes\":[]}", recipe);
            Assert.Throws<ArgumentException>(() => LevelRouteGenerator.Generate(recipe));
        }

        [Test]
        public void UnimplementedFormFailsExplicitly()
        {
            JsonUtility.FromJsonOverwrite("{\"form\":1}", recipe);
            Assert.Throws<NotSupportedException>(() => LevelRouteGenerator.Generate(recipe));
        }
    }
}
