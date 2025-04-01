using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage;         // Assign in inspector
    public float fadeDuration = 1f; // Seconds
    public float fadeDelay = 0f;    // Delay before fade starts

    private bool isFadingOut = false;

    void Start()
    {
        if (fadeImage != null)
        {
            Debug.Log("[FADE] Starting fade...");
            StartCoroutine(FadeFromBlack());
        }
    }

    public void TriggerFade()
    {
        if (fadeImage != null)
        {
            Debug.Log("[FADE] Triggering fade manually...");
            StartCoroutine(FadeFromBlack());
        }
    }

    public void StartFadeOut()
    {
        if (!isFadingOut && fadeImage != null)
        {
            isFadingOut = true;
            StartCoroutine(FadeToBlack());
        }
    }

    IEnumerator FadeFromBlack()
    {
        yield return new WaitForSeconds(fadeDelay);
        Debug.Log("[FADE] Coroutine started (fade in).");

        Color color = fadeImage.color;
        float elapsed = 0f;
        fadeImage.raycastTarget = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        fadeImage.raycastTarget = false;
    }

    IEnumerator FadeToBlack()
    {
        Debug.Log("[FADE] Starting fade to black...");

        Color color = fadeImage.color;
        float elapsed = 0f;
        fadeImage.raycastTarget = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }
}
