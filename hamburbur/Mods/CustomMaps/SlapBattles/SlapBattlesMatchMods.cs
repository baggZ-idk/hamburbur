using hamburbur.Mod_Backend;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

[hamburburmod(                "Start Tournament",   "Send the map's tournament start event", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesStartTournament : hamburburmod
{
    protected override void Pressed() => SlapBattlesNetwork.StartTournament();
}

[hamburburmod(                "Start Boss Fight",   "Start the map's boss encounter when its arena is ready", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesStartBoss : hamburburmod
{
    protected override void Pressed() => SlapBattlesNetwork.StartBoss();
}
