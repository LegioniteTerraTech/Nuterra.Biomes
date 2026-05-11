using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nuterra.World.Biomes;
using Nuterra.World.Chunks;
using Nuterra.World.Scenery;
using RandomAdditions;

namespace Nuterra.World.PatchBatch
{
    internal class GlobalPatches
    {/*
        internal static class UILoadingScreenModProgressPatches
        {
            internal static Type target = typeof(UILoadingScreenModProgress);

            /// <summary>
            /// OverrideTheScreenToShowWeAreLoadingBiomesRN
            /// </summary>
            /// <param name="__instance"></param>
            /// <returns></returns>
            [HarmonyPriority(9001)]
            internal static bool Update_Prefix(UILoadingScreenModProgress __instance)
            {
                if (ManModChunks.InProgress != null)
                {   // Show ManModBiomes status here 
                    __instance.loadingProgressText.text = "Injecting modded chunks - " +
                        ((int)(ManModChunks.EstPercentDone * 100f)).ToString() + "%\n" + 
                        ManModChunks.EstNumStepsIterator + " of " + ManModChunks.EstNumSteps +
                        "\n" + ManModChunks.InProgress;
                    __instance.loadingProgressImage.fillAmount = ManModChunks.EstPercentDone;
                    return false;
                }
                if (ManModScenery.InProgress != null)
                {   // Show ManModBiomes status here 
                    __instance.loadingProgressText.text = "Injecting modded scenery - " +
                        ((int)(ManModScenery.EstPercentDone * 100f)).ToString() + "%\n" +
                        ManModScenery.EstNumStepsIterator + " of " + ManModScenery.EstNumSteps +
                        "\n" + ManModScenery.InProgress;
                    __instance.loadingProgressImage.fillAmount = ManModScenery.EstPercentDone;
                    return false;
                }
                if (ManModBiomes.InProgress != null)
                {   // Show ManModBiomes status here 
                    __instance.loadingProgressText.text = "Injecting modded biomes - " +
                        ((int)(ManModBiomes.EstPercentDone * 100f)).ToString() + "%\n" +
                        ManModBiomes.EstNumStepsIterator + " of " + ManModBiomes.EstNumSteps +
                        "\n" + ManModBiomes.InProgress;
                    __instance.loadingProgressImage.fillAmount = ManModBiomes.EstPercentDone;
                    return false;
                }
                return true;
            }
        }//*/
    }
}
