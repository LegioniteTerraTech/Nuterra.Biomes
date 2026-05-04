using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Nuterra.ModLoading;
using Nuterra.World.Chunks;
using SafeSaves;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World.Recipes
{
    /// <summary>
    /// ON INDEFINATE HIATUS
    /// </summary>
    [AutoSaveManager]
    public class ManModRecipes : ModLoaderSystem<ManModRecipes, CustomRecipe>
    {
        public const string Tag = ".Recipes";
        public const string ModRecipesName = ManModWorld.ModName + Tag;

        protected override string ourTag => Tag;
        protected override string leadingFileName { get; } = "Rec_";
        public override string LogDirectoryName { get; } = "Recipes";
        [SSManagerInst]
        public static ManModRecipes inst = new ManModRecipes();
        public static Dictionary<int, string> modRecipesModNames = new Dictionary<int, string>();

        public ManModRecipes()
        {
        }
        protected override void Init_Internal()
        {
        }
        private static readonly FieldInfo recData = typeof(ResourceManager).GetField("m_ModdedRecipes", spamFlags);
        private static Dictionary<string, RecipeTable.RecipeList> modRecipies;
        public static void PrepareAllRecipies(bool reload)
        {
            inst.Log("ManModRecipes: Loading all modded!");
            string path = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "Custom Recipes");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            inst.Log("Path in: " + path);
            inst.CreateAll(reload, path);
            modRecipies = (Dictionary<string, RecipeTable.RecipeList>)recData.GetValue(ResourceManager.inst);
            inst.Log("ManModRecipes: finished!");
        }

        internal static CustomRecipe ExtractFromExisting(RecipeTable.RecipeList objTarget)
        {
            try
            {
                return inst.ExtractFromExisting((object)objTarget);
            }
            catch (Exception e)
            {
                inst.Log("Failed to fetch " +
                    (objTarget.m_Name.NullOrEmpty() ? "<NULL>" : objTarget.m_Name) + " - " + e);
                return null;
            }
        }

        protected override CustomRecipe ExtractFromExisting(object objTarget)
        {
            if (objTarget == null)
                throw new NullReferenceException("objTarget IS NULL");
            RecipeTable.RecipeList def = objTarget as RecipeTable.RecipeList;
            if (def == null)
                throw new NullReferenceException("ResourceTable.Definition IS NULL");

            //Collider Col = target.GetComponent<Collider>();
            return new CustomRecipe()
            {
                Name = def.m_Name,
                JSONData = new Dictionary<string, object>(),
            };
        }

        protected override void FinalAssignment(CustomRecipe recipe, string AssignedID)
        {

            try
            {
                Registered.Add(recipe.fileName, AssignedID);
            }
            catch (Exception e)
            {
                throw new Exception(typeof(ManModChunks).Name + ": Error when registering \"" + recipe.Name +
                    ", (" + AssignedIDInt + ")", e);
            }
            recipe.prefabBase.def.m_ChunkType = AssignedID;
            var List = (Dictionary<ChunkTypes, ResourceManager.ResourceDefWrapper>)ResData.GetValue(ResourceManager.inst);
            List.Add(AssignedID, recipe.prefabBase);
            vis.m_ItemType = new ItemTypeInfo(ObjectTypes.Chunk, AssignedIDInt);
            try
            {
                IdToNameIndexLookup.Add(AssignedIDInt, defRedirect);
            }
            catch (Exception e)
            {
                throw new Exception(typeof(ManModChunks).Name + ": Error when registering the name and description of \"" +
                    recipe.Name + ", (" + AssignedIDInt + ")", e);
            }
            try
            {
                modRecipesModNames.Add(AssignedIDInt, recipe.mod.ModID);
            }
            catch (Exception e)
            {
                throw new Exception(typeof(ManModChunks).Name + ": Error when registering the mod name of \"" +
                    recipe.Name + ", (" + AssignedIDInt + ")", e);
            }
            recipe.prefab.CreatePool(4);
            DebugRandAddi.Log("ManModChunks: Assigned Custom Recipie " + recipe.Name + " to ID " + AssignedIDInt);
        }

        protected override void CreateInstanceFile(ModContainer Mod, string path, bool Reload = false)
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
                chunk.mod = Mod;
            }
            if (!Reload && chunk.prefab != null)
                return;
            Active.Remove(fileName);
            if (ResourceManager.inst == null)
                throw new NullReferenceException("ResourceManager.inst is NULL - cannot continue!");
            var Prefabs = (Dictionary<ChunkTypes, ResourceManager.ResourceDefWrapper>)ResData.GetValue(ResourceManager.inst);
            if (Prefabs == null)
                throw new NullReferenceException("Chunk lookup is NULL - cannot continue!");
            ChunkTypes prefabType;
            if (chunk.PrefabName == null)
            {
                throw new NullReferenceException("Chunk PrefabName for file " + fileName + " does not exists!" +
                    "  A chunk NEEDS a valid prefab to exist!");
            }
            else if (chunk.Mass <= 0)
            {
                throw new NullReferenceException("Chunk PrefabName \"" + chunk.PrefabName + "\" cannot have Mass of " + chunk.Mass.ToString() + ", " +
                    "  A chunk NEEDS Mass greater than 0!");
            }
            else if (!EnumTryGetTypeFlexable(chunk.PrefabName, out prefabType))
                prefabType = ChunkTypes.Wood;
            if (Prefabs.TryGetValue(prefabType, out ResourceManager.ResourceDefWrapper PrefabOutter) &&
                PrefabOutter?.def?.basePrefab != null)
            {
                Transform Prefab = PrefabOutter.def.basePrefab;
                Transform Instance = Prefab.UnpooledSpawn();
                if (!Instance)
                    throw new NullReferenceException("Instance is null");

                try
                {
                    chunk.fileName = fileName;
                    chunk.mod = Mod;
                    Visible vis = Instance.GetComponent<Visible>();
                    if (!vis)
                        throw new NullReferenceException("Vis is null");

                    vis.m_ItemType = new ItemTypeInfo(ObjectTypes.Chunk, -1);
                    Rigidbody rbody = Instance.GetComponent<Rigidbody>();
                    if (!rbody)
                        throw new NullReferenceException("rbody is null");
                    rbody.mass = chunk.Mass;
                    ResourcePickup RP = Instance.GetComponent<ResourcePickup>();
                    if (!RP)
                        throw new NullReferenceException("ResourcePickup is null");
                    ResRare.SetValue(RP, chunk.Rarity);

                    try
                    {
                        var meshR = Mod.GetMaterialFromModAssetBundle(chunk.TextureName, false);
                        if (!meshR)
                            meshR = ResourcesHelper.GetMaterialFromBaseGameAllDeep(chunk.TextureName, false);
                        Instance.GetComponent<MeshRenderer>().sharedMaterial = meshR;
                    }
                    catch (Exception e)
                    {
                        DebugRandAddi.Log("MeshRenderer null");
                    }
                    try
                    {
                        var meshF = Mod.GetMeshFromModAssetBundle(chunk.MeshName, false);
                        if (meshF)
                            Instance.GetComponent<MeshFilter>().sharedMesh = meshF;
                    }
                    catch (Exception e)
                    {
                        DebugRandAddi.Log("MeshFilter null");
                    }
                    var dmg = Instance.GetComponent<Damageable>();
                    if (!dmg)
                        throw new NullReferenceException("Damageable is null");
                    healthMain.SetValue(dmg, (int)(chunk.Health * 4096));


                    poolStart.Invoke(vis, new object[] { });
                    poolStart2.Invoke(RP, new object[] { });

                    chunk.prefab = Instance;
                    Instance.gameObject.SetActive(false);
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
                    ResourceManager.ResourceDefWrapper InstanceOutter = new ResourceManager.ResourceDefWrapper()
                    {
                        def = InstanceDef,
                    };
                    InstanceOutter.InitMaterials();
                    chunk.prefabBase = InstanceOutter;

                    Active.Add(fileName, chunk);
                    DebugRandAddi.Log("Created " + chunk.Name + " instance.");

                }
                catch (Exception e)
                {
                    UnityEngine.Object.Destroy(Instance.gameObject);
                    DebugRandAddi.Log("Failed to create " + chunk.Name + " instance - " + e);
                }
            }
            else
                throw new NullReferenceException("Chunk PrefabName \"" + chunk.PrefabName + "\" does not have a valid prefab instance!" +
                    "  A chunk NEEDS a valid prefab to exist!");

        }
        protected override void CreateInstanceAsset(ModContainer Mod, TextAsset path, bool Reload = false)
        {
            string fileName = path.name;
            string text = null;
            Active.TryGetValue(fileName, out CustomChunk chunk);
            if (chunk == null || Reload)
            {
                JSONConverterUniversal.Foundation = null;
                JSONConverterUniversal.CreateNew = true;
                text = path.text;
                JsonConvert.DeserializeObject<CustomChunk>(text, new JSONConverterUniversal());
            }
            if (!Reload && chunk.prefab != null)
                return;
            var Prefabs = (Dictionary<ChunkTypes, ResourceManager.ResourceDefWrapper>)ResData.GetValue(ResourceManager.inst);
            ChunkTypes prefabType;
            if (chunk.PrefabName == null)
            {
                Debug.Log("Chunk PrefabName <FIELD IS NULL> does not exists!" +
                    "  A chunk NEEDS a valid prefab to exist!");
                return;
            }
            else if (chunk.Mass <= 0)
            {
                Debug.Log("Chunk PrefabName \"" + chunk.PrefabName + "\" cannot have Mass of " + chunk.Mass.ToString() + ", " +
                    "  A chunk NEEDS Mass greater than 0!");
                return;
            }
            else if (!EnumTryGetTypeFlexable(chunk.PrefabName, out prefabType))
                prefabType = ChunkTypes.Wood;
            if (Prefabs.TryGetValue(prefabType, out ResourceManager.ResourceDefWrapper PrefabOutter) &&
                PrefabOutter.def != null && PrefabOutter.def.basePrefab != null)
            {
                Transform Prefab = PrefabOutter.def.basePrefab;
                Transform Instance = Prefab.UnpooledSpawn();
                try
                {
                    chunk.mod = Mod;
                    chunk.fileName = fileName;
                    Visible vis = Instance.GetComponent<Visible>();
                    vis.m_ItemType = new ItemTypeInfo(ObjectTypes.Chunk, -1);
                    Rigidbody rbody = Instance.GetComponent<Rigidbody>();
                    rbody.mass = chunk.Mass;
                    ResourcePickup RP = Instance.GetComponent<ResourcePickup>();
                    ResRare.SetValue(RP, chunk.Rarity);
                    try
                    {
                        var meshR = Mod.GetMaterialFromModAssetBundle(chunk.TextureName, false);
                        if (!meshR)
                            meshR = ResourcesHelper.GetMaterialFromBaseGameAllDeep(chunk.TextureName, false);
                        Instance.GetComponent<MeshRenderer>().sharedMaterial = meshR;
                    }
                    catch (Exception e)
                    {
                        DebugRandAddi.Log("MeshRenderer null");
                    }
                    try
                    {
                        var meshF = Mod.GetMeshFromModAssetBundle(chunk.MeshName, false);
                        if (meshF)
                            Instance.GetComponent<MeshFilter>().sharedMesh = meshF;
                    }
                    catch (Exception e)
                    {
                        DebugRandAddi.Log("MeshFilter null");
                    }
                    healthMain.SetValue(Instance.GetComponent<Damageable>(), (int)(chunk.Health * 4096));


                    poolStart.Invoke(vis, new object[] { });
                    poolStart2.Invoke(RP, new object[] { });

                    chunk.prefab = Instance;
                    Instance.gameObject.SetActive(false);
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
                    ResourceManager.ResourceDefWrapper InstanceOutter = new ResourceManager.ResourceDefWrapper()
                    {
                        def = InstanceDef,
                    };
                    InstanceOutter.InitMaterials();
                    chunk.prefabBase = InstanceOutter;

                    Active.Add(fileName, chunk);

                }
                catch (Exception e)
                {
                    UnityEngine.Object.Destroy(Instance.gameObject);
                    DebugRandAddi.Log("Failed to create " + chunk.Name + " instance - " + e);
                }
            }
            else
                Debug.Log("Chunk PrefabName \"" + chunk.PrefabName + "\" does not have a valid prefab instance!" +
                    "  A chunk NEEDS a valid prefab to exist!");

        }=
}
}
