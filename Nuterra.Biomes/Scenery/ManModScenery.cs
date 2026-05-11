using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using HarmonyLib;
using Newtonsoft.Json;
using Nuterra.ModLoading;
using Nuterra.World.Chunks;
using Nuterra.World.PatchBatch;
using SafeSaves;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World.Scenery
{
    [Flags]
    public enum SceneryCategory
    { 
        Null = 0,
        Safe = 1,
        Hostile = 2,
        Harvestable = 4,
        HasSeam = 8,
        Mobile = 16,
    }

    /// <summary>
    /// Remember that Scenery is stored in the game by it's GameObject name, not by it's own SceneryType 
    ///   <para>(SceneryTypes is used by other utilities like Advanced AI to determine interactivity)</para>
    ///   <para>For other experimental matters see <seealso cref="SceneryMaker"/></para>
    ///   <para>For more advanced users, see <seealso cref="ManEvilFloraFauna"/> for interactive scenery</para>
    /// </summary>
    [AutoSaveManager]
    public class ManModScenery : ModLoaderSystem<ManModScenery, SceneryTypes, CustomScenery>, IModPreloadable
    {
        public const string Tag = ".Scenery";
        public const string ModSceneryName = ManModWorld.ModLogName + Tag;

        public static bool Reload = false;
        protected override string ourTag => Tag;
        protected override string leadingFileName { get; } = "Sce_";
        public override string LogDirectoryName { get; } = "Scenery";
        [SSManagerInst]
        public static ManModScenery inst = new ManModScenery();
        public static Dictionary<int, string> modSceneryModNames = new Dictionary<int, string>();
        public static readonly string FolderPath = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "Custom Scenery");
        internal static TerrainObjectTable table;
        public ManModScenery()
        {
            table = SpawnHelper.GetAllTerrainObjectPrefabTable();
            WikiPageScenery.GetSceneryModName = SceneryModNameWrapper;
        }


        ModDataHandle IModPreloadable.ModHandle => KickStartWorld.oInst;
        bool IModPreloadable.ChainFail => false;
        void IModPreloadable.OnFail() { }
        public string Subject => "Injecting modded scenery - ";
        string IModPreloadable.InProgress => InProgress;
        float IModPreloadable.EstPercentDone => EstPercentDone;
        int IModPreloadable.EstNumSteps => EstNumSteps;
        int IModPreloadable.EstNumStepsIterator => EstNumStepsIterator;
        public IEnumerator GetEnumerator() => PrepareAllScenery(Reload);


        public static void SanityCheck()
        {
            if (ResLook == null)
                throw new NullReferenceException(nameof(ResLook));
            if (ResLook2 == null)
                throw new NullReferenceException(nameof(ResLook2));
            if (m_ResourceSpawnChances == null)
                throw new NullReferenceException(nameof(m_ResourceSpawnChances));
            if (m_DamageStages == null)
                throw new NullReferenceException(nameof(m_DamageStages));
            if (m_TotalChunks == null)
                throw new NullReferenceException(nameof(m_TotalChunks));
            if (m_SpawnPointOverride == null)
                throw new NullReferenceException(nameof(m_SpawnPointOverride));
            if (m_SpawnRange == null)
                throw new NullReferenceException(nameof(m_SpawnRange));
            if (m_SpawnSpeed == null)
                throw new NullReferenceException(nameof(m_SpawnSpeed));
            if (m_SpawnVelocityRandom == null)
                throw new NullReferenceException(nameof(m_SpawnVelocityRandom));
            if (m_SpawnRotationRandom == null)
                throw new NullReferenceException(nameof(m_SpawnRotationRandom));
            if (m_DamageAnim == null)
                throw new NullReferenceException(nameof(m_DamageAnim));
            if (m_DeathAnim == null)
                throw new NullReferenceException(nameof(m_DeathAnim));
            if (m_RegrowAnim == null)
                throw new NullReferenceException(nameof(m_RegrowAnim));
            if (m_DontRegrow == null)
                throw new NullReferenceException(nameof(m_DontRegrow));
            if (m_RegrowSfxEvent == null)
                throw new NullReferenceException(nameof(m_RegrowSfxEvent));
            if (m_HitPrefab == null)
                throw new NullReferenceException(nameof(m_HitPrefab));
            if (m_DebrisPrefab == null)
                throw new NullReferenceException(nameof(m_DebrisPrefab));
            if (m_BigDebrisPrefab == null)
                throw new NullReferenceException(nameof(m_BigDebrisPrefab));
            if (m_IgnoreSceneryFade == null)
                throw new NullReferenceException(nameof(m_IgnoreSceneryFade));
        }
        private static void FirstLoad()
        {
            WikiPageScenery.GetSceneryData = GetSceneryJSON;
            WikiPageScenery.GetSceneryModName = GetSceneryMod;
        }
        private static string GetSceneryJSON(ResourceDispenser resDisp)
        {
            return JsonConvert.ToString(ExtractFromExisting(resDisp.GetComponent<TerrainObject>()));
        }
        private static string GetSceneryMod(int chunkID)
        {
            var inv = inst.Registered.FirstOrDefault(x => (int)x.Value == chunkID);
            if (inv.Key != default && inst.Active.TryGetValue(inv.Key, out var scenery))
                return scenery.modID;
            if (chunkID >= 350)
                return ManModWorld.ModID;
            return WikiPageScenery.GetSceneryModNameDefault(chunkID);
        }
        public static bool InitPatches = false;
        protected override void Init_Internal()
        {
        }
        public static string SceneryModNameWrapper(int CT)
        {
            if (modSceneryModNames.TryGetValue(CT, out string ModName))
                return ModName;
            return WikiPageScenery.GetSceneryModNameDefault(CT);
        }
        public static IEnumerator PrepareAllScenery(bool reload)
        {
            if (InitPatches || MassPatcher.MassPatchAllWithin(ManModWorld.harmonyInst, typeof(SceneryPatches), ManModWorld.ModLogName, true))
            {
                if (!InitPatches)
                    FirstLoad();
                InitPatches = true;
                inst.Log("Loading all modded!");
                if (!Directory.Exists(FolderPath))
                    Directory.CreateDirectory(FolderPath);
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
                inst.Error("Rebuilding lookup FAILED! " + nameof(SceneryPatches) + " didn't load!");
        }



        // ---------------------------------  CREATION  ---------------------------------
        private static readonly FieldInfo ResLook = typeof(StringLookup).GetField("m_SceneryNames", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo ResLook2 = typeof(StringLookup).GetField("m_SceneryDescriptions", BindingFlags.NonPublic | BindingFlags.Static);
        //private static readonly MethodInfo poolStart2 = typeof(TerrainObject).GetMethod("OnPool", spamFlags);// Already done by "Instance.CreatePool(4)"
        //private static readonly MethodInfo poolStart3 = typeof(ResourceDispenser).GetMethod("OnPool", spamFlags);// Already done by "Instance.CreatePool(4)"

        private static readonly FieldInfo m_ResourceSpawnChances = typeof(ResourceDispenser).GetField("m_ResourceSpawnChances", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_DamageStages = typeof(ResourceDispenser).GetField("m_DamageStages", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_TotalChunks = typeof(ResourceDispenser).GetField("m_TotalChunks", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_SpawnPointOverride = typeof(ResourceDispenser).GetField("m_SpawnPointOverride", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_SpawnRange = typeof(ResourceDispenser).GetField("m_SpawnRange", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_SpawnSpeed = typeof(ResourceDispenser).GetField("m_SpawnSpeed", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_SpawnVelocityRandom = typeof(ResourceDispenser).GetField("m_SpawnVelocityRandom", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_SpawnRotationRandom = typeof(ResourceDispenser).GetField("m_SpawnRotationRandom", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_DamageAnim = typeof(ResourceDispenser).GetField("m_DamageAnim", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_DeathAnim = typeof(ResourceDispenser).GetField("m_DeathAnim", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_RegrowAnim = typeof(ResourceDispenser).GetField("m_RegrowAnim", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_DontRegrow = typeof(ResourceDispenser).GetField("m_DontRegrow", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_RegrowSfxEvent = typeof(ResourceDispenser).GetField("m_RegrowSfxEvent", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_HitPrefab = typeof(ResourceDispenser).GetField("m_HitPrefab", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_DebrisPrefab = typeof(ResourceDispenser).GetField("m_DebrisPrefab", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_BigDebrisPrefab = typeof(ResourceDispenser).GetField("m_BigDebrisPrefab", spamFlags | BindingFlags.Instance);
        private static readonly FieldInfo m_IgnoreSceneryFade = typeof(ResourceDispenser).GetField("m_IgnoreSceneryFade", spamFlags | BindingFlags.Instance);

        internal static CustomScenery ExtractFromExisting(TerrainObject objTarget)
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

        protected override CustomScenery ExtractFromExisting(object objTarget)
        {
            TerrainObject def = objTarget as TerrainObject;
            if (!def)
                throw new NullReferenceException("TerrainObject IS NULL");
            Transform target = def.transform;
            if (!target)
                throw new NullReferenceException("Transform IS NULL");
            Visible vis = target.GetComponent<Visible>();
            if (!vis)
                throw new NullReferenceException("Visible IS NULL");
            ResourceDispenser RD = target.GetComponent<ResourceDispenser>();
            if (!RD)
                throw new NullReferenceException("ResourceDispenser IS NULL");
            Damageable dmg = target.GetComponent<Damageable>();
            var MR = target.GetComponentInChildren<MeshRenderer>(true);
            var MF = target.GetComponentInChildren<MeshFilter>(true);
            var CT = (ChunkTypes)vis.ItemType;
            if (dmg == null)
            {
                return new CustomScenery(target.name)
                {
                    Name = StringLookup.GetItemName(ObjectTypes.Scenery, vis.ItemType),
                    Description = StringLookup.GetItemDescription(ObjectTypes.Scenery, vis.ItemType),
                    PrefabName = target.name,
                    TextureName = MR ? (MR.sharedMaterial ?
                    MR.sharedMaterial.name : MR.material.name) : null,
                    MeshName = MF ? (MF.sharedMesh ?
                    MF.sharedMesh.name : MF.mesh.name) : null,
                    DamageableType = ManDamage.DamageableType.Standard,
                    Health = -1,//int.MaxValue / 2048f,
                    GroundRadius = RD.GroundRadius,
                    Hostile = false,
                    MaxHeightOffset = 0,
                    MinHeightOffset = 0,
                    JSONData = new Dictionary<string, object>(),

                    AttackedChunks = (int)m_TotalChunks.GetValue(RD),
                    CanRespawn = !(bool)m_DontRegrow.GetValue(RD),
                    ChunkSpawnWeights = ((ResourceSpawnChance[])m_ResourceSpawnChances.GetValue(RD)).Select(x => new CustomScenery.ChunkSpawn()
                    {
                        ChunkID = x.chunkType.ToString(),
                        SpawnWeight = x.spawnWeight,
                    }).ToArray(),
                    DamagedAnimation = (ManSceneryAnimation.AnimTypes)m_DamageAnim.GetValue(RD),
                    DeathAnimation = (ManSceneryAnimation.AnimTypes)m_DeathAnim.GetValue(RD),
                    RegrowAnimation = (ManSceneryAnimation.AnimTypes)m_RegrowAnim.GetValue(RD),
                    FadeOnObscure = !(bool)m_IgnoreSceneryFade.GetValue(RD),
                    Roving = false,
                    modID = ManIngameWiki.VanillaGameName,
                    SFXName = ((FMODEvent)m_RegrowSfxEvent.GetValue(RD)).EventPath,
                    SpawnOffset = ((Transform)m_SpawnPointOverride.GetValue(RD)).localPosition,
                    SpawnRandomDirection = (Vector3)m_SpawnVelocityRandom.GetValue(RD),
                    SpawnRandomOffset = (float)m_SpawnRange.GetValue(RD),
                    SpawnRandomRotation = (Vector3)m_SpawnRotationRandom.GetValue(RD),
                    SpawnVelocity = (float)m_SpawnVelocityRandom.GetValue(RD),
                    Stages = ((ResourceDispenser.DamageStage[])m_DamageStages.GetValue(RD)).Select(x => new CustomScenery.Stage()
                    {
                        DoesNotObstructSpawning = x.m_IgnoredForPlacementCheck,
                        ForceUpright = x.m_ForceUpright,
                        IsInvulnerable = x.m_Invulnerable,
                        MeshName = x.m_Geometry?.GetComponent<MeshFilter>()?.sharedMesh?.name,
                        StageHealth = x.m_Health,
                    }).ToArray(),
                };
            }
            else
            {
                return new CustomScenery(target.name)
                {
                    Name = StringLookup.GetItemName(ObjectTypes.Scenery, vis.ItemType),
                    Description = StringLookup.GetItemDescription(ObjectTypes.Chunk, vis.ItemType),
                    PrefabName = target.name,
                    TextureName = MR ? (MR.sharedMaterial ?
                    MR.sharedMaterial.name : MR.material.name) : null,
                    MeshName = MF ? (MF.sharedMesh ?
                    MF.sharedMesh.name : MF.mesh.name) : null,
                    DamageableType = dmg.DamageableType,
                    Health = (float)healthMain.GetValue(dmg),
                    GroundRadius = RD.GroundRadius,
                    Hostile = false,
                    MaxHeightOffset = 0,
                    MinHeightOffset = 0,
                    JSONData = new Dictionary<string, object>(),

                    AttackedChunks = (int)m_TotalChunks.GetValue(RD),
                    CanRespawn = !(bool)m_DontRegrow.GetValue(RD),
                    ChunkSpawnWeights = ((ResourceSpawnChance[])m_ResourceSpawnChances.GetValue(RD)).Select(x => new CustomScenery.ChunkSpawn()
                    {
                        ChunkID = x.chunkType.ToString(),
                        SpawnWeight = x.spawnWeight,
                    }).ToArray(),
                    DamagedAnimation = (ManSceneryAnimation.AnimTypes)m_DamageAnim.GetValue(RD),
                    DeathAnimation = (ManSceneryAnimation.AnimTypes)m_DeathAnim.GetValue(RD),
                    RegrowAnimation = (ManSceneryAnimation.AnimTypes)m_RegrowAnim.GetValue(RD),
                    FadeOnObscure = !(bool)m_IgnoreSceneryFade.GetValue(RD),
                    Roving = false,
                    modID = ManIngameWiki.VanillaGameName,
                    SFXName = ((FMODEvent)m_RegrowSfxEvent.GetValue(RD)).EventPath,
                    SpawnOffset = ((Transform)m_SpawnPointOverride.GetValue(RD)).localPosition,
                    SpawnRandomDirection = (Vector3)m_SpawnVelocityRandom.GetValue(RD),
                    SpawnRandomOffset = (float)m_SpawnRange.GetValue(RD),
                    SpawnRandomRotation = (Vector3)m_SpawnRotationRandom.GetValue(RD),
                    SpawnVelocity = (float)m_SpawnVelocityRandom.GetValue(RD),
                    Stages = ((ResourceDispenser.DamageStage[])m_DamageStages.GetValue(RD)).Select(x => new CustomScenery.Stage()
                    {
                        DoesNotObstructSpawning = x.m_IgnoredForPlacementCheck,
                        ForceUpright = x.m_ForceUpright,
                        IsInvulnerable = x.m_Invulnerable,
                        MeshName = x.m_Geometry?.GetComponent<MeshFilter>()?.sharedMesh?.name,
                        StageHealth = x.m_Health,
                    }).ToArray(),
                };
            }
        }
        protected override void CreatePrefabFromFile(string ModID, string path, bool Reload = false)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string text = null;
            Active.TryGetValue(fileName, out CustomScenery scenery);
            if (scenery == null || Reload)
            {
                JSONConverterUniversal.Foundation = null;
                JSONConverterUniversal.CreateNew = true;
                text = File.ReadAllText(path);
                scenery = JsonConvert.DeserializeObject<CustomScenery>(text);//, new JSONConverterUniversal());
                if (scenery == null)
                    throw new NullReferenceException("Scenery file " + fileName + " is corrupted!");
                scenery.modID = ModID;
            }
            if (!Reload && scenery.prefab != null)
                return;
            if (Active.Remove(fileName))
                DestroyPrefab(scenery);
            CreatePrefab(ModID, fileName, scenery);
        }
        protected override void CreatePrefabFromAsset(string ModID, TextAsset tAsset, bool Reload = false)
        {
            string fileName = tAsset.name;
            string text = null;
            Active.TryGetValue(fileName, out CustomScenery scenery);
            if (scenery == null || Reload)
            {
                JSONConverterUniversal.Foundation = null;
                JSONConverterUniversal.CreateNew = true;
                text = tAsset.text;
                scenery = JsonConvert.DeserializeObject<CustomScenery>(text, new JSONConverterUniversal());
                if (scenery == null)
                    throw new NullReferenceException("Scenery file " + fileName + " is corrupted!");
                scenery.modID = ModID;
            }
            if (!Reload && scenery.prefab != null)
                return;
            Active.Remove(fileName);
            CreatePrefab(ModID, fileName, scenery);
        }
        protected override void CreatePrefab(string ModID, string ID, CustomScenery scenery)
        {
            if (ModID == null)
                throw new NullReferenceException("ModID is NULL - cannot continue!");
            ModContainer Mod = ResourcesHelper.GetModContainer(ModID);
            if (Mod == null)
                throw new NullReferenceException("Mod is NULL - cannot continue!");
            if (ID.NullOrEmpty())
                throw new NullReferenceException("ID is NULL - cannot continue!");
            if (scenery == null)
                throw new NullReferenceException("CustomScenery is NULL - cannot continue!");
            if (scenery.PrefabName == null)
            {
                Debug.Log("Scenery PrefabName <FIELD IS NULL> does not exists!" +
                    "  scenery NEEDS a valid prefab to exist!");
                return;
            }
            TerrainObject PrefabTO = SpawnHelper.GetSceneryPrefabByName(scenery.PrefabName);
            if (PrefabTO != null && !Active.ContainsKey(PrefabTO.name))
            {
                Transform Instance = null;
                try
                {
                    scenery.modID = ModID;
                    scenery.fileName = ID;
                    Transform Prefab = PrefabTO.transform;
                    if (Prefab == null)
                        throw new NullReferenceException("Prefab is null");
                    Instance = UnityEngine.Object.Instantiate(Prefab, null);
                    if (Instance == null)
                        throw new NullReferenceException("Instance is null");


                    if (SpawnHelper.GetSceneryPrefabByName(ID) is TerrainObject TOCheck &&
                        !Active.ContainsKey(TOCheck.name))
                        throw new InvalidOperationException("Cannot add/replace scenery into the game with the same name as vanilla scenery");
                    Instance.name = ID;

                    Visible vis = Instance.GetComponent<Visible>();
                    if (vis == null)
                        throw new NullReferenceException("Visible is null");
                    vis.m_ItemType = new ItemTypeInfo(ObjectTypes.Scenery, -1);

                    try
                    {
                        var meshR = Mod.GetMaterialFromModAssetBundle(scenery.TextureName, false);
                        if (!meshR)
                            meshR = ResourcesHelper.GetMaterialFromBaseGameAllDeep(scenery.TextureName, false);
                        Instance.GetComponent<MeshRenderer>().sharedMaterial = meshR;
                    }
                    catch (Exception e)
                    {
                        Log("MeshRenderer null");
                    }
                    try
                    {
                        var meshF = Mod.GetMeshFromModAssetBundle(scenery.MeshName, false);
                        if (meshF)
                            Instance.GetComponent<MeshFilter>().sharedMesh = meshF;
                    }
                    catch (Exception e)
                    {
                        Log("MeshFilter null");
                    }
                    var dmg = Instance.GetComponent<Damageable>();
                    if (dmg == null)
                        throw new NullReferenceException("Damageable is null");
                    healthMain.SetValue(dmg, (int)(scenery.Health * 4096));

                    TerrainObject TO = Instance.GetComponent<TerrainObject>();
                    if (TO == null)
                        throw new NullReferenceException("TerrainObject is null");

                    // Already done by "Instance.CreatePool(4)"
                    //poolStart.Invoke(vis, new object[] { });
                    //poolStart2.Invoke(TO, new object[] { });

                    ResourceDispenser RD = Instance.GetComponent<ResourceDispenser>();
                    if (RD != null)
                    {
                        m_TotalChunks.SetValue(RD, scenery.AttackedChunks);
                        m_DontRegrow.SetValue(RD, !scenery.CanRespawn);
                        m_DamageAnim.SetValue(RD, scenery.DamagedAnimation);
                        m_DeathAnim.SetValue(RD, scenery.DeathAnimation);
                        m_RegrowAnim.SetValue(RD, scenery.RegrowAnimation);
                        m_IgnoreSceneryFade.SetValue(RD, !scenery.FadeOnObscure);
                        //Roving = false,
                        //SFXName = ((FMODEvent)m_RegrowSfxEvent.GetValue(RD)).EventPath,
                        ((Transform)m_SpawnPointOverride.GetValue(RD)).localPosition = scenery.SpawnOffset;
                        m_SpawnVelocityRandom.SetValue(RD, scenery.SpawnRandomDirection);
                        m_SpawnRange.SetValue(RD, scenery.SpawnRandomOffset);
                        m_SpawnRotationRandom.SetValue(RD, scenery.SpawnRandomRotation);
                        m_SpawnVelocityRandom.SetValue(RD, scenery.SpawnVelocity);
                        ResourceDispenser.DamageStage[] stages = (ResourceDispenser.DamageStage[])m_DamageStages.GetValue(RD);
                        ResourceDispenser.DamageStage[] stagesNew = new ResourceDispenser.DamageStage[stages.Length];
                        for (int i = 0; i < stages.Length; i++)
                        {
                            if (scenery.Stages.Length - 1 < i)
                                break;
                            var x = scenery.Stages[i];
                            var og = stages[i];
                            var transGet = og.m_Geometry;
                            var tryGet = ResourcesHelper.GetMeshFromModAssetBundle(Mod, x.MeshName);
                            if (tryGet != null)
                                transGet.GetComponentInChildren<MeshFilter>().sharedMesh = tryGet;
                            stagesNew[i] = new ResourceDispenser.DamageStage()
                            {
                                m_IgnoredForPlacementCheck = x.DoesNotObstructSpawning,
                                m_ForceUpright = x.ForceUpright,
                                m_Invulnerable = x.IsInvulnerable,
                                m_Geometry = transGet,
                                m_Health = x.StageHealth,
                            };
                        }
                        m_DamageStages.SetValue(RD, stagesNew);
                    }

                    scenery.prefab = TO;
                    Active.Add(ID, scenery);

                    Instance.gameObject.SetActive(false);
                }
                catch (Exception e)
                {
                    if (Instance != null)
                        UnityEngine.Object.Destroy(Instance.gameObject);
                    Log("Failed to create " + scenery.Name + " instance - " + e);
                }
            }
            else
            {
                StringBuilder SB = new StringBuilder();
                foreach (var item in SpawnHelper.IterateAllScenery().Where(x => x.Value != null &&
                    x.Key != null && !Registered.ContainsKey(x.Key)))
                    SB.AppendLine(" - " + item.Key + ", Type:" + (item.Value.GetComponent<Visible>()?.m_ItemType != null ?
                        ((SceneryTypes)item.Value.GetComponent<Visible>().m_ItemType.ItemType).ToString() : "No Visible"));
                Error("Scenery PrefabName \"" + scenery.PrefabName + "\", filename \"" + scenery.fileName +
                    "\" does not have a valid prefab instance!\nScenery NEEDS a valid prefab to exist!\nApplicable types as follows:\n" + SB.ToString());
            }
        }
        protected override void DestroyPrefab(CustomScenery scenery)
        {
            if (scenery != null)
                scenery.prefab.DeletePool();
        }



        // ---------------------------------  INJECTION  ---------------------------------
        protected override void InjectionStarting()
        {
            Reload = true;
            var tableCached = table.GetLookupList();
            foreach (var item in Active)
                tableCached.Remove(item.Key);
        }
        protected override void InjectOne(CustomScenery scenery, SceneryTypes AssignedID)
        {
            Visible vis = scenery.prefab.GetComponent<Visible>();
            int AssignedIDInt = (int)AssignedID;
            int PreviousIDInt = vis.m_ItemType.ItemType;
            if (PreviousIDInt == AssignedIDInt)
                return;
            Dictionary<int, int> IdToNameIndexLookup = (Dictionary<int, int>)ResLook.GetValue(null);
            Dictionary<int, int> IdToNameIndexLookup2 = (Dictionary<int, int>)ResLook2.GetValue(null);
            int defRedirect = AssignedIDInt;
            if (PreviousIDInt == -1)
            {
                LocalisationExt.RegisterRawEng(LocalisationEnums.StringBanks.SceneryName, AssignedIDInt, scenery.Name);
                LocalisationExt.RegisterRawEng(LocalisationEnums.StringBanks.SceneryDescription, AssignedIDInt, scenery.Description);
            }
            else
                defRedirect = IdToNameIndexLookup[AssignedIDInt];
            if (PreviousIDInt != AssignedIDInt)
            { // We resync this with our new ID
                try
                {
                    modSceneryModNames.Remove(PreviousIDInt);
                    IdToNameIndexLookup.Remove(PreviousIDInt);
                    IdToNameIndexLookup2.Remove(PreviousIDInt);
                    scenery.prefab.DeletePool();
                }
                catch (Exception e)
                {
                    Log(typeof(ManModScenery).Name + ": Error when assigning \"" +
                        scenery.Name + ", (" + PreviousIDInt + ")\" to (" + AssignedIDInt + "): " + e);
                }
            }
            if (!modSceneryModNames.ContainsKey(AssignedIDInt))
            {
                try
                {
                    Registered.Remove(scenery.fileName);
                    Registered.Add(scenery.fileName, AssignedID);
                    var hash = ItemTypeInfo.GetHashCode(ObjectTypes.Scenery, (int)AssignedID);
                    SceneryCategory CC = SceneryCategory.Null;
                    if (scenery.Hostile)
                        CC.SetFlags(SceneryCategory.Hostile, true);
                    else
                        CC.SetFlags(SceneryCategory.Safe, true);
                    if (scenery.Roving)
                        CC.SetFlags(SceneryCategory.Mobile, true);
                    ManSpawn.inst.VisibleTypeInfo.SetDescriptorFlags<SceneryCategory>(hash, (int)CC);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModScenery).Name + ": Error when registering \"" +
                        scenery.Name + ", (" + AssignedIDInt + ")", e);
                }
                vis.m_ItemType = new ItemTypeInfo(ObjectTypes.Scenery, AssignedIDInt);
                try
                {
                    IdToNameIndexLookup.Add(AssignedIDInt, defRedirect);
                    IdToNameIndexLookup2.Add(AssignedIDInt, defRedirect);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModScenery).Name + ": Error when registering the name and description of \"" +
                        scenery.Name + ", (" + AssignedIDInt + ")", e);
                }
                try
                {
                    modSceneryModNames.Add(AssignedIDInt, scenery.modID);
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModScenery).Name + ": Error when registering the mod name for \"" +
                        scenery.Name + ", (" + AssignedIDInt + ")", e);
                }
                try
                {
                    var resDisp = scenery.prefab.GetComponent<ResourceDispenser>();
                    if (resDisp != null)
                    {
                        m_ResourceSpawnChances.SetValue(resDisp, scenery.ChunkSpawnWeights.
                            Select(x =>
                            {
                                var def = ResourceManager.inst.resourceTable.resources.FirstOrDefault(y => y.name == x.ChunkID);
                                if (def == default)
                                {
                                    Error("Error when registering ResourceSpawnChance[] for \"" +
                                        scenery.Name + ", (" + AssignedIDInt + ") - chunkID of " + 
                                        def.m_ChunkType + " does not exists");
                                    return new ResourceSpawnChance()
                                    {
                                        chunkType = ChunkTypes.Wood,
                                        spawnWeight = x.SpawnWeight,
                                    };
                                }
                                else
                                {
                                    return new ResourceSpawnChance()
                                    {
                                        chunkType = def.m_ChunkType,
                                        spawnWeight = x.SpawnWeight,
                                    };
                                }
                            }).ToArray());
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(ManModScenery).Name + ": Error when registering ResourceSpawnChance[] for \"" +
                        scenery.Name + ", (" + AssignedIDInt + ")", e);
                }
                Log("Assigned Custom Scenery " + scenery.Name + "(" + scenery.fileName + ") to ID " + AssignedIDInt);
                var page = ManIngameWiki.GetSceneryPage(scenery.Name);
                if (page == null)
                    new WikiPageScenery(AssignedIDInt, ManIngameWiki.InsureSceneryWikiGroup(scenery.modID));
                else
                {
                    ManIngameWiki.CloseWiki();
                    page.ID = AssignedID;
                }
                scenery.prefab.CreatePool(4);
            }
        }
        protected override void InjectionFinished()
        {
            table.InitLookupTable();
            /// Then it goes to <see cref="AddOurSceneryNOW(Dictionary{string, TerrainObject})"/>
        }
        public static void AddOurSceneryNOW(Dictionary<string, TerrainObject> tableMainGame)
        {
            //CustomSceneryMaker.MakeScenery(tableMainGame);
            foreach (var item in inst.Registered)
            {
                if (inst.Active.TryGetValue(item.Key, out var cust))
                {
                    if (tableMainGame.ContainsKey(item.Key))
                    {
                        DebugWorld.LogError(Tag, "We tried to add Scenery of name " + item +
                            " but we failed because it was still registered even when it shouldn't be.  We will now be out of sync");
                    }
                    else
                    {
                        tableMainGame.Add(item.Key, cust.prefab);
                        DebugWorld.Log(Tag, "Added " + item + " to the lookups");
                    }
                }
                else
                    DebugWorld.LogError(Tag, "We tried to add Scenery of name " + item +
                        " but we failed because it does't actually exist(???).  We will now be out of sync");
            }
            SpawnHelper.RefetchScenery();
        }



        // ---------------------------------  ACTIVATION  ---------------------------------
        public override void SpawnNow(CustomScenery Mod, Vector3 scenePos)
        {
            if (Mod != null && Registered.TryGetValue(Mod.fileName, out var type))
            {
                var tiile = ManWorld.inst.TileManager.LookupTile(scenePos);
                var prefab = SpawnHelper.GetSceneryPrefabByName(Mod.ID);
                if (tiile != null && prefab != null)
                {
                    ManWorld.inst.TryProjectToGround(ref scenePos);
                    var newspawn = prefab.SpawnFromPrefab(tiile, scenePos, Quaternion.identity);
                    if (newspawn != null)
                        newspawn.GetComponent<ResourceDispenser>()?.SetAwake(true);
                    else
                        DebugWorld.LogError(Tag, "We tried spawn Scenery of filename " + Mod.fileName +
                            " but the spawn result was NULL");
                }
                else
                {
                    if (tiile == null)
                        DebugWorld.LogError(Tag, "We tried spawn Scenery of filename " + Mod.fileName +
                            " but the tile doesn't exist at " + scenePos.ToString());
                    else if (prefab == null)
                    {
                        DebugWorld.LogError(Tag, "We tried spawn Scenery of filename " + Mod.fileName +
                            " SpawnHelper.GetSceneryPrefabByName() returned a NULL lookup");
                        DebugWorld.Log(Tag, "SceneryTypeToFirstSceneryName");
                        foreach (var item in SpawnHelper.SceneryTypeToFirstSceneryName)
                            DebugWorld.Log(Tag, " - " + item.Key.ToString() + ": " + item.Value);
                        DebugWorld.Log(Tag, "SceneryByName");
                        foreach (var item in SpawnHelper.inst.SceneryByName)
                            DebugWorld.Log(Tag, " - " + item.Key.ToString() + ": " + item.Value);
                    }
                }
            }
            else
            {
                if (Mod == null)
                    DebugWorld.LogError(Tag, "We tried spawn Scenery that was NULL");
                else if (!Registered.ContainsKey(Mod.fileName))
                    DebugWorld.LogError(Tag, "We tried spawn Scenery of filename " + Mod.fileName +
                        " but it isn't registered!?!");
            }
        }
    }

}
