using System;
using Newtonsoft.Json;
using Nuterra.ModLoading;
using UnityEngine;

namespace Nuterra.World.Chunks
{
    public class CustomChunk : ModLoadable<ManModChunks, ChunkTypes, CustomChunk>
    {
        [JsonIgnore]
        internal Transform prefab;
        [JsonIgnore]
        internal ResourceTable.Definition runtimePrefabBase;

        [Doc("The name of the resource chunk")]
        public string Name = "Terry";
        [Doc("The description of the resource chunk")]
        public string Description = "A basic resource chunk";
        [Doc("The png to use as this item's lookup icon")]
        public string Icon = "BarryMesh";
        [Doc("The ingame Prefab to use for this. See the _Export folder for exported Chunks for more details.")]
        public string PrefabName = ChunkTypes.Wood.ToString();
        [Doc("The custom mesh name to use for the Chunk")]
        public string MeshName = "TerryMesh";
        [Doc("The custom texture/material to use for the Chunk")]
        public string TextureName = "TerryTex";
        [Doc("The health of the chunk")]
        public float Health = 50;
        [Doc("The damageable type to use for damage recieving")]
        public ManDamage.DamageableType DamageableType = ManDamage.DamageableType.Standard;
        [Doc("The rarity of the resource")]
        public ChunkRarity Rarity = ChunkRarity.Common;
        [Doc("How heavy the Chunk is")]
        public float Mass = 0.25f;
        [Doc("How much this sells for in BB.")]
        public int Cost = 8;
        [Doc("This is a refined resource that can be unrefined to a state immedeately before this. Otherwise assumes false unless this is a component")]
        public bool IsRefined = true;
        [Doc("Set this value to flag this as a component resource that is made of multiple other chunks or maybe even blocks?")]
        public ComponentTier ComponentTier = ComponentTier.Null;
        [Doc("This is a burnable resource that can be consumed for energy in any burner generator")]
        public bool IsFuel = true;
        [Doc("How much time this takes to provide FuelEnergy.  Since the BF Fusion Generator exists, this value still matters even if IsFuel is false")]
        public float FuelTime = 1;
        [Doc("How much energy this provides over FuelTime.  Since the BF Fusion Generator exists, this value still matters even if IsFuel is false")]
        public float FuelEnergy = 1;
        [Doc("The friction of the Chunk when it is moving.")]
        public float DynamicFriction = 0.8f;
        [Doc("The friction of the Chunk when it is stationary.")]
        public float StaticFriction = 0.8f;
        [Doc("The bounciness of the Chunk.")]
        public float Restitution = 1;


        /// <inheritdoc/>
        [JsonConstructor]
        [Obsolete("ONLY FOR JSON SERIALIZATION")]
        public CustomChunk() : base() { }
        /// <inheritdoc/>
        public CustomChunk(string ID) : base(ID)
        {
        }
    }

}
