using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Linq;


[System.Serializable]
public class VideoData
{
    public string video;
}

[System.Serializable]
public class SceneData
{
    public string name;
    public VideoData[] videos;
    public float displayDuration; // in seconds – how long each video is shown
    public float SceneLength;     // in seconds – total scene duration
    public int nextScene;         // Unity scene index to load next (e.g. 1 or 2)
    public string soundtrack;     // Path to the soundtrack
    public int numberOfVideos;    // (Optional) Number of videos to play in this scene
}

[System.Serializable]
public class RootData
{
    public SceneData[] scenes;
}

public class GenerativeVideoPlayer_Screen1 : MonoBehaviour
{
    [Header("Assign Two VideoPlayers (with overlapping planes)")]
    public VideoPlayer videoPlayerA;
    public VideoPlayer videoPlayerB;

    [Header("Render copies")]
    public Renderer[] duplicateARenderers;
    public Renderer[] duplicateBRenderers;

    [Header("Data Settings")]
    public TextAsset jsonFile; // Each Unity scene has its own JSON file (scene1_data.json or scene2_data.json)
    public float preloadBuffer = 0f; // seconds before displayDuration to start preloading next clip
    public float bufferTime = 0f;    // overall delay after scene loads before starting playback

    private RootData jsonData;
    private Dictionary<string, VideoClip> videoCache = new Dictionary<string, VideoClip>();
    private List<VideoClip> videoClips;
    private List<int> videoOrder;

    private int currentVideoIndex = 0;
    private int currentSceneIndex = 0;

    private float videoTimer = 0f;
    private float sceneTimer = 0f;
    private float displayDurationSeconds;
    private float sceneLengthSeconds;

    private bool isPreparingNext = false;
    private bool isFirstVideoPrepared = false;
    private bool isSceneReady = false;
    private bool hasStartedFadeOut = false;

    private bool usingPlayerA = true;
    private SceneFader sceneFader;

    VideoPlayer ActivePlayer() { return usingPlayerA ? videoPlayerA : videoPlayerB; }
    VideoPlayer InactivePlayer() { return usingPlayerA ? videoPlayerB : videoPlayerA; }
    Renderer GetRenderer(VideoPlayer vp) { return vp.GetComponent<Renderer>(); }

    void Start()
    {
        GetRenderer(videoPlayerA).enabled = true;
        GetRenderer(videoPlayerB).enabled = false;

        videoPlayerA.waitForFirstFrame = false;
        videoPlayerA.skipOnDrop = true;
        videoPlayerB.waitForFirstFrame = false;
        videoPlayerB.skipOnDrop = true;

        sceneFader = FindObjectOfType<SceneFader>();

        LoadJsonData();
        PreloadAllVideoClips();
        LoadScene(currentSceneIndex);
    }

    void Update()
    {
        sceneTimer += Time.deltaTime;

        if (!hasStartedFadeOut && sceneFader != null && sceneTimer >= sceneLengthSeconds - sceneFader.fadeDuration - 1f)
        {
            hasStartedFadeOut = true;
            sceneFader.StartFadeOut();
        }

        if (sceneTimer >= sceneLengthSeconds)
        {
            Debug.Log("[DEBUG] SceneLength reached. Loading next scene.");
            int nextUnitySceneIndex = jsonData.scenes[currentSceneIndex].nextScene;
            SceneManager.LoadScene(nextUnitySceneIndex);
        }

        if (isSceneReady)
        {
            videoTimer += Time.deltaTime;

            if (!isPreparingNext && videoTimer >= displayDurationSeconds - preloadBuffer)
            {
                PrepareNextVideo();
            }

            if (videoTimer >= displayDurationSeconds && InactivePlayer().isPrepared)
            {
                SwapPlayers();
            }
        }
    }

    void LoadJsonData()
    {
        if (jsonFile == null)
        {
            Debug.LogError("[DEBUG] No JSON file assigned in the Inspector.");
            return;
        }
        string jsonContent = jsonFile.text;
        jsonData = JsonUtility.FromJson<RootData>(jsonContent);
        Debug.Log("[DEBUG] JSON data loaded.");
    }

    void PreloadAllVideoClips()
    {
        Debug.Log("[DEBUG] Starting to preload all video clips...");
        int totalClips = 0;
        foreach (SceneData scene in jsonData.scenes)
        {
            foreach (VideoData videoData in scene.videos)
            {
                string path = System.IO.Path.ChangeExtension(videoData.video, null);
                if (!videoCache.ContainsKey(path))
                {
                    VideoClip clip = Resources.Load<VideoClip>(path);
                    if (clip != null)
                    {
                        videoCache.Add(path, clip);
                        //Debug.Log($"[DEBUG] Preloaded video clip: {clip.name} from path: {path}");
                        totalClips++;
                    }
                    else
                    {
                        Debug.LogError("[DEBUG] Failed to preload video: " + videoData.video);
                    }
                }
                else
                {
                    Debug.Log($"[DEBUG] Video clip already preloaded for path: {path}");
                }
            }
        }
        Debug.Log($"[DEBUG] Completed preloading. Total clips preloaded: {totalClips}");
    }

    void LoadVideoClips(SceneData scene)
    {
        videoClips = new List<VideoClip>();
        foreach (VideoData videoData in scene.videos)
        {
            string path = System.IO.Path.ChangeExtension(videoData.video, null);
            if (videoCache.TryGetValue(path, out VideoClip clip))
            {
                videoClips.Add(clip);
            }
            else
            {
                Debug.LogError("[DEBUG] Clip not found in cache: " + videoData.video);
            }
        }
        Debug.Log($"[DEBUG] Loaded {videoClips.Count} video clips for scene '{scene.name}'.");
    }

    void GenerateRandomVideoOrder()
    {
        videoOrder = new List<int>();
        for (int i = 0; i < videoClips.Count; i++)
        {
            videoOrder.Add(i);
        }
        for (int i = videoOrder.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = videoOrder[i];
            videoOrder[i] = videoOrder[swapIndex];
            videoOrder[swapIndex] = temp;
        }
        Debug.Log("[DEBUG] Random video order generated.");
        Debug.Log("[DEBUG] Random video sequence: " + string.Join(", ", videoOrder.Select(i => videoClips[i].name)));

    }

    void LoadScene(int index)
    {
        currentSceneIndex = index;
        SceneData currentScene = jsonData.scenes[currentSceneIndex];

        displayDurationSeconds = currentScene.displayDuration;
        sceneLengthSeconds = currentScene.SceneLength;
        Debug.Log($"[DEBUG] Loading scene {currentSceneIndex}: '{currentScene.name}'");

        videoPlayerA.Stop();
        videoPlayerB.Stop();
        videoPlayerA.clip = null;
        videoPlayerB.clip = null;

        GetRenderer(videoPlayerA).enabled = false;
        GetRenderer(videoPlayerB).enabled = false;

        LoadVideoClips(currentScene);
        GenerateRandomVideoOrder();
        currentVideoIndex = -1;

        sceneTimer = 0f;
        videoTimer = 0f;
        isFirstVideoPrepared = false;
        isPreparingNext = false;
        isSceneReady = false;
        hasStartedFadeOut = false;

        usingPlayerA = true;
        StartCoroutine(WaitForBufferAndStart());

        if (sceneFader != null)
        {
            sceneFader.TriggerFade();
        }
    }

    IEnumerator WaitForBufferAndStart()
    {
        Debug.Log("[DEBUG] Waiting for buffer time: " + bufferTime + " seconds");
        yield return new WaitForSecondsRealtime(bufferTime);
        isSceneReady = true;
        VideoPlayer active = ActivePlayer();
        active.frameReady += OnFirstFrameReady;
        Debug.Log("[DEBUG] Buffer time complete; starting video playback and waiting for first frame.");
        PlayNextVideo();
    }

    void OnFirstFrameReady(VideoPlayer vp, long frameIdx)
    {
        vp.frameReady -= OnFirstFrameReady;
        GetRenderer(ActivePlayer()).enabled = true;
       // Debug.Log("[DEBUG] First frame ready; renderer enabled at time " + Time.realtimeSinceStartup);
    }

    void PlayNextVideo()
    {
        currentVideoIndex = (currentVideoIndex + 1) % videoOrder.Count;
        VideoClip clipToPlay = videoClips[videoOrder[currentVideoIndex]];
        Debug.Log($"[DEBUG] [ACTIVE PLAYING] Video picked: {clipToPlay.name}");
        VideoPlayer active = ActivePlayer();
        active.clip = clipToPlay;
        active.Prepare();
        active.prepareCompleted += OnActivePrepared;
        isPreparingNext = false;
    }

    void OnActivePrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnActivePrepared;
        vp.Play();
        videoTimer = 0f;
        Debug.Log($"[DEBUG] [ACTIVE PLAYED] Now playing: {vp.clip.name}");
        if (!isFirstVideoPrepared)
        {
            isFirstVideoPrepared = true;
            SceneData currentScene = jsonData.scenes[currentSceneIndex];
            Debug.Log($"[DEBUG] Starting audio for scene: {currentScene.name} at time " + Time.realtimeSinceStartup);
            StartCoroutine(DelayedAudioStart());
        }
    }

    void PrepareNextVideo()
    {
        int nextIndex = (currentVideoIndex + 1) % videoOrder.Count;
        VideoClip nextClip = videoClips[videoOrder[nextIndex]];

        Debug.Log($"[DEBUG] [PRELOADING] Video picked for preload: {nextClip.name}");
        VideoPlayer inactive = InactivePlayer();
        inactive.clip = nextClip;
        inactive.Prepare();
        inactive.prepareCompleted += OnInactivePrepared;
        isPreparingNext = true;
    }

    void OnInactivePrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnInactivePrepared;
        //Debug.Log($"[DEBUG] [PRELOADED] {vp.clip.name}");
        isPreparingNext = false;
    }

    void SwapPlayers()
    {
        Debug.Log("[DEBUG] Swapping players based on JSON timer.");
        currentVideoIndex = (currentVideoIndex + 1) % videoOrder.Count;
        usingPlayerA = !usingPlayerA;
        GetRenderer(ActivePlayer()).enabled = true;
        GetRenderer(InactivePlayer()).enabled = false;

        // ✨ Add this here:
        foreach (Renderer dup in duplicateARenderers)
            dup.enabled = usingPlayerA;

        foreach (Renderer dup in duplicateBRenderers)
            dup.enabled = !usingPlayerA;

        InactivePlayer().Stop();
        VideoPlayer active = ActivePlayer();
        if (!active.isPrepared)
        {
            active.Prepare();
            active.prepareCompleted += OnActivePrepared;
        }
        else
        {
            active.Play();
            videoTimer = 0f;
            Debug.Log($"[DEBUG] [ACTIVE PLAYING] Now playing: {active.clip.name}");
        }
        PrepareNextVideo();
    }

    IEnumerator DelayedAudioStart()
    {
        //Debug.Log("[DEBUG] DelayedAudioStart: Before waiting at time " + Time.realtimeSinceStartup);
        yield return new WaitForSecondsRealtime(4f);
        //Debug.Log("[DEBUG] DelayedAudioStart: After waiting at time " + Time.realtimeSinceStartup);
        SceneData currentScene = jsonData.scenes[currentSceneIndex];
        //Debug.Log($"[DEBUG] Starting audio for scene: {currentScene.name} at time " + Time.realtimeSinceStartup);
        SoundtrackManager.Instance.PlaySoundtrack(currentScene.name);
    }
}
