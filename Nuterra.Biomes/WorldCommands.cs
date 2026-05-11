using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevCommands;
using Nuterra.World.Biomes;
using Nuterra.World.Chunks;
using RandomAdditions;
using TerraTechETCUtil;

namespace Nuterra.World
{
    public class WorldCommands
    {
        [DevCommand(Name = ManModWorld.ModLogName + ".ReloadChunks", Access = Access.Cheat, Users = User.Host)]
        public static CommandReturn ReloadChunks()
        {
            ManModChunks.inst.CreateLocal(true, ManModChunks.FolderPath);
            return new CommandReturn()
            {
                message = "Success, please wait...",
                success = true,
            };
        }
        [DevCommand(Name = ManModWorld.ModLogName + ".ReloadScenery", Access = Access.Cheat, Users = User.Host)]
        public static CommandReturn ReloadScenery()
        {
            ManModChunks.inst.CreateLocal(true, ManModChunks.FolderPath);
            return new CommandReturn()
            {
                message = "Success, please wait... You may need to reload biomes to fully see the changes.",
                success = true,
            };
        }
        [DevCommand(Name = ManModWorld.ModLogName + ".ReloadBiomes", Access = Access.Cheat, Users = User.Host)]
        public static CommandReturn ReloadBiomes()
        {
            ManModBiomes.InitiateMassLoading();
            return new CommandReturn()
            {
                message = "Success, please wait...",
                success = true,
            };
        }
    }
}
