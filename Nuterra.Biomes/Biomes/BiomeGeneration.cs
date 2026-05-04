using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World.Biomes
{
    internal class BiomeGeneration : ManWorldGeneratorExt
    {
        private static bool applied = false;
        private static Biome[] SingleSelectorMain = null;
        private static BiomeGroup SingleSelector = null;
        private static BiomeGroup[] SingleMain = null;

        public const int numExpectedBiomes = 19;
        private static void InsureFraudBiome()
        {
            if (SingleSelector == null)
            {
                SingleSelectorMain = new Biome[1];
                SingleSelector = ScriptableObject.CreateInstance<BiomeGroup>();
                SingleMain = new BiomeGroup[1];
                SingleMain[0] = SingleSelector;
                biomesWeights.SetValue(SingleSelector, new float[1] { 1f });
                biomesInside.SetValue(SingleSelector, SingleMain);
            }
        }
        private static void ForceSetOnlyOneBiomeHolySHI(BiomeMap biomesMain, Biome one)
        {
            if (!PRIMARY_SanityCheck(biomesMain, out var groupB))
                return; 
            InsureFraudBiome();

            InsureInit();

            ManWorld.inst.TileManager.PauseGenerationOneFrame();
            biomesMain.InvalidateBiomeDB();

            //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
            SingleMain[0] = SingleSelector;
            SingleSelectorMain[0] = one;

            List<BiomeGroup> biomesGrouped = groupB.ToList();
            List<Biome> biomes = InsureBiomesAreLoaded(biomesMain, biomesGrouped);
            if (biomes.Count < numExpectedBiomes)
            {
                DebugWorld.Log(ManModBiomes.Tag, "We are not in the main game. We cannot apply any changes made by " + nameof(BiomeGeneration));
                return;
            }
            if (ManPointer.inst.IsInteractionModeEnabled)
                UIHelpersExt.BigF5broningBannerSP("Rebuilding planet...", false);
            var dataDeployed = biomesData.GetValue(biomesMain);
            if (dataDeployed != null)
            {
                biomesBatched.SetValue(dataDeployed, SingleMain);
                biomesAll.SetValue(dataDeployed, SingleSelectorMain);
            }
            biomesBatched2.SetValue(dataDeployed, SingleMain);
            biomesInside.SetValue(dataDeployed, SingleSelectorMain);
            ManWorld.inst.TileManager.Reset();
            DebugWorld.Log(ManModBiomes.Tag, "ForceSetOnlyOneBiomeHolySHI");
        }
        private static void ForceSetOnlyOneBiomeGroupHolySHI(BiomeMap biomesMain, BiomeGroup one)
        {
            if (!PRIMARY_SanityCheck(biomesMain, out var groupB))
                return;
            InsureFraudBiome();

            InsureInit();

            ManWorld.inst.TileManager.PauseGenerationOneFrame();
            biomesMain.InvalidateBiomeDB();

            //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
            SingleMain[0] = one;

            List<BiomeGroup> biomesGrouped = groupB.ToList();
            List<Biome> biomes = InsureBiomesAreLoaded(biomesMain, biomesGrouped);
            if (biomes.Count < numExpectedBiomes)
            {
                DebugWorld.Log(ManModBiomes.Tag, "We are not in the main game. We cannot apply any changes made by " + nameof(BiomeGeneration));
                return;
            }
            if (ManPointer.inst.IsInteractionModeEnabled)
                UIHelpersExt.BigF5broningBannerSP("Rebuilding planet...", false);
            var dataDeployed = biomesData.GetValue(biomesMain);
            if (dataDeployed != null)
            {
                biomesBatched.SetValue(dataDeployed, SingleMain);
                biomesAll.SetValue(dataDeployed, one.Biomes);
            }
            biomesBatched2.SetValue(dataDeployed, SingleMain);
            biomesInside.SetValue(dataDeployed, one.Biomes);
            ManWorld.inst.TileManager.Reset();
            DebugWorld.Log(ManModBiomes.Tag, "ForceSetOnlyOneBiomeHolySHI");
        }
        internal static void Reset() => applied = false;
        internal static void OnBiomesReloaded(BiomeMap biomesMain)
        {
            if (applied)
                return;
            applied = true;
            try
            {
                if (ManModBiomes.ToFORCELOAD != null)
                {
                    ForceSetOnlyOneBiomeHolySHI(biomesMain, ManModBiomes.ToFORCELOAD);
                    return;
                }
                if (ManModBiomes.ToFORCELOAD2 != null)
                {
                    ForceSetOnlyOneBiomeGroupHolySHI(biomesMain, ManModBiomes.ToFORCELOAD2);
                    return;
                }
            }
            catch (Exception e)
            {
                DebugWorld.FatalError("fail - " + e);
                return;
            }

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


                if (!PRIMARY_SanityCheck(biomesMain, out var groupB))
                    return;

                InsureInit();

                OnClampTerrain.Subscribe(CleanupMess);
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                biomesMain.InvalidateBiomeDB();

                //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
                List<BiomeGroup> biomesGrouped = groupB.ToList();
                var biomesDataGet = biomesData.GetValue(biomesMain);
                errorCode++;
                List<Biome> biomes = InsureBiomesAreLoaded(biomesMain, biomesGrouped);
                if (biomes.Count < numExpectedBiomes)
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
        private static List<Biome> InsureBiomesAreLoaded(BiomeMap biomesMain, List<BiomeGroup> biomesGrouped)
        {
            List<Biome> biomes = null;
            var biomesDataGet = biomesData.GetValue(biomesMain);
            if (biomesDataGet == null || biomesAll.GetValue(biomesDataGet) == null)
            {
                biomes = new List<Biome>();
                foreach (BiomeGroup biomeGroup in biomesGrouped)
                {
                    for (int step = 0; step < biomeGroup.Biomes.Length; step++)
                    {
                        Biome biom = biomeGroup.Biomes[step];
                        if (!biomes.Contains(biom))
                            biomes.Add(biom);
                    }
                }
                /*
                DebugWater.Log("Waiting on biomesDataGet to load...");
                return;
                */
            }
            else
                biomes = ((Biome[])biomesAll.GetValue(biomesDataGet)).ToList();
            if (biomes == null)
                throw new NullReferenceException(nameof(biomes));
            return biomes;
        }
        private static bool PRIMARY_SanityCheck(BiomeMap biomesMain, out BiomeGroup[] groupB)
        {
            DebugWorld.Log(ManModBiomes.Tag, "Ocean man why don't you take me by the hand~");
            if (biomesMain == null)
            {
                DebugWorld.Log(ManModBiomes.Tag, "Could not change biomes: Biomes is not loaded...");
                groupB = null;
                return false;
            }
            if (ManWorld.inst?.TileManager == null)
            {
                DebugWorld.Log(ManModBiomes.Tag, "Could not change biomes: TileManager is not loaded...");
                groupB = null;
                return false;
            }
            groupB = biomesBatched2.GetValue(biomesMain) as BiomeGroup[];
            if (groupB == null || groupB.Length == 0)
            {
                DebugWorld.Log(ManModBiomes.Tag, "Could not change biomes: BiomeGroups is not loaded...");
                return false;
            }
            return true;
        }

        public static void CleanupMess()
        {
        }
    }
}
