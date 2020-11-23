using System;

namespace ImpostorHQ.Plugin.Fashionable.Designer
{
    public class Structures
    {
        public class ItemConverter
        {
            public SkinId[] Skins = (SkinId[])Enum.GetValues(typeof(SkinId));
            public HatId[] Hats = (HatId[])Enum.GetValues(typeof(HatId));
            public PetId[] Pets = (PetId[])Enum.GetValues(typeof(PetId));
        }
        public enum HatId
        {
            NoHat = 0x00,
            Astronaut = 0x01,
            BaseballCap = 0x02,
            BrainSlug = 0x03,
            BushHat = 0x04,
            CaptainsHat = 0x05,
            DoubleTopHat = 0x06,
            Flowerpot = 0x07,
            Goggles = 0x08,
            HardHat = 0x09,
            Military = 0x0a,
            PaperHat = 0x0b,
            PartyHat = 0x0c,
            Police = 0x0d,
            Stethescope = 0x0e,
            TopHat = 0x0f,
            TowelWizard = 0x10,
            Ushanka = 0x11,
            Viking = 0x12,
            WallCap = 0x13,
            Snowman = 0x14,
            Reindeer = 0x15,
            Lights = 0x16,
            Santa = 0x17,
            Tree = 0x18,
            Present = 0x19,
            Candycanes = 0x1a,
            ElfHat = 0x1b,
            NewYears2018 = 0x1c,
            WhiteHat = 0x1d,
            Crown = 0x1e,
            Eyebrows = 0x1f,
            HaloHat = 0x20,
            HeroCap = 0x21,
            PipCap = 0x22,
            PlungerHat = 0x23,
            ScubaHat = 0x24,
            StickminHat = 0x25,
            StrawHat = 0x26,
            TenGallonHat = 0x27,
            ThirdEyeHat = 0x28,
            ToiletPaperHat = 0x29,
            Toppat = 0x2a,
            Fedora = 0x2b,
            Goggles_2 = 0x2c,
            Headphones = 0x2d,
            MaskHat = 0x2e,
            PaperMask = 0x2f,
            Security = 0x30,
            StrapHat = 0x31,
            Banana = 0x32,
            Beanie = 0x33,
            Bear = 0x34,
            Cheese = 0x35,
            Cherry = 0x36,
            Egg = 0x37,
            Fedora_2 = 0x38,
            Flamingo = 0x39,
            FlowerPin = 0x3a,
            Helmet = 0x3b,
            Plant = 0x3c,
            BatEyes = 0x3d,
            BatWings = 0x3e,
            Horns = 0x3f,
            Mohawk = 0x40,
            Pumpkin = 0x41,
            ScaryBag = 0x42,
            Witch = 0x43,
            Wolf = 0x44,
            Pirate = 0x45,
            Plague = 0x46,
            Machete = 0x47,
            Fred = 0x48,
            MinerCap = 0x49,
            WinterHat = 0x4a,
            Archae = 0x4b,
            Antenna = 0x4c,
            Balloon = 0x4d,
            BirdNest = 0x4e,
            BlackBelt = 0x4f,
            Caution = 0x50,
            Chef = 0x51,
            CopHat = 0x52,
            DoRag = 0x53,
            DumSticker = 0x54,
            Fez = 0x55,
            GeneralHat = 0x56,
            GreyThing = 0x57,
            HunterCap = 0x58,
            JungleHat = 0x59,
            MiniCrewmate = 0x5a,
            NinjaMask = 0x5b,
            RamHorns = 0x5c,
            Snowman_2 = 0x5d
        }
        public enum PetId
        {
            None = 0x00,
            Alien = 0x01,
            Crewmate = 0x02,
            Doggy = 0x03,
            Stickmin = 0x04,
            Hamster = 0x05,
            Robot = 0x06,
            UFO = 0x07,
            Ellie = 0x08,
            Squig = 0x09,
            Bedcrab = 0x0a
        }
        public enum SkinId
        {
            None = 0x00,
            Astro = 0x01,
            Capt = 0x02,
            Mech = 0x03,
            Military = 0x04,
            Police = 0x05,
            Science = 0x06,
            SuitB = 0x07,
            SuitW = 0x08,
            Wall = 0x09,
            Hazmat = 0x0a,
            Security = 0x0b,
            Tarmac = 0x0c,
            Miner = 0x0d,
            Winter = 0x0e,
            Archae = 0x0f
        }
    }
    public class Skin
    {
        public Structures.SkinId Clothes { get; set; }
        public Structures.HatId Hat { get; set; }
        public Structures.PetId Pet { get; set; }
        public string Name { get; set; }
        public Skin()
        {
            Name = "N/A";
        }

        public override string ToString()
        {
            return $"{{\"Clothes\":{(int)Clothes},\"Hat\":{(int)Hat},\"Pet\":{(int)Pet},\"Name\":\"{Name}\"}}";
        }

    }
}
