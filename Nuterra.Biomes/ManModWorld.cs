using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nuterra.World.Biomes;
using Nuterra.World.Chunks;
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
        public const string ModName = "Nuterra.World";
        public static bool Init = false;

        internal static Harmony harmonyInst = new Harmony("nuterra.biomes");


        public static bool DisplayErrorsToUser = true;

        internal static bool isSteamManaged = false;
        internal static bool isNativeOptionsPresent = false;
        internal static bool isConfigHelperPresent = false;
        internal static bool isNuterraSteamPresent = false;
        internal static bool isWaterModPresent = false;
        internal static bool isRandomAdditionsPresent = false;
        public static bool LookForMod(string name) => ModStatusChecker.LookForMod(name);

        public static void Initiate()
        {
            if (Init)
                return;
            Init = true;

            LegModExt.InsurePatches();

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
            PrepExternalChunksSceneryBiomes(true);

            ManGameMode.inst.ModeSetupEvent.Subscribe(ManBiomeSave.inst.ModeStart);
            ManGameMode.inst.ModeFinishedEvent.Subscribe(ManBiomeSave.inst.ModeExit);
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

        public static void PrepExternalChunksSceneryBiomes(bool reload = false)
        {
            try
            {
                ManModChunks.SanityCheck();
                ManModChunks.PrepareAllChunks(reload);
            }
            catch (Exception e)
            {
                DebugWorld.FatalError("Failed to launch " + nameof(ManModChunks) + " - " + e);
                ManModChunks.enabled = false;
            }
            try
            {
                ManModScenery.SanityCheck();
                ManModScenery.PrepareAllScenery(reload);
            }
            catch (Exception e)
            {
                DebugWorld.FatalError("Failed to launch " + nameof(ManModScenery) + " - " + e);
                ManModScenery.enabled = false;
            }
            try
            {
                ManModBiomes.Load();
            }
            catch (Exception e)
            {
                DebugWorld.FatalError("Failed to launch " + nameof(ManModBiomes) + " - " + e);
                ManModScenery.enabled = false;
            }
        }


        private static void OnSaveManagers(bool Doing)
        {
            if (Doing)
            {
                ManModChunks.inst.PrepareForSaving();
                ManModScenery.inst.PrepareForSaving();
            }
            else
            {
                ManModChunks.inst.FinishedSaving();
                ManModScenery.inst.FinishedSaving();
            }
        }
        private static void OnLoadManagers(bool Doing)
        {
            if (Doing)
            {
                ManModChunks.inst.PrepareForLoading();
                ManModScenery.inst.PrepareForLoading();
            }
            else
            {
                ManModChunks.inst.FinishedLoading();
                ManModScenery.inst.FinishedLoading();
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
