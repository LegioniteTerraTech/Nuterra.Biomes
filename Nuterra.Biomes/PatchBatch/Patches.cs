using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nuterra.World.Biomes;

namespace Nuterra.World
{
    internal static class Patches
    {
        // Legacy - kept around for future reference
        /*
        static class ModeMain_Patches
        {
            [HarmonyPatch(typeof(ModeMain), "SetupModeLoadSaveListeners")]
            static class SetupModeLoadSaveListeners
            {
                static void Postfix(ref ModeMain __instance)
                {
                    __instance.SubscribeToEvents(BiomeBootstrapper.biomeSaveManager);
                }
            }

            [HarmonyPatch(typeof(ModeMain), "CleanupModeLoadSaveListeners")]
            static class CleanupModeLoadSaveListeners
            {
                static void Postfix(ref ModeMain __instance)
                {
                    __instance.UnsubscribeFromEvents(BiomeBootstrapper.biomeSaveManager);
                }
            }
        }

        static class ModeMisc_Patches
        {
            [HarmonyPatch(typeof(ModeMisc), "SetupModeLoadSaveListeners")]
            static class SetupModeLoadSaveListeners
            {
                static void Postfix(ref ModeMisc __instance)
                {
                    if (__instance.GetGameType() == ManGameMode.GameType.Creative)
                        __instance.SubscribeToEvents(BiomeBootstrapper.biomeSaveManager);
                }
            }

            [HarmonyPatch(typeof(ModeMisc), "CleanupModeLoadSaveListeners")]
            static class CleanupModeLoadSaveListeners
            {
                static void Postfix(ref ModeMisc __instance)
                {
                    if (__instance.GetGameType() == ManGameMode.GameType.Creative)
                        __instance.UnsubscribeFromEvents(BiomeBootstrapper.biomeSaveManager);
                }
            }
        }

        static class UIScreenNewGame_Patches
        {
            [HarmonyPatch(typeof(UIScreenNewGame), "OnBackClicked")]
            static class OnBackClicked
            {
                static void Postfix()
                {
                    HideSelector();
                }
            }

            [HarmonyPatch(typeof(UIScreenNewGame), "Hide")]
            static class Hide
            {
                static void Postfix()
                {
                    BiomeBootstrapper.selector.useGUILayout = false;
                }
            }

            [HarmonyPatch(typeof(UIScreenNewGame), "OnModeClicked")]
            static class OnModeClicked
            {
                static void Postfix()
                {
                    HideSelector();
                }
            }

            static void HideSelector()
            {
                BiomeBootstrapper.selector.Reset();
                BiomeBootstrapper.selector.useGUILayout = false;
            }
        }//*/
    }
}
