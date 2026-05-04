using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuterra.World.Biomes
{
    /// <summary>
    /// TO-DO: Add new things like biome music and the sequencer patches to ManMusic to handle the external music
    /// </summary>
    internal struct BiomeWrapper
    {
        public string[] biomeGroupNames;

        public float[] biomeWeights;

        public Biome biome;
    }
}
