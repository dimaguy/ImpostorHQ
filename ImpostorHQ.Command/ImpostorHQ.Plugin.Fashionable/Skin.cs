using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Impostor.Api.Innersloth.Customization;
using Impostor.Commands.Core;

namespace ImpostorHQ.Plugin.Fashionable
{
    public class ItemConverter
    {
        public Random Random = new Random();
        public SkinType[] Skins = (SkinType[])Enum.GetValues(typeof(Impostor.Api.Innersloth.Customization.SkinType));
        public HatType[] Hats = (HatType[])Enum.GetValues(typeof(Impostor.Api.Innersloth.Customization.HatType));
        public PetType[] Pets = (PetType[])Enum.GetValues(typeof(Impostor.Api.Innersloth.Customization.PetType));

        public SkinType GetRandomSkin()
        {
            return Skins[Random.Next(0, Skins.Length)];
        }
        public PetType GetRandomPet()
        {
            return Pets[Random.Next(0, Pets.Length)];
        }
        public HatType GetRandomHat()
        {
            return Hats[Random.Next(0, Hats.Length)];
        }
    }

    public class Skin
    {
        public SkinType Clothes { get; set; }
        public HatType Hat { get; set; }
        public PetType Pet { get; set; }
        public string Name { get; set; }
        public Skin()
        {
            Name = "N/A";
        }

        public static Skin GetRandomSkin(ItemConverter converter)
        {
            return new Skin()
            {
                Clothes =  (SkinType)converter.GetRandomSkin(),
                Hat = (HatType)converter.GetRandomHat(),
                Pet = (PetType)converter.GetRandomPet()
            };
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public static Skin FromString(string str)
        {
            return JsonSerializer.Deserialize<Skin>(str);
        }

        public static IEnumerable<Skin> FromDir(string dir)
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                if (file.EndsWith(".hqf") && file.Contains("fashionable-"))
                {
                    yield return Skin.FromString(File.ReadAllText(file));
                }
            }
        }
    }
}
