using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace SceneSorter.Examples
{

    public class GreatMenus : MonoBehaviour
    {
        public static bool LoadedFromMenu = false;

        void Start()
        {
            Assert.IsTrue(BootLoader.ResourcesOk, "You did not load the resources from the boot scene!");
        }

        public void OnPlayGame()
        {
            if (BootLoader.ResourcesOk)
            {
                LoadedFromMenu = true;
                SceneManager.LoadScene("Fun Gameplay");
            }
            else
            {
                Debug.LogWarning("You didn't load your resources!");
            }
        }
    }
}
