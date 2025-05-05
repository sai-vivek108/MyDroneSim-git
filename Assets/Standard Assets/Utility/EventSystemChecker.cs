using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityStandardAssets.Utility
{
    public class EventSystemChecker : MonoBehaviour
    {
        // OnLevelWasLoaded is called when a level is loaded
        private void OnLevelWasLoaded()
        {
            if (!FindFirstObjectByType<EventSystem>())
            {
                // Instantiate the event system prefab
                GameObject obj = new GameObject("EventSystem");
                obj.AddComponent<EventSystem>();
                obj.AddComponent<StandaloneInputModule>().forceModuleActive = true;
            }
        }
    }
}
