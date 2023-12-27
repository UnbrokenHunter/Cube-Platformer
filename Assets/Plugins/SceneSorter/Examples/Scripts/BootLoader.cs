using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSorter.Examples
{
    public class BootLoader : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(LoadMyResources());
        }

        IEnumerator LoadMyResources()
        {
            yield return new WaitForSeconds(2.0f);
            ResourcesOk = true;
            SceneManager.LoadScene("Great Menus");
        }

        public static bool ResourcesOk = false;
    }
}