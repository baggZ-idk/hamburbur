using hamburbur.GUI;
using hamburbur.Mod_Backend;

namespace hamburbur.Mods.CustomMaps;

[hamburburmod("Xeno Gorilla", "Open Xeno Gorilla custom map mods", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class XG : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance.SetCategory("Xeno Gorilla");
}
