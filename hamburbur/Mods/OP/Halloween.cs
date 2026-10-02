using GorillaLocomotion.Gameplay;
using GorillaTagScripts;
using hamburbur.Libs;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.OP;

public static class HalloweenInteractables
{
    public static HalloweenGhostChaser Lucy
    {
        get
        {
            field ??= GameObject.Find("Environment Objects/05Maze_PersistentObjects/Ghosts/Halloween Ghost/FloatingChaseSkeleton").GetComponent<HalloweenGhostChaser>();

            return field;
        }
    }

    public static LurkerGhost Lurker
    {
        get
        {
            field ??= GameObject.Find("Environment Objects/05Maze_PersistentObjects/Ghosts/GhostLurker_Prefab").GetComponent<LurkerGhost>();

            return field;
        }
    }

    public static GameObject Terraformer
    {
        get
        {
            field ??= GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest/2026_Halloween_Forest/Terraformer");
            
            return field;
        }
    }

    public static NoncontrollableBroomstick[] Broomsticks
    {
        get
        {
            const string BroomsticksPath = "Environment Objects/LocalObjects_Prefab/Forest/2026_Halloween_Forest/Broomsticks/";

            return [
                           GameObject.Find(BroomsticksPath + "/Broomstick2/NoncontrollableBroomstick").GetComponent<NoncontrollableBroomstick>(),
                           GameObject.Find(BroomsticksPath + "/Broomstick3/NoncontrollableBroomstick").GetComponent<NoncontrollableBroomstick>(),
                   ];
        }
    }
}

[hamburburmod("Enable Halloween Terraformer", "Enabled the 'Terraformer' from the halloween update", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class EnableTerraformer : hamburburmod
{
    protected override void OnEnable() => HalloweenInteractables.Terraformer.SetActive(true);
    protected override void OnDisable() => HalloweenInteractables.Terraformer.SetActive(false);
}

[hamburburmod("Spawn Lucy [M]", "Spawns the Blue Lucy", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SpawnLucy : hamburburmod
{
    protected override void Pressed()
    {
        HalloweenGhostChaser lucy = HalloweenInteractables.Lucy;

        if (!Tools.Utils.IsMasterClient || !lucy.IsMine)
        {
            NotificationManager.SendNotification("<color=red>Error</color>", "You are not master client", 3f, true, false);

            return;
        }

        lucy.timeGongStarted = Time.time;
        lucy.currentState    = HalloweenGhostChaser.ChaseState.Gong;
        lucy.isSummoned      = false;
    }
}

[hamburburmod("Lucy Chase Gun [M]", "Makes lucy chase whoever you want", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class LucyChaseGun : hamburburmod
{
    private readonly GunLib gunLib = new() { ShouldFollow = true, };

    private            bool wasShooting;
    protected override void Start() => gunLib.Start();

    protected override void LateUpdate()
    {
        gunLib.LateUpdate();

        bool isShooting = gunLib.IsShooting && gunLib.ChosenRig;

        if (isShooting && !wasShooting)
        {
            HalloweenGhostChaser lucy = HalloweenInteractables.Lucy;

            if (Tools.Utils.IsMasterClient || lucy.IsMine)
            {
                lucy.currentState = HalloweenGhostChaser.ChaseState.Chasing;
                lucy.targetPlayer = gunLib.ChosenRig.creator;
                lucy.followTarget = gunLib.ChosenRig.head.rigTarget;
            }
            else
            {
                NotificationManager.SendNotification("<color=red>Error</color>", "You are not master client", 3f, true, false);
            }
        }

        wasShooting = isShooting;
    }

    protected override void OnDisable() => gunLib.OnDisable();
}

[hamburburmod("Fast Broomsticks [M]", "Makes the broomsticks fast", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FastBroomsticks : hamburburmod
{
    protected override void OnEnable()
    {
        if (!Tools.Utils.IsMasterClient)
        {
            NotificationManager.SendNotification("<color=red>Error</color>", "You are not master client", 3f, true, false);
            Toggle(ButtonState.Normal, false);
            return;
        }
        
        foreach (NoncontrollableBroomstick broomstick in HalloweenInteractables.Broomsticks)
            broomstick.duration = 10f;
    }
    
    protected override void OnDisable()
    {
        if (!Tools.Utils.IsMasterClient)
            return;
        
        foreach (NoncontrollableBroomstick broomstick in HalloweenInteractables.Broomsticks)
            broomstick.duration = 30f;
    }
}

[hamburburmod("Slow Broomsticks [M]", "Makes the broomsticks slow", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlowBroomsticks : hamburburmod
{
    protected override void OnEnable()
    {
        if (!Tools.Utils.IsMasterClient)
        {
            NotificationManager.SendNotification("<color=red>Error</color>", "You are not master client", 3f, true, false);
            Toggle(ButtonState.Normal, false);
            return;
        }
        
        foreach (NoncontrollableBroomstick broomstick in HalloweenInteractables.Broomsticks)
            broomstick.duration = 60f;
    }
    
    protected override void OnDisable()
    {
        if (!Tools.Utils.IsMasterClient)
            return;
        
        foreach (NoncontrollableBroomstick broomstick in HalloweenInteractables.Broomsticks)
            broomstick.duration = 30f;
    }
}