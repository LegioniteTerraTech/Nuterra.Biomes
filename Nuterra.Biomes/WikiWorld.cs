using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nuterra.World.Chunks;
using Nuterra.World.Scenery;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World
{
    internal class WikiWorld
    {
        private static string exportsPath => Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "_Export");
        private static Sprite nullSprite;

        internal static void InitWiki()
        {
            if (nullSprite != null)
                return;
            nullSprite = UIHelpersExt.NullSprite;
            InitChunks();
        }
        internal static void InitChunks()
        {
            var group = ManIngameWiki.InsureChunksWikiGroup(ManModWorld.ModName);
            foreach (var item in ChunkMaker.Resurrected)
            {
                new WikiPageChunk((int)item, group);
            }
        }
        public static void ViewOnWiki()
        {
            GUILayout.BeginVertical(AltUI.TextfieldBlackHuge);
            GUILayout.Label("Mod Data", AltUI.LabelBlueTitle);
            GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge);
            GUILayout.Label("Custom Chunks: ", AltUI.LabelBlueTitle);
            if (AltUI.Button("Export", ManSFX.UISfxType.Enter, AltUI.ButtonBlueLarge))
            {
                string path = exportsPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path = Path.Combine(exportsPath, "ChunkInfoDump.json");
                var SB = new List<CustomChunk>();
                for (int i = 0; i < Enum.GetValues(typeof(ChunkTypes)).Length; i++)
                {
                    var def = ResourceManager.inst.GetResourceDef((ChunkTypes)i);
                    if (def != null)
                    {
                        var refP = ManModChunks.ExtractFromExisting(def);
                        if (refP != null)
                            SB.Add(refP);
                    }
                }
                File.WriteAllText(path, JsonConvert.SerializeObject(SB, Formatting.Indented));
            }
            if (AltUI.Button("Open", ManSFX.UISfxType.Open, AltUI.ButtonBlueLarge))
            {
                string path = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "Custom Chunks");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                ManModWorld.OpenInExplorer(path);
            }
            if (AltUI.Button("Reload", ManSFX.UISfxType.Enter, AltUI.ButtonBlueLarge))
            {
                ManModChunks.PrepareAllChunks(true);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge);
            GUILayout.Label("Custom Scenery: ", AltUI.LabelBlueTitle);
            if (AltUI.Button("Export", ManSFX.UISfxType.Enter, AltUI.ButtonBlueLarge))
            {
                string path = exportsPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path = Path.Combine(exportsPath, "SceneryInfoDump.json");
                var LCS = new List<CustomScenery>();
                var iterated = new HashSet<string>();
                foreach (var sc in SpawnHelper.IterateSceneryTypes())
                {
                    foreach (var sc2 in sc)
                    {
                        foreach (var val in sc2.Value)
                        {
                            if (val != null && iterated.Add(val.name))
                            {
                                var refP = ManModScenery.ExtractFromExisting(val);
                                if (refP != null)
                                {
                                    LCS.Add(refP);
                                    break;
                                }
                            }
                        }
                    }
                }
                File.WriteAllText(path, JsonConvert.SerializeObject(LCS, Formatting.Indented));
                path = Path.Combine(exportsPath, "SceneryInfoList.txt");
                var SB = new StringBuilder();
                SpawnHelper.PrintAllRegisteredResourceNodes(SB);
                File.WriteAllText(path, SB.ToString());
            }
            if (AltUI.Button("Open", ManSFX.UISfxType.Open, AltUI.ButtonBlueLarge))
            {
                string path = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "Custom Scenery");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                ManModWorld.OpenInExplorer(path);
            }
            if (AltUI.Button("Reload", ManSFX.UISfxType.Enter, AltUI.ButtonBlueLarge))
            {
                ManModScenery.PrepareAllScenery(true);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
