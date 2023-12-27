using UnityEngine;

namespace Porkchop.SceneSorter
{
    public static class SceneSorterGlobals
    {
        public enum SSGTexture
        {
            Logo,
            LogoStretch,
            LogoCap,
            Load,
            LoadFilled,
            Play,
            Love,
            LoveFilled,
            Up,
            Down,
            Focus,
            Hide,
            Show,
            Total
        };

        private static Texture2D[] _allTextures;

        public static Texture2D GetTexture(SSGTexture e)
        {
            if (_allTextures == null || _allTextures[(int)e] == null)
            {
                LoadMyResources();
            }

            return _allTextures[(int)e];
        }

        public static bool ResourcesOk = false;
        public static bool TrimLongStrings = true;

        public const float ButtonWidth = 32;
        public const float ButtonHeight = 32;

        public const float NoBorderButtonWidth = 24;
        public const float NoBorderButtonHeight = 24;

        public const float LeftMargin = 10.0f;
        public const float RightMargin = 10.0f;

        public const float HorizPad = 2.0f;
        public const float VertPad = 2.0f;

        public const float LabelTopMargin = 8.0f;

        public const float NameWidth = 600.0f;
        public const float NameHeight = 32.0f;

        private const string VersionString = "1.2";

        public static void LoadMyResources()
        {
            int total = (int)SSGTexture.Total;
            if (_allTextures == null)
            {
                _allTextures = new Texture2D[total];
            }

            // Load the textures we need.
            ResourcesOk = true;
            for (int i = 0; i < total; i++)
            {
                string filename = "SSG" + (SSGTexture)i;
                object resource = Resources.Load(filename);
                if (resource == null)
                {
                    filename += ".png";
                    resource = Resources.Load(filename);
                }

                _allTextures[i] = resource as Texture2D;
                if (_allTextures[i] == null)
                {
                    Debug.Log(
                        $"SceneSorter {VersionString}, failed to load {filename}, try forcing Unity to reimport any file, and if that fails reinstall the Unity package.");
                    ResourcesOk = false;
                }
            }
        }
    }
}
