using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TerraTechETCUtil;
using UnityEngine;
using static BiomeMap;

namespace Nuterra.World.Biomes
{
    internal class BiomeGeneration : ManWorldGeneratorExt
    {
        private static bool applied = false;
        private static Biome[] singleBiomeList = null;
        private static BiomeGroup singleBiomeGroup = null;
        private static BiomeGroup[] singleBiomeGroupList = null;
        private static BiomeMap theMAp = null;
        private static FieldInfo defaultBiome = AccessTools.Field(typeof(BiomeMap), "defaultBiome");
        private static FieldInfo weightDist = AccessTools.Field(typeof(BiomeGroup), "m_WeightingByDistance");

        public const int numExpectedBiomes = 19;
        private static BiomeMap InsureFakeBiomeMap(BiomeMap refBiome)
        {
            InsureFraudBiome(refBiome);
            theMAp.InvalidateBiomeDB();
            return theMAp;
        }
        public static BiomeMap GetFakeBiomeMap(BiomeMap refBiome, Biome one)
        {
            if (recursionStopper)
                throw new InvalidOperationException("We tried to recurse illegally - " + StackTraceUtility.ExtractStackTrace());
            recursionStopper = true;
            try
            {
                var map = InsureFakeBiomeMap(refBiome);
                FixupBiome(one, ManModBiomes.FallbackBiome, false);
                singleBiomeGroupList[0] = singleBiomeGroup;
                for (int i = 0; i < singleBiomeList.Length; i++)
                    singleBiomeList[i] = one;
                for (int i = 0; i < singleBiomeGroupList.Length; i++)
                    singleBiomeGroupList[i] = singleBiomeGroup;
                defaultBiome.SetValue(map, one);
                map.LookupBiome(0);
                return map;
            }
            finally
            {
                recursionStopper = false;
            }
        }
        public static BiomeMap GetFakeBiomeMap(BiomeMap refBiome, BiomeGroup one)
        {
            if (recursionStopper)
                throw new InvalidOperationException("We tried to recurse illegally - " + StackTraceUtility.ExtractStackTrace());
            recursionStopper = true;
            try
            {
                var map = InsureFakeBiomeMap(refBiome);
                FixupGroup(one, false);
                for (int i = 0; i < singleBiomeGroupList.Length; i++)
                    singleBiomeGroupList[i] = one;
                defaultBiome.SetValue(map, one.Biomes[0]);
                map.LookupBiome(0);
                return map;
            }
            finally
            {
                recursionStopper = false;
            }
        }
        public static void FixupBiome(Biome one, Biome refOne, bool LOG)
        {
            try
            {
                var cloudflare = one.CloudParams;
                if (cloudflare == null)
                    throw new NullReferenceException(nameof(one.CloudParams));
            }
            catch (Exception)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "m_CloudParams");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no m_CloudParams assigned.  Please add or reference one.");
            }
            if (one.HeightMapGenerator == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "heightMapGenerator");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no heightMapGenerator assigned.  Please add or reference one.");
            }
            if (one.MultiTextureGenerator == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "multiTextureGenerator");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no multiTextureGenerator assigned.  Please add or reference one.");
            }
            if (one.MainMaterialLayer == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "m_MainMaterialLayer");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no m_MainMaterialLayer assigned.  Please add or reference one.");
            }
            if (one.AltMaterialLayer == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "m_AltMaterialLayer");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no m_AltMaterialLayer assigned.  Please add or reference one.");
            }
            if (one.MapTexture == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "m_MapTexture");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no m_MapTexture assigned.  Please add or reference one.");
            }
            if (one.DetailLayers == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "layers");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no layers assigned.  Please add or reference one.");
            }
            if (one.DayLighting == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "m_DayLighting");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no m_DayLighting assigned.  Please add or reference one.");
            }
            if (one.NightLighting == null)
            {
                var fixMe = AccessTools.Field(typeof(Biome), "m_NightLighting");
                fixMe.SetValue(one, fixMe.GetValue(refOne));
                if (LOG)
                    NuterraRes.LogError("Biome \"" + one.name +
                        "\" had no m_NightLighting assigned.  Please add or reference one.");
            }
        }
        public static void FixupGroup(BiomeGroup one, bool LOG)
        {
            Biome[] biomesListed = (Biome[])biomesInside.GetValue(one);
            float[] weightsListed = (float[])biomesWeights.GetValue(one);
            AnimationCurve weightDistMain = (AnimationCurve)weightDist.GetValue(one);
            if (biomesListed.Any(x => x.IsNull()))
            {
                if (!biomesListed.Any(x => x.IsNotNull()))
                {
                    NuterraRes.LogError("Biome group \"" + one.name +
                        "\" HAS NO BIOMES!!! REJECTED!!!");
                    throw new NullReferenceException("No biomes in biome group  \"" + one.name + "\"");
                }
                biomesInside.SetValue(one, biomesListed.Where(x => x.IsNotNull()).ToArray());
                if (LOG)
                    NuterraRes.LogError("Biome group \"" + one.name +
                        "\" has missing or NULL Biomes!");
            }
            if (weightsListed.Length != biomesListed.Length)
            {
                Array.Resize(ref weightsListed, biomesListed.Length);
                biomesWeights.SetValue(one, weightsListed);
                if (LOG)
                    NuterraRes.LogError("Biome group \"" + one.name + 
                        "\" has mismatch in weights assigned.  Should be the same count as m_Biomes!");
            }
            if (weightDistMain.keys.Length != biomesListed.Length)
            {   // This is fully resolvable automatically.
                var keys = weightDistMain.keys;
                Array.Resize(ref keys, biomesListed.Length);
                for (int i = 0; i < keys.Length; i++)
                {
                    float time = i / keys.Length;
                    keys[i] = new Keyframe(time, weightDistMain.Evaluate(time));
                }
                weightDist.SetValue(one, new AnimationCurve(keys));
            }
        }
        public static bool IsGroupValid(BiomeGroup one)
        {
            if (one == null)
                return false;
            Biome[] biomesListed = (Biome[])biomesInside.GetValue(one);
            return biomesListed != null && biomesListed.Any();
        }




        private static void InsureFraudBiome(BiomeMap refBiome)
        {
            if (singleBiomeGroup == null)
            {
                int biomeCopies = 8;
                int biomeGroupCopies = 8;
                singleBiomeList = new Biome[biomeCopies];
                float[] floatato = new float[biomeCopies];
                var animcurve = AnimationCurve.Linear(0f, 1f, 100f, 1f);
                var animcurveKeys = new Keyframe[biomeCopies];
                for (int i = 0; i < singleBiomeList.Length; i++)
                {
                    singleBiomeList[i] = refBiome.LookupBiome(0);
                    floatato[i] = 1f;
                    animcurveKeys[i] = new Keyframe(1f, 1f);
                }
                animcurveKeys[0] = new Keyframe(0f, 1f);
                animcurve.keys = animcurveKeys;

                singleBiomeGroup = ScriptableObject.CreateInstance<BiomeGroup>();
                biomesInside.SetValue(singleBiomeGroup, singleBiomeList);
                biomesWeights.SetValue(singleBiomeGroup, floatato);
                weightDist.SetValue(singleBiomeGroup, animcurve);
                singleBiomeGroupList = new BiomeGroup[biomeGroupCopies];
                for (int i = 0; i < singleBiomeGroupList.Length; i++)
                    singleBiomeGroupList[i] = singleBiomeGroup;

                theMAp = new GameObject("FakeMap").AddComponent<BiomeMap>();
                biomesBatched2.SetValue(theMAp, singleBiomeGroupList);
                theMAp.m_LandmarkSpawner = refBiome.LandmarkSpawner;
                theMAp.m_VendorSpawner = refBiome.VendorSpawner;
                theMAp.m_VendorLandmarkMinSeparation = refBiome.VendorLandmarkMinSeparation;

                string[] TempCopy = new string[]
                    {
                         "m_RequiredWorldGenVersionType",
                         "m_SceneryDistributionWeights",
                         "m_AdvancedParameters",
                         "m_RequiredWorldGenVersion",
                         "m_BiomeDistributionScaleMacro",
                         "defaultBiome",
                         "biomes",
                         "m_BiomeDefaultSwapNumClosestRegions",
                         "enableRegions",
                         "bandTolerance",
                         "layer1DistMethod",
                         "layer1Scale",
                         "layer1Rotation",
                         "layer1Translation",
                         "layer0DistMethod",
                         "layer0Scale",
                         "layer0Rotation",
                         "layer0Translation",
                         "m_StuntRampSpawner",
                    };
                foreach (var item in TempCopy)
                {
                    var valGet = AccessTools.Field(typeof(BiomeMap), item);
                    valGet.SetValue(theMAp, valGet.GetValue(refBiome));
                }
            }
        }

        private static bool PRIMARY_SanityCheckAndGetBiomes(BiomeMap biomesMain,
            out BiomeGroup[] ogBiomeGroups, ref List<Biome> biomesCopy)
        {
            ogBiomeGroups = null;
            if (biomesMain == null)
            {
                DebugWorld.Log(ManModBiomes.Tag, "Could not change biomes: Biomes is not loaded...");
                return false;
            }
            if (ManWorld.inst?.TileManager == null)
            {
                DebugWorld.Log(ManModBiomes.Tag, "Could not change biomes: TileManager is not loaded...");
                return false;
            }
            ogBiomeGroups = biomesBatched2.GetValue(biomesMain) as BiomeGroup[];
            if (ogBiomeGroups == null || ogBiomeGroups.Length == 0)
            {
                DebugWorld.Log(ManModBiomes.Tag, "Could not change biomes: BiomeGroups is not loaded...");
                return false;
            }
            biomesCopy.Clear();
            var biomesDataGet = biomesData.GetValue(biomesMain);
            if (biomesDataGet == null || biomesAll.GetValue(biomesDataGet) == null)
            {   // Get from the existing default pool
                foreach (BiomeGroup biomeGroup in ogBiomeGroups)
                {
                    for (int step = 0; step < biomeGroup.Biomes.Length; step++)
                    {
                        Biome biom = biomeGroup.Biomes[step];
                        if (!biomesCopy.Contains(biom))
                            biomesCopy.Add(biom);
                    }
                }
            }
            else
                biomesCopy.AddRange((Biome[])biomesAll.GetValue(biomesDataGet)); // get from BiomeGroupDatabase
            return true;
        }

        private static void SetBiomesAndGroups(BiomeMap biomesMain,
           List<BiomeGroup> biomeGroups, List<Biome> allBiomes)
        {
            recursionStopper = true;
            try
            {
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                biomesMain.InvalidateBiomeDB();
                var sharedBGArray = biomeGroups.ToArray();
                var biomesDataGet = biomesData.GetValue(biomesMain);
                if (biomesDataGet != null)
                {
                    biomesAll.SetValue(biomesDataGet, new List<Biome>(allBiomes));
                    biomesBatched.SetValue(biomesDataGet, sharedBGArray);
                }
                biomesBatched2.SetValue(biomesMain, sharedBGArray);
                biomesMain.LookupBiome(0);
                ManWorld.inst.Reset(ManWorld.inst.CurrentBiomeMap);
                ManWorldTileExt.RushTileLoading();
            }
            finally
            {
                recursionStopper = false;
            }
        }

        private static List<Biome> curList = new List<Biome>();

        internal static void Reset() => applied = false;
        private static bool recursionStopper = false;
        internal static void OnBiomesReloaded(BiomeMap biomesMain)
        {
            if (recursionStopper)
                return;

            //Debug_TTExt.ShouldLogBiomeGen = true;
            int errorCode = 0;
            try
            {
                if (biomesData == null)
                    throw new NullReferenceException(nameof(biomesData));
                if (biomesAll == null)
                    throw new NullReferenceException(nameof(biomesAll));
                if (biomesInside == null)
                    throw new NullReferenceException(nameof(biomesInside));
                if (biomesWeights == null)
                    throw new NullReferenceException(nameof(biomesWeights));
                if (biomesBatched == null)
                    throw new NullReferenceException(nameof(biomesBatched));
                if (biomesBatched2 == null)
                    throw new NullReferenceException(nameof(biomesBatched2));

                if (applied)
                    return;
                applied = true;

                if (!PRIMARY_SanityCheckAndGetBiomes(biomesMain, out var groupB, ref curList))
                    return;

                InsureInit();

                OnClampTerrain.Subscribe(CleanupMess);

                //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
                List<BiomeGroup> biomesGrouped = groupB.ToList();
                var biomesDataGet = biomesData.GetValue(biomesMain);
                errorCode++;
                if (curList.Count < numExpectedBiomes)
                {
                    DebugWorld.Log(ManModBiomes.Tag, "We are not in the main game. We cannot apply any changes made by " + nameof(BiomeGeneration));
                    return;
                }

                if (Singleton.playerTank != null)
                    UIHelpersExt.BigF5broningBannerSP("Rebuilding planet...", false);

                ManWorld.inst.TileManager.Reset();
            }
            catch (Exception e)
            {
                throw new Exception("Failed at " + errorCode, e);
            }
        }


        public static void CleanupMess()
        {
        }
    }
}
