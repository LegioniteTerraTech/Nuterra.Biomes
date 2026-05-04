using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nuterra.ModLoading;
using Nuterra.World.PatchBatch;
using SafeSaves;
using TerraTechETCUtil;
using UnityEngine;
using static LocalisationEnums;

namespace Nuterra.World.Chunks
{
    /// <summary>
    /// Makes the EXISTING chunks usuable again yay
    /// </summary>
    [AutoSaveManager]
    public class ManModChunks : ModLoaderSystem<ManModChunks, ChunkTypes, CustomChunk>
    {
        public const string Tag = ".Chunks";
        public const string ModChunksName = ManModWorld.ModName + Tag;

        protected override string ourTag => Tag;
        protected override string leadingFileName { get; } = "Res_";
        public override string LogDirectoryName { get; } = "Chunks";
        [SSManagerInst]
        public static ManModChunks inst = new ManModChunks();
        public static Dictionary<int, string> modChunksModNames = new Dictionary<int, string>();

        public ManModChunks()
        {
            WikiPageChunk.GetChunkModName = ChunkModNameWrapper;
        }

        private static int DefaultChunkCount;
        public static void SanityCheck()
        {
            if (ResLook == null)
                throw new NullReferenceException(nameof(ResLook));
            if (ResLook2 == null)
                throw new NullReferenceException(nameof(ResLook2));
            if (ResRare == null)
                throw new NullReferenceException(nameof(ResRare));
            /*
            if (poolStart2 == null)
                throw new NullReferenceException(nameof(poolStart2));//*/
        }
        public static bool InitPatches = false;
        protected override void Init_Internal()
        {
        }
        public static string ChunkModNameWrapper(int CT)
        {
            if (modChunksModNames.TryGetValue(CT, out string ModName))
                return ModName;
            if (ChunkMaker.Resurrected.Contains((ChunkTypes)CT))
                return DebugWorld.ModName;
            return WikiPageChunk.GetChunkModNameDefault(CT);
        }
        public static void PrepareAllChunks(bool reload)
        {
            if (InitPatches || MassPatcher.MassPatchAllWithin(ManModWorld.harmonyInst, typeof(ChunkPatches), ManModWorld.ModName, true))
            {
                InitPatches = true;
                inst.Log("Loading all modded!");
                string path = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "Custom Chunks");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                inst.Log("Path in: " + path);
                inst.CreateAll(reload, path);
                inst.Log("finished!");
            }
            else
                inst.Error("Rebuilding lookup FAILED! " + nameof(ChunkPatches) + " didn't load!");
        }


        internal static readonly FieldInfo ResLook = typeof(StringLookup).GetField("m_ChunkNames", BindingFlags.NonPublic | BindingFlags.Static);
        internal static readonly FieldInfo ResLook2 = typeof(StringLookup).GetField("m_ChunkDescriptions", BindingFlags.NonPublic | BindingFlags.Static);
        internal static readonly FieldInfo ResRare = typeof(ResourcePickup).GetField("m_ChunkRarity", spamFlags);
        //internal static readonly MethodInfo poolStart2 = typeof(ResourcePickup).GetMethod("OnPool", spamFlags);// Already done by "Instance.CreatePool(4)"

        internal static CustomChunk ExtractFromExisting(ResourceTable.Definition objTarget)
        {
            try
            {
                return inst.ExtractFromExisting((object)objTarget);
            }
            catch (Exception e)
            {
                inst.Log("Failed to fetch " +
                    (objTarget.name.NullOrEmpty() ? "<NULL>" : objTarget.name) + " - " + e);
                return null;
            }
        }

        protected override CustomChunk ExtractFromExisting(object objTarget)
        {
            if (objTarget == null)
                throw new NullReferenceException("objTarget IS NULL");
            ResourceTable.Definition def = objTarget as ResourceTable.Definition;
            if (def == null)
                throw new NullReferenceException("ResourceTable.Definition IS NULL");
            Transform target = def.basePrefab;
            if (!target)
                throw new NullReferenceException("basePrefab IS NULL");
            Visible vis = target.GetComponent<Visible>();
            if (!vis)
                throw new NullReferenceException("visible IS NULL");
            Damageable dmg = target.GetComponent<Damageable>();
            if (!dmg)
                throw new NullReferenceException("Damageable IS NULL");
            ResourcePickup RP = target.GetComponent<ResourcePickup>();
            if (!RP)
                throw new NullReferenceException("ResourcePickup IS NULL");

            //Collider Col = target.GetComponent<Collider>();
            var CT = (ChunkTypes)vis.ItemType;
            var MR = target.GetComponentInChildren<MeshRenderer>(true);
            var MF = target.GetComponentInChildren<MeshFilter>(true);
            return new CustomChunk()
            {
                Name = target.name,
                Description = StringLookup.GetItemDescription(ObjectTypes.Chunk, vis.ItemType),
                PrefabName = target.name,
                TextureName = MR.sharedMaterial ?
                    MR.sharedMaterial.name : MR.material.name,
                MeshName = MF.sharedMesh ?
                    MF.sharedMesh.name : MF.mesh.name,
                Cost = def.saleValue,
                Health = (float)healthMain.GetValue(dmg),
                Mass = def.mass,
                Rarity = RP.ChunkRarity,
                DynamicFriction = def.frictionDynamic,
                StaticFriction = def.frictionStatic,
                Restitution = def.restitution,
                JSONData = new Dictionary<string, object>(),
            };
        }


        protected override void FinalAssignmentStarting()
        {
            if (DefaultChunkCount == 0)
                DefaultChunkCount = SpawnHelper.GetResourceChunkPrefabs().Length;
        }
        protected override void FinalAssignment(CustomChunk chunk, ChunkTypes AssignedID)
        {
            Visible vis = chunk.prefab.GetComponent<Visible>();
            int AssignedIDInt = (int)AssignedID;
            int PreviousIDInt = vis.m_ItemType.ItemType;
            if (PreviousIDInt == AssignedIDInt)
                return;
            Dictionary<int, int> IdToNameIndexLookup = (Dictionary<int, int>)ResLook.GetValue(null);
            int defRedirect = AssignedIDInt;
            if (PreviousIDInt == -1)
            {
                LocalisationExt.RegisterRawEng(StringBanks.ChunkName, AssignedIDInt, chunk.Name);
                LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, AssignedIDInt, chunk.Description);
            }
            else
                defRedirect = IdToNameIndexLookup[AssignedIDInt];
            if (PreviousIDInt != AssignedIDInt)
            { // We resync this with our new ID
                try
                {
                    modChunksModNames.Remove(PreviousIDInt);
                    IdToNameIndexLookup.Remove(PreviousIDInt);
                    chunk.prefab.DeletePool();
                }
                catch (Exception e)
                {
                    Log(typeof(ManModChunks).Name + ": Error when assigning \"" + chunk.Name + ", (" +
                        vis.m_ItemType.ItemType + ")\" to (" + AssignedIDInt + "): " + e);
                }
            }
            if (!modChunksModNames.ContainsKey(AssignedIDInt))
            {
                try
                {
                    Registered.Add(chunk.fileName, AssignedID);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModChunks).Name + ": Error when registering \"" + chunk.Name +
                        ", (" + AssignedIDInt + ")", e);
                }
                chunk.runtimePrefabBase.m_ChunkType = AssignedID;
                vis.m_ItemType = new ItemTypeInfo(ObjectTypes.Chunk, AssignedIDInt);
                try
                {
                    IdToNameIndexLookup.Add(AssignedIDInt, defRedirect);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModChunks).Name + ": Error when registering the name and description of \"" +
                        chunk.Name + ", (" + AssignedIDInt + ")", e);
                }
                try
                {
                    modChunksModNames.Add(AssignedIDInt, chunk.runtimeMod.ModID);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModChunks).Name + ": Error when registering the mod name of \"" +
                        chunk.Name + ", (" + AssignedIDInt + ")", e);
                }
                chunk.prefab.CreatePool(4);
                Log("Assigned Custom Chunk " + chunk.Name + " to ID " + AssignedIDInt);
                var group = ManIngameWiki.InsureChunksWikiGroup(chunk.runtimeMod.ModID);
                new WikiPageChunk(AssignedIDInt, group);
            }
        }

        protected override void FinalAssignmentFinished()
        {
            var Prefabs = SpawnHelper.GetResourceChunkPrefabs();
            if (Prefabs.Length != DefaultChunkCount + Registered.Count)
                Array.Resize(ref Prefabs, DefaultChunkCount + Registered.Count);
            for (int i = 0; i < Registered.Count; i++)
            {
                var pair = Active.ElementAt(i);
                Prefabs[i + DefaultChunkCount] = pair.Value.runtimePrefabBase;
            }
            SpawnHelper.OverrideResourceChunkPrefabs(Prefabs);
        }


        protected override void LoadInstanceFile(ModContainer Mod, string path, bool Reload = false)
        {
            string fileName = Path.GetFileName(path);
            string text;
            Active.TryGetValue(fileName, out CustomChunk chunk);
            if (chunk == null || Reload)
            {
                JSONConverterUniversal.Foundation = null;
                JSONConverterUniversal.CreateNew = true;
                text = File.ReadAllText(path);
                chunk = JsonConvert.DeserializeObject<CustomChunk>(text);//, new JSONConverterUniversal());
                if (chunk == null)
                    throw new NullReferenceException("Chunk file " + fileName + " is corrupted!");
                chunk.runtimeMod = Mod;
            }
            if (!Reload && chunk.prefab != null)
                return;
            Active.Remove(fileName);
            LoadInstance(Mod, fileName, chunk);
        }
        protected override void LoadInstanceAsset(ModContainer Mod, TextAsset path, bool Reload = false)
        {
            string fileName = path.name;
            string text = null;
            Active.TryGetValue(fileName, out CustomChunk chunk);
            if (chunk == null || Reload)
            {
                JSONConverterUniversal.Foundation = null;
                JSONConverterUniversal.CreateNew = true;
                text = path.text;
                chunk = JsonConvert.DeserializeObject<CustomChunk>(text, new JSONConverterUniversal());
                if (chunk == null)
                    throw new NullReferenceException("Chunk file " + fileName + " is corrupted!");
                chunk.runtimeMod = Mod;
            }
            if (!Reload && chunk.prefab != null)
                return;
            Active.Remove(fileName);
            LoadInstance(Mod, fileName, chunk);
        }
        /// <inheritdoc/>
        protected override void LoadInstance(ModContainer Mod, string ID, CustomChunk chunk)
        {
            if (ResourceManager.inst == null)
                throw new NullReferenceException("ResourceManager.inst is NULL - cannot continue!");
            if (Mod == null)
                throw new NullReferenceException("Mod is NULL - cannot continue!");
            if (ID.NullOrEmpty())
                throw new NullReferenceException("ID is NULL - cannot continue!");
            if (chunk == null)
                throw new NullReferenceException("CustomChunk is NULL - cannot continue!");
            var Prefabs = SpawnHelper.GetResourceChunkPrefabs();
            if (Prefabs == null)
                throw new NullReferenceException("Chunk lookup is NULL - cannot continue!");
            ChunkTypes prefabType;
            if (chunk.PrefabName == null)
            {
                throw new NullReferenceException("Chunk PrefabName for file " + ID + " does not exists!" +
                    "  A chunk NEEDS a valid prefab to exist!");
            }
            else if (chunk.Mass <= 0)
            {
                throw new NullReferenceException("Chunk PrefabName \"" + chunk.PrefabName +
                    "\" cannot have Mass of " + chunk.Mass.ToString() + ", " +
                    "  A chunk NEEDS Mass greater than 0!");
            }
            else if (!EnumTryGetTypeFlexable(chunk.PrefabName, out prefabType))
                prefabType = ChunkTypes.Wood;
            if (Prefabs.FirstOrDefault(x => x?.basePrefab != null && x.m_ChunkType == prefabType)
                is ResourceTable.Definition PrefabInner && PrefabInner != null)
            {
                Transform Prefab = PrefabInner.basePrefab;
                if (Prefab == null)
                    throw new NullReferenceException("Prefab is null");
                Transform Instance = UnityEngine.Object.Instantiate(Prefab, null);
                if (Instance == null)
                    throw new NullReferenceException("Instance is null");

                int failPoint = 0;
                try
                {
                    chunk.fileName = ID;
                    failPoint++;
                    chunk.runtimeMod = Mod;
                    failPoint++;
                    Visible vis = Instance.GetComponent<Visible>();
                    if (!vis)
                        throw new NullReferenceException("Vis is null");
                    failPoint++;

                    vis.m_ItemType = new ItemTypeInfo(ObjectTypes.Chunk, -1);
                    failPoint++;
                    Rigidbody rbody = Instance.GetComponent<Rigidbody>();
                    if (!rbody)
                        throw new NullReferenceException("rbody is null");
                    failPoint++;
                    rbody.mass = chunk.Mass;
                    failPoint++;
                    ResourcePickup RP = Instance.GetComponent<ResourcePickup>();
                    if (!RP)
                        throw new NullReferenceException("ResourcePickup is null");
                    failPoint++;
                    ResRare.SetValue(RP, chunk.Rarity);

                    failPoint++;
                    try
                    {
                        var meshR = Mod.GetMaterialFromModAssetBundle(chunk.TextureName, false);
                        if (!meshR)
                            meshR = ResourcesHelper.GetMaterialFromBaseGameAllDeep(chunk.TextureName, false);
                        Instance.GetComponent<MeshRenderer>().sharedMaterial = meshR;
                    }
                    catch (Exception e)
                    {
                        Log("MeshRenderer null");
                    }
                    failPoint++;
                    try
                    {
                        var meshF = Mod.GetMeshFromModAssetBundle(chunk.MeshName, false);
                        if (meshF)
                            Instance.GetComponent<MeshFilter>().sharedMesh = meshF;
                    }
                    catch (Exception e)
                    {
                        Log("MeshFilter null");
                    }
                    failPoint++;
                    var dmg = Instance.GetComponent<Damageable>();
                    if (!dmg)
                        throw new NullReferenceException("Damageable is null");
                    failPoint++;
                    healthMain.SetValue(dmg, (int)(chunk.Health * 4096));


                    failPoint++;
                    //poolStart.Invoke(vis, new object[] { });// Already done by "Instance.CreatePool(4)"
                    //failPoint++;
                    //poolStart2.Invoke(RP, new object[] { });// Already done by "Instance.CreatePool(4)"

                    chunk.prefab = Instance;
                    failPoint++;
                    Instance.gameObject.SetActive(false);
                    failPoint++;
                    ResourceTable.Definition InstanceDef = new ResourceTable.Definition
                    {
                        basePrefab = Instance,
                        frictionDynamic = chunk.DynamicFriction,
                        frictionStatic = chunk.StaticFriction,
                        mass = chunk.Mass,
                        saleValue = chunk.Cost,
                        m_ChunkType = (ChunkTypes)(-1),
                        name = chunk.Name,
                        restitution = chunk.Restitution,
                    };
                    failPoint++;
                    chunk.runtimePrefabBase = InstanceDef;
                    failPoint++;


                    Active.Add(ID, chunk);
                    failPoint++;
                    Instance.CreatePool(4);
                    failPoint++;
                    Log("Created " + chunk.Name + " instance.");
                }
                catch (Exception e)
                {
                    UnityEngine.Object.Destroy(Instance.gameObject);
                    Log("Failed to create " + chunk.Name + " instance (" + failPoint + ") - " + e);
                }
            }
            else
                throw new NullReferenceException("Chunk PrefabName \"" + chunk.PrefabName + "\" does not have a valid prefab instance!" +
                    "  A chunk NEEDS a valid prefab to exist!");
        }
    }
}
