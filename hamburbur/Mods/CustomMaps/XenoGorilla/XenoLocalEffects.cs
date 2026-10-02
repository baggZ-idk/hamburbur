using System.Collections.Generic;
using System.Linq;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.XenoGorilla;

internal static class XenoScene
{
    internal static GameObject Find(string name) => GameObject.Find(name) ??
                                                    Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(item => item.name == name && item.scene.IsValid());
}

[hamburburmod("Clear Xeno Horror Overlays", "Locally hides static, Lucy, Dimwi, and Flowery screen effects", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoClearHorrorOverlays : hamburburmod
{
    private static readonly string[]                       OverlayNames = ["StaticOverlay", "ErrorLucyOverlay", "DimwiJumpscare", "FloweryOverlay",];
    private readonly        Dictionary<string, GameObject> overlays     = new();

    protected override void LateUpdate()
    {
        if (!XenoNetwork.IsInMap) return;
        foreach (string name in OverlayNames)
        {
            if (!overlays.TryGetValue(name, out GameObject effect) || effect == null)
                overlays[name] = effect = XenoScene.Find(name);

            if (effect != null && effect.activeSelf) effect.SetActive(false);
        }
    }

    protected override void OnDisable() => overlays.Clear();
}

[hamburburmod("Mute Xeno Jumpscare Sounds", "Locally mutes the map's jumpscare and scream sources", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoMuteJumpscareSounds : hamburburmod
{
    private static readonly string[]                      SourceNames  = ["JumpscareSFX", "Scream1SFX", "Scream2SFX", "Scream3SFX",];
    private readonly        Dictionary<AudioSource, bool> originalMute = new();

    protected override void LateUpdate()
    {
        if (!XenoNetwork.IsInMap) return;
        foreach (string name in SourceNames)
        {
            AudioSource source = XenoScene.Find(name)?.GetComponent<AudioSource>();

            if (source == null) continue;
            if (!originalMute.ContainsKey(source)) originalMute[source] = source.mute;
            source.mute = true;
        }
    }

    protected override void OnDisable()
    {
        foreach (KeyValuePair<AudioSource, bool> entry in originalMute)
            if (entry.Key != null)
                entry.Key.mute = entry.Value;

        originalMute.Clear();
    }
}

[hamburburmod("Reset Xeno Lighting", "Locally retriggers the map's lighting reset object", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoResetLighting : hamburburmod
{
    protected override void Pressed()
    {
        if (!XenoNetwork.IsInMap) return;
        GameObject reset = XenoScene.Find("ResetLighting");

        if (reset == null) return;
        reset.SetActive(false);
        reset.SetActive(true);
    }
}