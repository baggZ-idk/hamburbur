using hamburbur.GUI;
using hamburbur.Tools;
using UnityEngine;

namespace hamburbur.Components;

public class ColourChanger : MonoBehaviour
{

    public  float    alpha = 1f;
    private float    elapsedTime;
    private Renderer meshRenderer;
    private Renderer renderer;

    private bool useMeshRenderer;

    private void Start()
    {
        if (gameObject.TryGetComponent(out renderer))
            useMeshRenderer = false;

        else if (!gameObject.TryGetComponent(out meshRenderer))
            useMeshRenderer = true;

        else
            this.Obliterate();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        float time = Mathf.PingPong(elapsedTime, 1f);

        if (useMeshRenderer)
            meshRenderer.material.color = Color.Lerp(new Color(MenuHandler.Instance.currentMainColour.r, MenuHandler.Instance.currentMainColour.g, MenuHandler.Instance.currentMainColour.b, alpha), new Color(MenuHandler.Instance.currentSecondaryColour.r, MenuHandler.Instance.currentSecondaryColour.g, MenuHandler.Instance.currentSecondaryColour.b, alpha), time);
        else
            renderer.material.color = Color.Lerp(new Color(MenuHandler.Instance.currentMainColour.r, MenuHandler.Instance.currentMainColour.g, MenuHandler.Instance.currentMainColour.b, alpha), new Color(MenuHandler.Instance.currentSecondaryColour.r, MenuHandler.Instance.currentSecondaryColour.g, MenuHandler.Instance.currentSecondaryColour.b, alpha), time);
    }
}