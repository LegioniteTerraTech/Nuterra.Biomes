using System;
using System.Reflection;
using Newtonsoft.Json;
using Nuterra.ModLoading;
using Nuterra.World.Scenery;
using UnityEngine;

namespace Nuterra.World.Scenery
{
    public class CustomScenery : ModLoadable<ManModScenery, SceneryTypes, CustomScenery>
    {
        [JsonIgnore]
        internal TerrainObject prefab;
        [JsonIgnore]
        internal string ID
        {
            get => fileName;
            set => fileName = value;
        }

        /// <summary> <b>THIS IS NOT THE <c>sceneryName</c> identifier!!!</b> <c>sceneryName</c> is <see cref="ID"/> </summary>
        [Doc("The name of the scenery. Note the filename will be the actual ID of the scenery")]
        public string Name = "Barry";
        [Doc("The description to display for the scenery")]
        public string Description = "A basic tree scenery";
        [Doc("The png to use as this item's lookup icon")]
        public string Icon = "BarryMesh";
        [Doc("The ingame Prefab to use for this. See the _Export folder for exported Scenery for more details.")]
        public string PrefabName = "Biome7Tree05";
        [Doc("The custom mesh name to use for the full HP visual stage")]
        public string MeshName = "BarryMesh";
        [Doc("The custom texture/material to use for ALL stages")]
        public string TextureName = "BarryTex";
        [Doc("The expected spherical radius this takes up for pathfinding purposes")]
        public float GroundRadius = 2.5f;
        [Doc("The minimum random height this resource node should be placed at.  Does not need to intersect ground.")]
        public float MinHeightOffset = 0;
        [Doc("The maximum random height this resource node should be placed at.  Does not need to intersect ground.")]
        public float MaxHeightOffset = 0;
        [Doc("The total health of this scenery node.  Damage stages are automatically evenly spaced within this value.  Any values below or equal to zero will set this to invulnerable.")]
        public float Health = 50;
        [Doc("Damageable type this has")]
        public ManDamage.DamageableType DamageableType = ManDamage.DamageableType.Wood;
        [Doc("If this can respawn")]
        public bool CanRespawn = false;
        [Doc("If this attacks any Techs.  Note some high types of Advanced AI can target and shoot hostile flora!")]
        public bool Hostile = false;
        [Doc("If this can move some distance away from it's spawn point like fauna")]
        public bool Roving = false;
        [Doc("Hide this if the player's own tech is obscured by this")]
        public bool FadeOnObscure = false;

        [Doc("Animation played when damaged")]
        public ManSceneryAnimation.AnimTypes DamagedAnimation = ManSceneryAnimation.AnimTypes.Shake;
        [Doc("Animation played when destroyed")]
        public ManSceneryAnimation.AnimTypes DeathAnimation = ManSceneryAnimation.AnimTypes.Topple;
        [Doc("Animation played when regrowing")]
        public ManSceneryAnimation.AnimTypes RegrowAnimation = ManSceneryAnimation.AnimTypes.Regrow;
        [Doc("Auto to play when regrowing")]
        public string SFXName = null;

        //DO THESE LATER
        /*
        private static readonly FieldInfo m_HitPrefab = typeof(ResourceDispenser).GetField("m_HitPrefab", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo m_DebrisPrefab = typeof(ResourceDispenser).GetField("m_DebrisPrefab", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo m_BigDebrisPrefab = typeof(ResourceDispenser).GetField("m_BigDebrisPrefab", BindingFlags.NonPublic | BindingFlags.Static);
        //*/
        public Stage[] Stages = new Stage[1]
            {
                new Stage()
                {
                    MeshName = "Barry1",
                    IsInvulnerable = false,
                    StageHealth = 500,
                    ForceUpright = true,
                    DoesNotObstructSpawning = true,
                },
            };
        [Doc("Total Chunks this will dispense when attacked")]
        public int AttackedChunks = 0;
        [Doc("The speed of launched chunks")]
        public float SpawnVelocity = 26f;
        [Doc("The offset where chunks shall spawn from")]
        public Vector3 SpawnOffset = Vector3.up;
        [Doc("The random radius offset where chunks shall spawn from")]
        public float SpawnRandomOffset = 0.5f;
        [Doc("The scalar range of random where chunks shall be launched in")]
        public Vector3 SpawnRandomDirection = Vector3.up;
        [Doc("The scalar range of random where chunks shall spin while launched")]
        public Vector3 SpawnRandomRotation = Vector3.up;
        [Doc("Each one of these will be selected at random")]
        public ChunkSpawn[] ChunkSpawnWeights = new ChunkSpawn[1]
            {
                new ChunkSpawn()
                {
                    ChunkID = "Terry",
                    SpawnWeight = 60,
                },
            };


        /// <inheritdoc/>
        [JsonConstructor]
        [Obsolete("ONLY FOR JSON SERIALIZATION")]
        public CustomScenery() : base() { }
        /// <inheritdoc/>
        public CustomScenery(string ID) : base(ID)
        {
        }

        [Serializable]
        public struct Stage
        {
            [Doc("The custom mesh name to use for this stage. Texture is set in CustomScenery.TextureName")]
            public string MeshName;
            [Doc("The health this stage has until it moves to the next stage below this one (integer increments by +1)")]
            public float StageHealth;
            [Doc("When this stage is reached, it will no longer take damage or be destructable (except by ModuleScoop in RandomAdditions)")]
            public bool IsInvulnerable;
            [Doc("When this stage is reached, it forces itself upright")]
            public bool ForceUpright;
            [Doc("When this stage is reached, it will no longer obstrust spawning of things, like tech swapping")]
            public bool DoesNotObstructSpawning;
        }
        [Serializable]
        public struct ChunkSpawn
        {
            [Doc("The chunk name according to ChunkTypes for vanilla or CustomChunk.ID (filename sans .json) for modded")]
            public string ChunkID;
            [Doc("Chance this will be spawned versus other options declared in CustomScenery.ChunkSpawnWeights")]
            public int SpawnWeight;
        }
    }

}
