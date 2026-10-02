using System.Linq;
using hamburbur.Mod_Backend;
using hamburbur.Mods.Settings;
using UnityEngine;

namespace hamburbur.Mods.Visual;

[hamburburmod("Disable Leaves", "Disables the Leaves", ButtonType.Togglable, AccessSetting.Public,
        EnabledType.Disabled,
        0)]
public class DisableLeaves : hamburburmod
{
    private const int LeavesIndex = 27;

    // If you need to update the leaves index above. Get the latest temp file name from https://hamburbur.org/data or get it manually
    // Then paste it in this constant below and make sure to compile with Constants.BetaBuild as true, enable the mod and look in your logs
    private const string LeafName = "";

    private string LeavesName
    {
        get
        {
            // ReSharper disable once InvertIf
            if (field == null)
            {
                Transform[] forestChildren = GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest").transform
                                                       .GetComponentsInChildren<Transform>(true)
                                                       .Where(t => t.name.Contains("UnityTempFile")).ToArray();

                if (LeavesIndex >= 0 && LeavesIndex < forestChildren.Length)
                    field = forestChildren[LeavesIndex].gameObject.name;
            }

            return field;
        }
    }

    protected override void OnEnable()
    {
        ChangeObjectVisibility(FirstPersonVisuals.FirstPersonOnly);
        FirstPersonVisuals.OnFirstPersonOnlyChange += ChangeObjectVisibility;

        if (Constants.BetaBuild && !LeavesName.IsNullOrEmpty())
            FindLeavesIndex();
    }

    private static void FindLeavesIndex()
    {
        Transform[] forestChildren = GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest").transform
                                               .GetComponentsInChildren<Transform>(true)
                                               .Where(t => t.name.Contains("UnityTempFile"))
                                               .ToArray();

        for (int index = 0; index < forestChildren.Length; index++)
        {
            Transform child = forestChildren[index];

            if (LeafName != child.gameObject.name)
                continue;

            Debug.Log($"[No Leaves Debug] Index: {index}  |  Name: {child.gameObject.name}");
        }
    }

    protected override void OnDisable()
    {
        FirstPersonVisuals.OnFirstPersonOnlyChange -= ChangeObjectVisibility;

        foreach (Transform child in GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest").transform)
            if (child.gameObject.name == LeavesName)
            {
                child.gameObject.SetActive(true);
                child.gameObject.SetLayer(UnityLayer.Default);
            }
    }

    private void ChangeObjectVisibility(bool firstPersonOnly)
    {
        foreach (Transform child in GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest").transform)
            if (child.gameObject.name == LeavesName)
            {
                child.gameObject.SetActive(firstPersonOnly);
                child.gameObject.SetLayer(firstPersonOnly ? UnityLayer.MirrorOnly : UnityLayer.Default);
            }
    }
}