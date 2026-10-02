using hamburbur.GUI;
using hamburbur.Mod_Backend;

[hamburburmod(                "Slap Battles",       "Open Slap Battles custom map mods", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SB : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance.SetCategory("Slap Battles");
}
