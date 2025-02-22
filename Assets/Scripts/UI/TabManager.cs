using UnityEngine;

public class TabManager : MonoBehaviour
{
    // The currently active tab (set via the Inspector or at runtime)
    [SerializeField] private GameObject currentTab;

    public void SwitchTab(GameObject newTab)
    {
        // If we're already on this tab, nothing to do.
        if (currentTab == newTab)
        {
            return;
        }

        currentTab.SetActive(false);

        newTab.SetActive(true);

        // Update our current tab reference.
        currentTab = newTab;
    }
}
