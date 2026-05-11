using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nuterra.World.Biomes;
using Nuterra.World.Chunks;
using Nuterra.World.PatchBatch;
using Nuterra.World.Scenery;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World
{
    /// <summary>
    /// Master manager <b>of the world!</b>
    /// </summary>
    public class ManModWorld
    {
        public const string Tag = "";
        public const string ModLogName = "Nuterra.World";
        public const string ModID = "NuterraWorld Beta";
        public static bool Init = false;
        public static bool ReloadALL = false;

        internal static Harmony harmonyInst = new Harmony("nuterra.biomes");

        internal static bool isSteamManaged = false;
        internal static bool isNativeOptionsPresent = false;
        internal static bool isConfigHelperPresent = false;
        internal static bool isNuterraSteamPresent = false;
        internal static bool isWaterModPresent = false;
        internal static bool isRandomAdditionsPresent = false;

        internal static int IterateExtraRate = Mathf.Clamp(Mathf.RoundToInt(4 / Time.deltaTime), 1, 1000);


        public static bool DevMode = false;
        public static bool DisplayErrorsToUser => DevMode;


        public static bool LookForMod(string name) => ModStatusChecker.LookForMod(name);

        public static void Initiate()
        {
            if (Init)
                return;
            Init = true;

            LegModExt.InsurePatches();
            MassPatcher.MassPatchAllWithin(harmonyInst, typeof(GlobalPatches), ModLogName, true);

            isSteamManaged = LookForMod("NLogManager");

            if (!LookForMod("0Harmony"))
            {
                DebugWorld.Log(Tag, "This mod NEEDS Harmony to function!  Please subscribe to it on the Steam Workshop.");
                return;
            }

            isNativeOptionsPresent = ModStatusChecker.IsNativeOptionsPresent;
            isConfigHelperPresent = ModStatusChecker.IsConfigHelperPresent;
            if (isConfigHelperPresent && isNativeOptionsPresent)
            {
                try
                {
                    InitOptions();
                }
                catch (Exception e)
                {
                    DebugWorld.Log(Tag, "Error on Init options and config - " + e);
                }
            }
            
            isNuterraSteamPresent = LookForMod("NuterraSteam");
            if (isNuterraSteamPresent)
                DebugWorld.Log(Tag, "Found NuterraSteam!  Making sure blocks work!");

            isWaterModPresent = LookForMod("WaterMod");
            if (isWaterModPresent)
                DebugWorld.Log(Tag, "Found Water Mod!  Enabling water-related features!");

            isRandomAdditionsPresent = LookForMod("RandomAdditions");
            if (isRandomAdditionsPresent)
                DebugWorld.Log(Tag, "Found RandomAdditions!  Enabling random added features!");

            try
            {
                SafeSaves.ManSafeSaves.RegisterSaveSystem(Assembly.GetExecutingAssembly(), OnSaveManagers, OnLoadManagers);
            }
            catch (Exception e)
            {
                DebugWorld.Log(Tag, "Error on RegisterSaveSystem - " + e);
            }
            ManGameMode.inst.ModeSetupEvent.Subscribe(ManBiomeSave.inst.ModeStart);
            ManGameMode.inst.ModeFinishedEvent.Subscribe(ManBiomeSave.inst.ModeExit);
            ManGameMode.inst.ModeCleanUpEvent.Subscribe(NuterraRes.DestroyAllPending);
            PrepExternalChunksSceneryBiomes();
        }
        private static void InitOptions()
        {
            try
            {
                KickStartOptions.TryInitOptionAndConfig();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public static void DeInitiate()
        {
            if (!Init)
                return;

        }

        /// <summary>
        /// Chunks -> Scenery -> Biomes
        /// <para>Based on order of requirements</para>
        /// </summary>
        /// <param name="reload"></param>
        public static void PrepExternalChunksSceneryBiomes()
        {
            SpawnHelper.GrabInitList();

            DebugWorld.Log(Tag, "PrepExternalChunksSceneryBiomes()");
            try
            {
                ManModChunks.enabled = true;
                ManModChunks.SanityCheck();
            }
            catch (Exception e)
            {
                DebugWorld.FatalError("Failed to launch " + nameof(ManModChunks) + " - " + e);
                ManModChunks.enabled = false;
            }
            if (ManModChunks.enabled)
                InvokeHelper.PreloadThis(ManModChunks.inst);

            try
            {
                ManModScenery.enabled = true;
                ManModScenery.SanityCheck();
            }
            catch (Exception e)
            {
                DebugWorld.FatalError("Failed to launch " + nameof(ManModScenery) + " - " + e);
                ManModScenery.enabled = false;
            }
            if (ManModScenery.enabled)
                InvokeHelper.PreloadThis(ManModScenery.inst);

            DebugWorld.Log(Tag, "ManModBiomes()");
            InvokeHelper.PreloadThis(new ManModBiomes());
        }


        private static void OnSaveManagers(bool Doing)
        {
            if (Doing)
            {
                ManBiomeSave.inst.PrepareForSaving();
                ManModScenery.inst.PrepareForSaving();
                ManModChunks.inst.PrepareForSaving();
            }
            else
            {
                ManBiomeSave.inst.FinishedSaving();
                ManModScenery.inst.FinishedSaving();
                ManModChunks.inst.FinishedSaving();
            }
        }
        private static void OnLoadManagers(bool Doing)
        {
            if (Doing)
            {
                ManModChunks.inst.PrepareForLoading();
                ManModScenery.inst.PrepareForLoading();
                ManBiomeSave.inst.PrepareForLoading();
            }
            else
            {
                ManModChunks.inst.FinishedLoading();
                ManModScenery.inst.FinishedLoading();
                ManBiomeSave.inst.FinishedLoading();
            }
        }


        internal static void OpenInExplorer(string directory)
        {
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.MacOSX:
                    Process.Start(new ProcessStartInfo("file://" + directory));
                    break;
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.Windows:
                    Process.Start(new ProcessStartInfo("explorer.exe", directory));
                    break;
                default:
                    throw new Exception("This operating system is UNSUPPORTED by RandomAdditions");
            }
        }
    }
}
