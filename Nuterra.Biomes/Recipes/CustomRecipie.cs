using System;
using Newtonsoft.Json;
using Nuterra.ModLoading;

namespace Nuterra.World.Recipes
{
    public class CustomRecipe : ModLoadable
    {
        [Doc("The name of the recipie.  Must be unique")]
        public string Name = "Terry";
    }
}
