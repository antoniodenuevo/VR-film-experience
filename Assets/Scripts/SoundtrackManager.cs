using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[System.Serializable]
public class SoundSceneData
{
    public string name;
    public string soundtrack;
}

[System.Serializable]
public class SoundRootData
{
    public SoundSceneData[] scenes;
}

public class SoundtrackManager : MonoBehaviour
{
    public static SoundtrackManager Instance;

    public AudioSource audioSource;
    public TextAsset jsonFile;

    private SoundRootData data;
    private string currentSoundtrack;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

void Start()
{
    if (jsonFile == null)
    {
        Debug.LogError("No JSON file assigned to SoundtrackManager.");
        return;
    }

    data = JsonUtility.FromJson<SoundRootData>(jsonFile.text);
    Debug.Log("[DEBUG] Soundtrack JSON data loaded.");

    // This immediate call is causing audio to play right away:
    // string activeSceneName = SceneManager.GetActiveScene().name;
    // Debug.Log("[DEBUG] Active scene is: " + activeSceneName);
    // PlaySoundtrack(activeSceneName);
}


    public void PlaySoundtrack(string sceneName)
{
    if (data == null) return;
    
    // Clean the input scene name
    string cleanedSceneName = sceneName.Replace(" ", "").ToLower();

    foreach (var scene in data.scenes)
    {
        // Clean the scene name from JSON
        string jsonSceneName = scene.name.Replace(" ", "").ToLower();

        if (jsonSceneName == cleanedSceneName)
        {
            if (scene.soundtrack == currentSoundtrack) return; // already playing

            string pathWithoutExtension = Path.ChangeExtension(scene.soundtrack, null);
            AudioClip clip = Resources.Load<AudioClip>(pathWithoutExtension);

            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.volume = 0.5f;
                audioSource.Play();
                currentSoundtrack = scene.soundtrack;
                Debug.Log("Playing soundtrack: " + scene.soundtrack);
            }
            else
            {
                Debug.LogError("Failed to load soundtrack: " + scene.soundtrack);
            }
            return;
        }
    }
    Debug.LogWarning("No soundtrack found for scene: " + sceneName);
}


}
