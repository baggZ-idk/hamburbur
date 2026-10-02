using GorillaLocomotion;
using hamburbur.Mod_Backend;
using hamburbur.Mods.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace hamburbur.Mods.Misc;

[hamburburmod(                "PC Press Buttons", "Lets you press buttons on PC", ButtonType.Togglable, AccessSetting.Public,
        EnabledType.Disabled, 0)]
public class PCPressButtons : hamburburmod
{
    private LayerMask acceptedLayers;

    private GameObject handIndicator;
    private TransformFollow handIndicatorTransformFollow;

    protected override void Start()
    {
        handIndicator = GorillaTagger.Instance.rightHandTriggerCollider;
        handIndicatorTransformFollow = handIndicator.GetComponent<TransformFollow>();

        acceptedLayers = 1 << 18;
    }

    protected override void FixedUpdate()
    {
        if (!Mouse.current.leftButton.isPressed)
        {
            if (!handIndicatorTransformFollow.enabled)
                handIndicatorTransformFollow.enabled = true;
            
            return;
        }

        Camera cameraToUse = Tools.Utils.GetActiveCamera();

        Ray ray = cameraToUse.ScreenPointToRay(Mouse.current.position.ReadValue());
        Physics.Raycast(ray, out RaycastHit hit, 512f, Tools.Utils.NoInvisLayerMask());

        handIndicatorTransformFollow.enabled = false;
        handIndicator.transform.position     = hit.point;

        if (MoveHands.IsEnabled)
            GTPlayer.Instance.leftHand.controllerTransform.position = hit.point;
    }
}