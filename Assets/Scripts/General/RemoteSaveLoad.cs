using UnityEngine;

public class RemoteSaveLoad : MonoBehaviour
{
    public void LoadGame()
    {
        GameManager.Instance?.LoadGame("SaveFile1");
    }
}
