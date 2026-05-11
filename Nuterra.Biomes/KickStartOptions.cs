using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraTechETCUtil;
using static Singleton;

namespace Nuterra.World
{
    internal class KickStartOptions
    {
        internal static ModHelper.ModConfig config;

        // NativeOptions Parameters
        // GENERAL
        public static NativeOptions.OptionToggle EnableDevPopups;
        public static void TryInitOptionAndConfig()
        {
            if (config != null)
                return;
            config = new ModHelper.ModConfig();

            config.BindConfig<ManModWorld>(null, "DevMode");
            string modName = ManModWorld.ModID;

            EnableDevPopups = new NativeOptions.OptionToggle("Enable In-Game Debugger", modName, ManModWorld.DevMode);
            EnableDevPopups.onValueSaved.AddListener(() => {
                ManModWorld.DevMode = EnableDevPopups.SavedValue;
                if (ManModWorld.DevMode)
                    ManModGUI.ShowErrorPopup("You will need to reboot the game for NuterraWorld In-Game Debugger to work");
            });

            NativeOptions.NativeOptionsMod.onOptionsSaved.AddListener(() =>
            {
                try
                {
                    config.WriteConfigJsonFile();
                }
                catch (Exception e)
                {
                    DebugWorld.Log(ManModWorld.Tag, e);
                }
            });
        }
    }
}
