using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Porkchop.SceneSorter
{

    public class SceneSorterElement
    {
        public bool favorite;
        public bool hidden;
        public string name;
        public string trimmedName;
    }

    public static class SceneSorterSerializer
    {
        public static string GetKey()
        {
            return (PlayerSettings.companyName + "." + PlayerSettings.productName + ".SceneSorter");
        }

        public static List<SceneSorterElement> Load()
        {
            List<SceneSorterElement> result = new List<SceneSorterElement>();
            string key = GetKey();
            if (EditorPrefs.HasKey(key))
            {
                string csv = EditorPrefs.GetString(key);
                var tokens = csv.Split(',');
                int total = tokens.Length / 2;
                for (int i = 0; i < total; i++)
                {
                    int idx = i * 2;
                    SceneSorterElement element = new SceneSorterElement();
                    if (tokens[idx] == "2")
                        element.hidden = true;
                    if (tokens[idx] == "1")
                        element.favorite = true;
                    else
                        element.favorite = false;
                    element.name = tokens[idx + 1];

                    // Only add it if the scene still exists in the same directory structure.
                    if (SceneExists(element.name))
                    {
                        result.Add(element);
                    }
                }
            }
            else
            {
                // The key is missing so we havent saved prefs for this project.
            }

            return result;
        }

        private static bool SceneExists(string name)
        {
            string fullPath = Application.dataPath + "/" + name + ".unity";
            if (File.Exists(fullPath))
                return true;
            return false;
        }

        public static void Save(List<SceneSorterElement> elements)
        {
            if (elements == null || elements.Count == 0)
                return;

            // To a csv string.
            StringBuilder sb = new StringBuilder();

            foreach (var element in elements)
            {
                if(element.hidden)
                    sb.Append("2,");
                else if (element.favorite)
                    sb.Append("1,");
                else
                    sb.Append("0,");
                sb.Append(element.name);
                sb.Append(",");
            }

            EditorPrefs.SetString(GetKey(), sb.ToString());
        }
    }

}