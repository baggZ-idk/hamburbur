using hamburbur.Mod_Backend;

namespace hamburbur.Mods.Settings;

[hamburburmod("Favourite Mods", "Allows grip-modified physical presses to favourite mods", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class FavouriteModsSetting : hamburburmod
{
    public static bool IsEnabled { get; private set; }

    protected override void OnEnable()  => IsEnabled = true;
    protected override void OnDisable() => IsEnabled = false;
}
