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
        internal static class ModeMain_Patches
        {
            internal static Type target = typeof(ModeMain);
            /// <summary>
            /// PatchTankToAllowLocoEngine
            /// </summary>
            [HarmonyPriority(-9001)]
            static void SetupModeLoadSaveListeners_Postfix(ref ModeMain __instance, ref TechAudio.UpdateAudioCache cache)
            {
                __instance.SubscribeToEvents(ManModBiomes.biomeSaveManager);
            }
            static void CleanupModeLoadSaveListeners_Postfix(ref ModeMain __instance)
            {
                __instance.UnsubscribeFromEvents(ManModBiomes.biomeSaveManager);
            }
        }
        internal static class ModeMisc_Patches
        {
            internal static Type target = typeof(ModeMisc);
            /// <summary>
            /// PatchTankToAllowLocoEngine
            /// </summary>
            [HarmonyPriority(-9001)]
            static void SetupModeLoadSaveListeners_Postfix(ref ModeMisc __instance, ref TechAudio.UpdateAudioCache cache)
            {
                if (__instance.GetGameType() == ManGameMode.GameType.Creative)
                    __instance.SubscribeToEvents(ManModBiomes.biomeSaveManager);
            }
            static void CleanupModeLoadSaveListeners_Postfix(ref ModeMisc __instance)
            {
                if (__instance.GetGameType() == ManGameMode.GameType.Creative)
                    __instance.UnsubscribeFromEvents(ManModBiomes.biomeSaveManager);
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

    }
}
