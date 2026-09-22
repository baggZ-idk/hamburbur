using hamburbur.Mod_Backend;

namespace hamburbur.Mods.Rig;

[hamburburmod("Max Quest Score", "Gives you the highest possible value on the Monke Biz Badge", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class MaxQuestScore : hamburburmod
{
    protected override void OnEnable()
    {
        SetTheScoreYeah();
        NetworkSystem.Instance.OnJoinedRoomEvent += SetTheScoreYeah;
    }
    
    protected override void OnDisable() => NetworkSystem.Instance.OnJoinedRoomEvent -= SetTheScoreYeah;

    private static void SetTheScoreYeah() => VRRig.LocalRig.SetQuestScore(int.MaxValue);
}