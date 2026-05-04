using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nuterra.World.Biomes
{
    public class BiomeLoader : MonoBehaviour
    {
        public IEnumerator Start()
        {
            ManModBiomes.LogAsset("Asset loading started", ManModBiomes.MetaTag);
            yield return StartCoroutine(ManModBiomes.LoadAllTextures());
            //yield return StartCoroutine(Resources.LoadAllMeshes());
            //yield return StartCoroutine(Resources.LoadAllAudioClips());
            yield return StartCoroutine(ManModBiomes.LoadAllTerrainLayers());
            yield return StartCoroutine(ManModBiomes.LoadAllMapGenerators());
            yield return StartCoroutine(ManModBiomes.LoadAllBiomes());
            yield return StartCoroutine(ManModBiomes.LoadAllBiomeGroups());
            ManModBiomes.LogAsset("Asset loading ended", ManModBiomes.MetaTag);

            if (ManModBiomes.biomeWrappers.Count > 0 || ManModBiomes.userResources.ContainsKey(typeof(BiomeGroup)) && ManModBiomes.userResources[typeof(BiomeGroup)].Count > 0)
            {
                var m_ModePlayPrefab = typeof(UIGameMode).GetField("m_ModePlayPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
                var m_ModePlayPrefabNoTwitter = typeof(UIGameMode).GetField("m_ModePlayPrefabNoTwitter", BindingFlags.NonPublic | BindingFlags.Instance);

                var UIGameModes = UnityEngine.Resources.FindObjectsOfTypeAll<UIGameMode>().ToList();

                var CampaignMode = UIGameModes.Find(gm => gm.name == "Campaign Mode");
                var CreativeMode = UIGameModes.Find(gm => gm.name == "Creative Mode");

                var buttonPrefab = DefaultControls.CreateButton(default(DefaultControls.Resources));
                var button = buttonPrefab.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    ManModBiomes.selector.useGUILayout = true;
                });
                var text = buttonPrefab.GetComponentInChildren<Text>();
                text.text = "Select biomes";
                buttonPrefab.AddComponent<UIModeInitButton>();
                buttonPrefab.transform.SetParent(null, false);

                var containers = new object[]
                {
                    m_ModePlayPrefab.GetValue(CampaignMode),
                    m_ModePlayPrefabNoTwitter.GetValue(CampaignMode),
                    m_ModePlayPrefab.GetValue(CreativeMode),
                    m_ModePlayPrefabNoTwitter.GetValue(CreativeMode)
                };

                foreach (var item in containers)
                {
                    if (item != null && item is GameObject go)
                    {
                        var parent = go.transform.Find("Options");
                        var btn = GameObject.Instantiate(buttonPrefab);
                        btn.transform.SetParent(parent, false);

                        parent.GetComponent<GridLayoutGroup>().constraintCount += 1;
                    }
                }

                ManUI.inst.GetScreen(ManUI.ScreenType.NewGame).ScreenInitialize(ManUI.ScreenType.NewGame);
                ManModBiomes.selector.enabled = true;
            }

            yield break;
        }

        /*float rendertime = 0;
        void Update()
        {
            try
            {
                //if(Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftControl))
                if ((Time.time - rendertime) > 5)
                {
                    List<Color> pixels = new List<Color>();
                    Action<Color> doPixel = pixels.Add;

                    var size = new Vector2(250, 250);
                    var origin = new Vector2(Singleton.playerPos.x, Singleton.playerPos.z);// - size * 0.5f;

                    var step = 1;
                    var seed = ManWorld.inst.SeedValue;
                    var gridScale = Singleton.Manager<ManWorld>.inst.CellsPerTileEdge;
                    var cellScale = (int)Singleton.Manager<ManWorld>.inst.CellScale;
                    var renderDetail = 1;
                    var renderMode = BiomeMap.RenderMode.Weighted;

                    ManWorld.inst.CurrentBiomeMap.Render(origin, size, step, seed, gridScale, cellScale, doPixel, renderDetail, renderMode);

                    var tex_size = (int)Math.Sqrt(pixels.Count);
                    var tex = new Texture2D(tex_size, tex_size);
                    tex.SetPixels(pixels.ToArray());
                    tex.Apply();
                    var bytes = tex.EncodeToPNG();

                    File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BIOME_RENDER.png"), bytes);
                    rendertime = Time.time;
                }
            }
            catch { }
        }*/

        class UIModeInitButton : MonoBehaviour, UIGameModeSettings.ModeInitSettingProvider
        {
            public void AddSettings(ManGameMode.ModeSettings modeSettings) { }

            public void InitComponent()
            {
                GetComponent<Button>().onClick.AddListener(() =>
                {
                    ManModBiomes.selector.useGUILayout = true;
                });
            }
        }
    }
}
