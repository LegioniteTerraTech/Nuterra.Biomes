using System;
using System.Collections;
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
using UnityEngine.SceneManagement;
using static LocalisationEnums;

namespace Nuterra.World.Chunks
{
    /// <summary>
    /// Makes the EXISTING chunks usuable again yay
    /// </summary>
    [AutoSaveManager]
    public class ManModChunks : ModLoaderSystem<ManModChunks, ChunkTypes, CustomChunk>, IModPreloadable
    {
        public const string Tag = ".Chunks";
        public const string ModChunksName = ManModWorld.ModLogName + Tag;

        public static bool Reload = false;
        protected override string ourTag => Tag;
        protected override string leadingFileName { get; } = "Res_";
        public override string LogDirectoryName { get; } = "Chunks";
        [SSManagerInst]
        public static ManModChunks inst = new ManModChunks();
        public static Dictionary<int, string> modChunksModNames = new Dictionary<int, string>();
        public static readonly string FolderPath = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "Custom Chunks");

        public ManModChunks()
        {
            WikiPageChunk.GetChunkModName = ChunkModNameWrapper;
        }


        ModDataHandle IModPreloadable.ModHandle => KickStartWorld.oInst;
        bool IModPreloadable.ChainFail => false;
        void IModPreloadable.OnFail() { }
        public string Subject => "Injecting modded chunks - ";
        string IModPreloadable.InProgress => InProgress;
        float IModPreloadable.EstPercentDone => EstPercentDone;
        int IModPreloadable.EstNumSteps => EstNumSteps;
        int IModPreloadable.EstNumStepsIterator => EstNumStepsIterator;
        public IEnumerator GetEnumerator() => PrepareAllChunks(Reload);



        private static int DefaultChunkCount;
        public static void SanityCheck()
        {
            if (ResourceManager.inst == null)
                throw new NullReferenceException("ResourceManager.inst is NULL - cannot continue!");
            if (ResLook == null)
                throw new NullReferenceException(nameof(ResLook));
            if (ResLook2 == null)
                throw new NullReferenceException(nameof(ResLook2));
            if (ResRare == null)
                throw new NullReferenceException(nameof(ResRare));
            if (ResNameBase == null)
                throw new NullReferenceException(nameof(ResNameBase));
            if (ResCostBase == null)
                throw new NullReferenceException(nameof(ResCostBase));
            ChunkMaker.RenewOldChunks();
        }
        private static void FirstLoad()
        {
            WikiPageChunk.GetChunkData = GetChunkJSON;
            WikiPageChunk.GetChunkModName = GetChunkMod;
        }
        private static string GetChunkJSON(ChunkTypes type)
        {
            return JsonConvert.ToString(ExtractFromExisting(ResourceManager.inst.GetResourceDef(type)));
        }
        private static string GetChunkMod(int chunkID)
        {
            var inv = inst.Registered.FirstOrDefault(x => (int)x.Value == chunkID);
            if (inv.Key != default && inst.Active.TryGetValue(inv.Key, out var chunk))
                return chunk.modID;
            if (chunkID >= 350)
                return ManModWorld.ModID;
            return WikiPageChunk.GetChunkModNameDefault(chunkID);
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
                return ManModWorld.ModID;
            return WikiPageChunk.GetChunkModNameDefault(CT);
        }
        public static IEnumerator PrepareAllChunks(bool reload)
        {
            if (InitPatches || MassPatcher.MassPatchAllWithin(ManModWorld.harmonyInst, typeof(ChunkPatches), ManModWorld.ModLogName, true))
            {
                if (!InitPatches)
                    FirstLoad();
                InitPatches = true;
                inst.Log("Loading all modded!");
                if (!Directory.Exists(FolderPath))
                    Directory.CreateDirectory(FolderPath);
                //inst.Log("Path in: " + path);
                EstPercentDone = 0f;
                EstNumSteps = inst.GetTimeEstimate(reload, FolderPath);
                EstNumStepsIterator = 0;
                foreach (var item in inst.CreateAll(reload, FolderPath))
                {
                    EstNumStepsIterator++;
                    EstPercentDone = EstNumStepsIterator / (float)EstNumSteps;
                    yield return item;
                }
                inst.Log("Loading finished!");
            }
            else
                inst.Error("Rebuilding lookup FAILED! " + nameof(ChunkPatches) + " didn't load!");
        }



        // ---------------------------------  CREATION  ---------------------------------
        internal static readonly FieldInfo ResLook = typeof(StringLookup).GetField("m_ChunkNames", BindingFlags.NonPublic | BindingFlags.Static);
        internal static readonly FieldInfo ResLook2 = typeof(StringLookup).GetField("m_ChunkDescriptions", BindingFlags.NonPublic | BindingFlags.Static);
        internal static readonly FieldInfo ResRare = typeof(ResourcePickup).GetField("m_ChunkRarity", spamFlags);
        internal static readonly FieldInfo ResNameBase = typeof(ResourceManager).GetField("s_ChunkTypeNames", spamFlags | BindingFlags.Static);
        internal static readonly FieldInfo ResCostBase = typeof(RecipeManager).GetField("m_ChunkPriceLookup", spamFlags | BindingFlags.Static);
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

            var ITI = vis.m_ItemType;
            var hash = ITI.GetHashCode();

            //Collider Col = target.GetComponent<Collider>();
            var CT = (ChunkTypes)vis.ItemType;
            var MR = target.GetComponentInChildren<MeshRenderer>(true);
            var MF = target.GetComponentInChildren<MeshFilter>(true);
            RecipeTable.Recipe recipeBurner = null;
            foreach (var m in RecipeManager.inst.recipeTable.m_RecipeLists)
            {
                recipeBurner = m.m_Recipes.Find(x => x.m_EnergyOutput > 0 && 
                    x.m_EnergyType == TechEnergy.EnergyType.Electric && x.InputsContain(ITI));
                if (recipeBurner != null)
                    break;
            }
            if (recipeBurner == null)
            {
                recipeBurner = new RecipeTable.Recipe()
                {
                    m_BuildTimeSeconds = 0,
                    m_EnergyOutput = 0,
                };
            }
            var descFlags = ManSpawn.inst.VisibleTypeInfo.GetDescriptorFlags<ChunkCategory>(hash);
            return new CustomChunk(target.name)
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
                ComponentTier = (ComponentTier)ManSpawn.inst.VisibleTypeInfo.GetDescriptorFlags<ComponentTier>(hash),
                DamageableType = dmg.m_DamageableType,
                fileName = target.name,
                FuelTime = recipeBurner.m_BuildTimeSeconds,
                FuelEnergy = recipeBurner.m_EnergyOutput,
                IsFuel = ((ChunkCategory)descFlags & ChunkCategory.Fuel) > 0,
                IsRefined = ((ChunkCategory)descFlags & ChunkCategory.Refined) > 0,
            };
        }

        protected override void CreatePrefabFromFile(string ModID, string path, bool Reload = false)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
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
                chunk.modID = ModID;
            }
            if (!Reload && chunk.prefab != null)
                return;
            if (Active.Remove(fileName))
                DestroyPrefab(chunk);
            CreatePrefab(ModID, fileName, chunk);
        }
        protected override void CreatePrefabFromAsset(string ModID, TextAsset tAsset, bool Reload = false)
        {
            string fileName = tAsset.name;
            string text = null;
            Active.TryGetValue(fileName, out CustomChunk chunk);
            if (chunk == null || Reload)
            {
                JSONConverterUniversal.Foundation = null;
                JSONConverterUniversal.CreateNew = true;
                text = tAsset.text;
                chunk = JsonConvert.DeserializeObject<CustomChunk>(text, new JSONConverterUniversal());
                if (chunk == null)
                    throw new NullReferenceException("Chunk file " + fileName + " is corrupted!");
                chunk.modID = ModID;
            }
            if (!Reload && chunk.prefab != null)
                return;
            if (Active.Remove(fileName))
                DestroyPrefab(chunk);
            CreatePrefab(ModID, fileName, chunk);
        }
        protected override void CreatePrefab(string ModID, string ID, CustomChunk chunk)
        {
            if (ModID == null)
                throw new NullReferenceException("ModID is NULL - cannot continue!");
            ModContainer Mod = ResourcesHelper.GetModContainer(ModID);
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
            if (Prefabs.FirstOrDefault(x => x?.basePrefab != null && x.m_ChunkType == prefabType && !Active.ContainsKey(x.name))
                is ResourceTable.Definition PrefabInner && PrefabInner != null)
            {
                int failPoint = 0;
                Transform Instance = null;
                try
                {
                    Transform Prefab = PrefabInner.basePrefab;
                    if (Prefab == null)
                        throw new NullReferenceException("Prefab is null");
                    Instance = UnityEngine.Object.Instantiate(Prefab, null);
                    if (Instance == null)
                        throw new NullReferenceException("Instance is null");

                    if (Prefabs.FirstOrDefault(x => x?.basePrefab != null && x.name == ID) is
                        ResourceTable.Definition defCheck && defCheck != default && !Active.ContainsKey(defCheck.name))
                        throw new InvalidOperationException("Cannot add/replace chunk into the game with the same name as vanilla chunks");

                    chunk.fileName = ID;
                    Instance.name = ID;
                    failPoint++;
                    chunk.modID = ModID;
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
                        name = chunk.Name,
                        m_ChunkType = (ChunkTypes)(-1),
                        basePrefab = Instance,
                        frictionDynamic = chunk.DynamicFriction,
                        frictionStatic = chunk.StaticFriction,
                        mass = chunk.Mass,
                        saleValue = chunk.Cost,
                        restitution = chunk.Restitution,
                    };
                    failPoint++;
                    chunk.runtimePrefabBase = InstanceDef;
                    failPoint++;


                    Active.Add(ID, chunk);
                    failPoint++;
                    Log("Created " + chunk.Name + " instance.");
                }
                catch (Exception e)
                {
                    if (Instance != null)
                        UnityEngine.Object.Destroy(Instance.gameObject);
                    Log("Failed to create " + chunk.Name + " instance (" + failPoint + ") - " + e);
                }
            }
            else
            {
                StringBuilder SB = new StringBuilder();
                foreach (var item in Prefabs.Where(x => x?.name != null && !Registered.ContainsKey(x.name)))
                    SB.AppendLine(" - " + item.name + ", Type:" + item.m_ChunkType.ToString());
                Error("Chunk PrefabName \"" + chunk.PrefabName + "\", filename \"" + chunk.fileName +
                    "\" does not have a valid prefab instance!\nA chunk NEEDS a valid prefab to exist!\nApplicable types as follows:\n" + SB.ToString());
            }
        }

        protected override void DestroyPrefab(CustomChunk chunk)
        {
            if (chunk != null)
                chunk.prefab.DeletePool();
        }


        // ---------------------------------  INJECTION  ---------------------------------
        protected override void InjectionStarting()
        {
            Reload = true;
            if (DefaultChunkCount == 0)
                DefaultChunkCount = SpawnHelper.GetResourceChunkPrefabs().Length;
        }
        protected override void InjectOne(CustomChunk chunk, ChunkTypes AssignedID)
        {
            Visible vis = chunk.prefab.GetComponent<Visible>();
            int AssignedIDInt = (int)AssignedID;
            int PreviousIDInt = vis.m_ItemType.ItemType;
            if (PreviousIDInt == AssignedIDInt)
                return;
            Dictionary<int, int> IdToNameIndexLookup = (Dictionary<int, int>)ResLook.GetValue(null);
            Dictionary<int, int> IdToNameIndexLookup2 = (Dictionary<int, int>)ResLook2.GetValue(null);
            Dictionary<int, int> IdToPriceLookup2 = (Dictionary<int, int>)ResCostBase.GetValue(RecipeManager.inst);
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
                    IdToNameIndexLookup2.Remove(PreviousIDInt);
                    IdToPriceLookup2.Remove(PreviousIDInt);
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
                    Registered.Remove(chunk.fileName);
                    Registered.Add(chunk.fileName, AssignedID);
                    var hash = ItemTypeInfo.GetHashCode(ObjectTypes.Chunk, (int)AssignedID);
                    ChunkCategory CC = ChunkCategory.Null;
                    if (chunk.ComponentTier > ComponentTier.Null)
                    {
                        CC.SetFlags(ChunkCategory.Component, true);
                        ManSpawn.inst.VisibleTypeInfo.SetDescriptorFlags<ComponentTier>(hash, (int)chunk.ComponentTier);
                    }
                    else
                    {
                        ManSpawn.inst.VisibleTypeInfo.SetDescriptorFlags<ComponentTier>(hash, 0);
                        if (chunk.IsRefined)
                            CC.SetFlags(ChunkCategory.Refined, true);
                        else
                            CC.SetFlags(ChunkCategory.Raw, true);
                    }
                    if (chunk.IsFuel)
                        CC.SetFlags(ChunkCategory.Fuel, true);
                    ManSpawn.inst.VisibleTypeInfo.SetDescriptorFlags<ChunkCategory>(hash, (int)CC);
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
                    IdToNameIndexLookup2.Add(AssignedIDInt, defRedirect);
                    IdToPriceLookup2.Add(AssignedIDInt, chunk.Cost);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModChunks).Name + ": Error when registering the name and description of \"" +
                        chunk.Name + ", (" + AssignedIDInt + ")", e);
                }
                try
                {
                    modChunksModNames.Add(AssignedIDInt, chunk.modID);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModChunks).Name + ": Error when registering the mod name of \"" +
                        chunk.Name + ", (" + AssignedIDInt + ")", e);
                }
                Log("Assigned Custom Chunk " + chunk.Name + "(" + chunk.fileName + ") to ID " + AssignedIDInt);
                var page = ManIngameWiki.GetChunkPage(chunk.Name);
                if (page == null)
                    new WikiPageChunk(AssignedIDInt, ManIngameWiki.InsureChunksWikiGroup(chunk.modID));
                else
                {
                    ManIngameWiki.CloseWiki();
                    page.ID = AssignedID;
                }
                chunk.prefab.CreatePool(4);
            }
        }
        protected override void InjectionFinished()
        {
            var Prefabs = SpawnHelper.GetResourceChunkPrefabs();
            if (Prefabs.Length != DefaultChunkCount + Registered.Count)
                Array.Resize(ref Prefabs, DefaultChunkCount + Registered.Count);
            for (int i = 0; i < Registered.Count; i++)
            {
                var pair = Active.ElementAt(i);
                Prefabs[i + DefaultChunkCount] = pair.Value.runtimePrefabBase;
            }
            string[] directNames = (string[])ResNameBase.GetValue(null);
            if (directNames.Length < RegisteredIDIterator)
                Array.Resize(ref directNames, Mathf.Max(directNames.Length, RegisteredIDIterator));
            foreach (var regi in Registered)
                directNames[(int)regi.Value] = regi.Key;
            ResNameBase.SetValue(null, directNames);
            SpawnHelper.OverrideResourceChunkPrefabs(Prefabs);
        }



        // ---------------------------------  ACTIVATION  ---------------------------------
        public override void SpawnNow(CustomChunk Mod, Vector3 scenePos)
        {
            float y = scenePos.y;
            if (Mod != null && Registered.TryGetValue(Mod.fileName, out var type) &&
                ManWorld.inst.TryProjectToGround(ref scenePos))
            {
                if (scenePos.y + 1 < y)
                    scenePos.y = y;
                else
                    scenePos.y += 1;
                ManLooseBlocks.inst.HostSpawnChunk(type, scenePos, Quaternion.identity, true);
            }
        }
    }
}
