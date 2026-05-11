using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nuterra.World.Biomes;
using TerraTechETCUtil;

namespace Nuterra.World.PatchBatch
{
    /// <summary>
    /// Converted version for use with slightly more secure <see cref="MassPatcher"/>
    /// </summary>
    internal class BiomePatches
    {
        internal static class ModuleLight_Patches
        {
            internal static Type target = typeof(ModuleLight);
            /// <summary>
            /// LightsInTheDarkBiomesDuringDay
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static void EnableLights_Prefix(ModuleLight __instance, ref bool enable)
            {
                if (__instance.block?.tank)
                {
                    var weights = ManWorld.inst.GetBiomeWeightsAtScenePosition(__instance.block.tank.boundsCentreWorldNoCheck);
                    if (weights.Valid && ManModBiomes.DarkInDayBiomes.Contains(weights.Biome(0)))
                        enable = true;
                }
            }
        }
        
        internal static class UIScreenNewGame_Patches
        {
            internal static Type target = typeof(UIScreenNewGame);
            /// <summary>
            /// PatchTankToAllowLocoEngine
            /// </summary>
            [HarmonyPriority(-9001)]
            static void OnBackClicked_Postfix()
            {
                HideSelector();
            }
            [HarmonyPriority(-9001)]
            static void Hide_Postfix()
            {
                ManModBiomes.selector.useGUILayout = false;
            }
            [HarmonyPriority(-9001)]
            static void OnModeClicked_Postfix()
            {
                HideSelector();
            }
            static void HideSelector()
            {
                ManModBiomes.selector.Reset();
                ManModBiomes.selector.useGUILayout = false;
            }
        }
        internal static class AddMOARBiomes_Patches
        {
            internal static Type target = typeof(BiomeMap);
            /// <summary>
            /// PatchTankToAllowLocoEngine
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static void GetBiomeDB_Prefix(BiomeMap __instance)
            {
                //BiomeGeneration.OnBiomesReloaded(__instance);
            }
        }

    }
}
