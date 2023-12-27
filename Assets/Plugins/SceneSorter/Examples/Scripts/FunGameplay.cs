using UnityEngine;
using UnityEngine.Assertions;

namespace SceneSorter.Examples
{
    public class FunGameplay : MonoBehaviour
    {
        void Start()
        {
            Assert.IsTrue(GreatMenus.LoadedFromMenu, "You did not load from the menus!");
        }
    }
}
