using hamburbur.Mod_Backend;

namespace hamburbur.Mods.Room;

[hamburburmod("Disconnect", "Disconnects you from the current room", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Disconnect : hamburburmod
{
    protected override void Pressed() => NetworkSystem.Instance.ReturnToSinglePlayer();
}