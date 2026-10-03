using UnityEngine;

namespace GoatDescent
{
    public static class DecorMaterials
    {
        private static Material _bush;
        private static Material _bushDark;
        private static Material _rock;
        private static Material _rockDark;
        private static Material _trunk;
        private static Material _pine;
        private static Material _pineDark;
        private static Material _flowerWhite;
        private static Material _flowerYellow;
        private static Material _flowerRed;
        private static Material _grass;
        private static Material _grassDark;

        public static Material Bush => _bush ??= Mat(new Color(.30f, .52f, .24f), .25f);
        public static Material BushDark => _bushDark ??= Mat(new Color(.16f, .32f, .16f), .3f);
        public static Material Rock => _rock ??= Mat(new Color(.54f, .55f, .58f), .06f);
        public static Material RockDark => _rockDark ??= Mat(new Color(.34f, .36f, .40f), .05f);
        public static Material Trunk => _trunk ??= Mat(new Color(.30f, .20f, .11f), .05f);
        public static Material Pine => _pine ??= Mat(new Color(.10f, .30f, .16f), .3f);
        public static Material PineDark => _pineDark ??= Mat(new Color(.07f, .22f, .13f), .3f);
        public static Material FlowerWhite => _flowerWhite ??= Mat(new Color(.96f, .95f, .92f), .4f);
        public static Material FlowerYellow => _flowerYellow ??= Mat(new Color(.98f, .84f, .28f), .4f);
        public static Material FlowerRed => _flowerRed ??= Mat(new Color(.88f, .32f, .30f), .4f);
        public static Material Grass => _grass ??= Mat(new Color(.34f, .58f, .24f), .15f);
        public static Material GrassDark => _grassDark ??= Mat(new Color(.22f, .42f, .18f), .15f);

        private static Material Mat(Color color, float gloss)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", gloss);
            return material;
        }
    }
}