using UnityEngine;

public class QuitApplication : MonoBehaviour
{
    public void QuitApp()
    {
        Debug.Log("Resetting app state and quitting...");

        ResetPersistentData();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void ResetPersistentData()
    {
        // Clear PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // OPTIONAL: Delete saved files (if any)
        // string savePath = Application.persistentDataPath + "/save.json";
        // if (System.IO.File.Exists(savePath))
        //     System.IO.File.Delete(savePath);
    }
}
