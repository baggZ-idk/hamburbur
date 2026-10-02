using System.Linq;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.XenoGorilla;

public abstract class XenoLandmarkTeleport : hamburburmod
{
    protected abstract string Landmark { get; }

    protected override void Pressed()
    {
        if (!XenoNetwork.IsInMap) return;
        GameObject point = GameObject.Find(Landmark) ?? Resources.FindObjectsOfTypeAll<GameObject>()
                                   .FirstOrDefault(item => item.name == Landmark && item.scene.IsValid());
        if (point != null) Tools.Utils.TeleportPlayer(point.transform.position + Vector3.up);
    }
}

[hamburburmod("Visit Blue Lucy Tombstone", "Teleport to Blue Lucy's summoning tombstone", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitBlueLucyTombstone : XenoLandmarkTeleport { protected override string Landmark => "TombstonePos"; }

[hamburburmod("Visit Lucyfer Site", "Teleport to Lucyfer's summoning site", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitLucyferSite : XenoLandmarkTeleport { protected override string Landmark => "LucyferPos"; }

[hamburburmod("Visit Error Lucy Stand", "Teleport to Error Lucy's summoning stand", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitErrorLucyStand : XenoLandmarkTeleport { protected override string Landmark => "ErrorLucyStand"; }

[hamburburmod("Visit White Lucy Grave", "Teleport to White Lucy's grave", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitWhiteLucyGrave : XenoLandmarkTeleport { protected override string Landmark => "WhiteLucyGrave"; }

[hamburburmod("Visit Tiptoe Spawn", "Teleport to Tiptoe's summoning site", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitTiptoeSpawn : XenoLandmarkTeleport { protected override string Landmark => "TiptoeSpawn"; }

[hamburburmod("Visit Echo Obelisk", "Teleport to Echo's summoning obelisk", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitEchoObelisk : XenoLandmarkTeleport { protected override string Landmark => "EchoObelisk"; }

[hamburburmod("Visit City Mirror", "Teleport to Mirror Man's city mirror", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitCityMirror : XenoLandmarkTeleport { protected override string Landmark => "CityMirror"; }

[hamburburmod("Visit Angel Stand", "Teleport to Angel's summoning stand", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitAngelStand : XenoLandmarkTeleport { protected override string Landmark => "AngelStand"; }

[hamburburmod("Visit Door Knock", "Teleport to the door encounter", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitDoorKnock : XenoLandmarkTeleport { protected override string Landmark => "DoorKnock"; }

[hamburburmod("Visit Xeno Intro", "Teleport to the normal intro spawn point", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVisitIntro : XenoLandmarkTeleport { protected override string Landmark => "ErrorBGM"; }
