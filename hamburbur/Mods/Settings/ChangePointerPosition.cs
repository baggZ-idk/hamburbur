using hamburbur.Components;
using hamburbur.GUI;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.Settings;

[hamburburmod(
        "Pointer Position: ",
        "Changes where the menu and keyboard button pressers sit on each hand",
        ButtonType.Incremental,
        AccessSetting.Public,
        EnabledType.Disabled,
        0)]
public class ChangePointerPosition : hamburburmod
{
    private static readonly string[] PositionNames =
    [
            "Default",
            "Bottom",
            "Index Finger",
    ];

    private static readonly Vector3[] Positions =
    [
            new(0f,     -0.01f, 0.13f),
            new(-0.01f, -0.09f, 0.02f),
            new(0f,     0.015f, 0.125f),
    ];

    public static ChangePointerPosition Instance { get; private set; }

    private static int CurrentIndex => NormalizeIndex(Instance?.IncrementalValue ?? 0);

    public override string ModName => AssociatedAttribute.Name + PositionNames[CurrentIndex];

    protected override void Start()
    {
        Instance         = this;
        IncrementalValue = CurrentIndex;
        ApplyToAllPressers();
    }

    protected override void Increment()
    {
        IncrementalValue = NormalizeIndex(IncrementalValue + 1);
        ApplyToAllPressers();
    }

    protected override void Decrement()
    {
        IncrementalValue = NormalizeIndex(IncrementalValue - 1);
        ApplyToAllPressers();
    }

    protected override void OnIncrementalStateLoaded()
    {
        IncrementalValue = NormalizeIndex(IncrementalValue);
        ApplyToAllPressers();
    }

    public static void ApplyCurrentPosition(GameObject presser, bool isLeftHand)
    {
        if (presser == null)
            return;

        Vector3 position = Positions[CurrentIndex];
        position.x *= isLeftHand ? -1f : 1f;
        presser.transform.localPosition = position;
    }

    private static void ApplyToAllPressers()
    {
        ApplyFromComponent(MenuHandler.Instance?.ButtonPresser);
        ApplyFromComponent(KeyboardManager.Instance?.NonDominantButtonPresser);
    }

    private static void ApplyFromComponent(GameObject presser)
    {
        if (presser == null || !presser.TryGetComponent(out ButtonPresser buttonPresser))
            return;

        ApplyCurrentPosition(presser, buttonPresser.isLeft);
    }

    private static int NormalizeIndex(int index) => (index % Positions.Length + Positions.Length) % Positions.Length;
}
