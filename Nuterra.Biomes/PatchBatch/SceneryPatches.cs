using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nuterra.World.Scenery;

namespace Nuterra.World.PatchBatch
{
    internal static class SceneryPatches
    {
        internal static class TerrainObjectTablePatches
        {
            internal static Type target = typeof(TerrainObjectTable);

            [HarmonyPriority(-9001)]
            internal static void InitLookupTable_Postfix(Dictionary<string, TerrainObject> ___m_GUIDToPrefabLookup)
            {
                ManModScenery.AddOurSceneryNOW(___m_GUIDToPrefabLookup);
            }
        }
    }
}
