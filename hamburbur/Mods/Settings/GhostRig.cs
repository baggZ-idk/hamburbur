using hamburbur.Mod_Backend;

namespace hamburbur.Mods.Settings;

[hamburburmod("Ghost Rig", "Makes it so you can see where your hands are when your rig is disabled", ButtonType.Togglable, AccessSetting.Public, EnabledType.Enabled, 0)]
public class GhostRig : hamburburmod
{
    public static bool IsEnabled;
    
    protected override void OnEnable() => IsEnabled = true;
    protected override void OnDisable() => IsEnabled = false;
}