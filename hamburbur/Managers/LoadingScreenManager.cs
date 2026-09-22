using System.Collections;
using GorillaLocomotion;
using hamburbur.Components;
using hamburbur.Tools;
using UnityEngine;
using UnityEngine.Video;

namespace hamburbur.Managers;

public class LoadingScreenManager : Singleton<LoadingScreenManager>
{
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(3f);

        GameObject canvasPrefab = Plugin.Instance.HamburburBundle.LoadAsset<GameObject>("LoadingScreenCanvas");
        GameObject worldCanvasPrefab =
                Plugin.Instance.HamburburBundle.LoadAsset<GameObject>("LoadingScreenCanvasWorldSpace");

        AudioClip clip = Plugin.Instance.HamburburBundle.LoadAsset<AudioClip>("StartupSound");

        GameObject canvasInstance = Instantiate(canvasPrefab);

        GameObject worldCanvasInstance = Instantiate(worldCanvasPrefab, GTPlayer.Instance.headCollider.transform);
        worldCanvasInstance.transform.localPosition = new Vector3(0f, 0f, 1f);
        worldCanvasInstance.transform.localScale    = Vector3.one * 0.001f;
        worldCanvasInstance.SetLayer(UnityLayer.FirstPersonOnly);

        CanvasGroup group      = canvasInstance.GetComponentInChildren<CanvasGroup>();
        CanvasGroup worldGroup = worldCanvasInstance.GetComponentInChildren<CanvasGroup>();

        group.alpha      = 0f;
        worldGroup.alpha = 0f;

        Plugin.Instance.PlaySound(clip);

        while (worldGroup.alpha < 1f)
        {
            group.alpha      += Time.deltaTime * 1f;
            worldGroup.alpha += Time.deltaTime * 1f;

            yield return null;
        }

        group.alpha      = 1f;
        worldGroup.alpha = 1f;

        yield return new WaitForSeconds(clip.length - 2f);

        while (worldGroup.alpha > 0f)
        {
            group.alpha      -= Time.deltaTime * 1f;
            worldGroup.alpha -= Time.deltaTime * 1f;

            yield return null;
        }

        canvasInstance.Obliterate();
        worldCanvasInstance.Obliterate();

        Plugin.Instance.DelayedStart();
        gameObject.Obliterate();
    }

    public static IEnumerator TutorialScreen()
    {
        GTPlayer.Instance.disableMovement = true;

        yield return new WaitForSeconds(3f);

        GameObject tutorialScreen = Instantiate(Plugin.Instance.HamburburBundle.LoadAsset<GameObject>("TutorialScreen"));
        tutorialScreen.transform.position = new Vector3(-68.0419f, 11.95f, -83.5273f);
        tutorialScreen.transform.rotation = Quaternion.Euler(0f, 70f, 0f);

        // Already contains the video URL and Play On Awake is true in the AssetBundle
        VideoPlayer player = tutorialScreen.GetComponent<VideoPlayer>();

        bool shouldClose  = false;
        bool videoStarted = false;

        // Exit Button
        tutorialScreen.transform.GetChild(0).AddComponent<ButtonCollider>().OnPress += () => shouldClose = true;

        player.loopPointReached += _ => shouldClose = true;

        while (!shouldClose)
        {
            if (tutorialScreen == null)
                yield break;

            if (player.isPlaying)
                videoStarted = true;

            if (videoStarted && !player.isPlaying)
            {
                shouldClose = true;

                continue;
            }

            float distance = Vector3.Distance(
                    GTPlayer.Instance.bodyCollider.transform.position,
                    tutorialScreen.transform.position
            );

            if (distance > 10f)
                shouldClose = true;

            yield return null;
        }

        GTPlayer.Instance.disableMovement = false;

        CoroutineManager.Instance.StartCoroutine(ShrinkAndDestroy(tutorialScreen));
    }

    private static IEnumerator ShrinkAndDestroy(GameObject obj)
    {
        const float Duration = 0.25f;
        float       time     = 0f;

        Vector3 startScale = obj.transform.localScale;

        while (time < Duration)
        {
            if (obj == null)
                yield break;

            time += Time.deltaTime;

            float t = time / Duration;

            obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        GTPlayer.Instance.disableMovement = false;
        obj.Obliterate();
    }
}