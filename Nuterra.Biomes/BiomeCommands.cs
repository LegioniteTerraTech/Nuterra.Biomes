using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevCommands;
using Nuterra.World.Biomes;
using RandomAdditions;
using TerraTechETCUtil;

namespace Nuterra.World
{
    public class BiomeCommands
    {
        [DevCommand(Name = ManModWorld.ModName + ".ReloadBiomes", Access = Access.Cheat, Users = User.Host)]
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
