using hamburbur.Components;
using hamburbur.GUI;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.Settings;

[hamburburmod(                "Right Handed",       "Puts the menu in your right hand",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RightHanded : hamburburmod
{
    public static bool IsEnabled;

    protected override void OnEnable()
    {
        IsEnabled = true;

        MenuHandler.Instance.ButtonPresser.transform.SetParent(Tools.Utils.RealLeftController, false);
        MenuHandler.Instance.ButtonPresser.GetComponent<ButtonPresser>().isLeft                = true;
        KeyboardManager.Instance.NonDominantButtonPresser.GetComponent<ButtonPresser>().isLeft = false;
        ChangePointerPosition.ApplyCurrentPosition(MenuHandler.Instance.ButtonPresser, true);
        ChangePointerPosition.ApplyCurrentPosition(KeyboardManager.Instance.NonDominantButtonPresser, false);

        MenuHandler.Instance.Menu.transform.parent.SetParent(Tools.Utils.RealRightController, false);
        MenuHandler.Instance.Menu.transform.parent.transform.localRotation = Plugin.Instance.MenuLocalRotationRight;
        MenuHandler.Instance.Menu.transform.parent.localPosition           = Plugin.Instance.MenuLocalPositionRight;

        CoroutineManager.Instance.StartCoroutine(MenuHandler.Instance.CloseMenu());
    }

    protected override void OnDisable()
    {
        IsEnabled = false;

        MenuHandler.Instance.ButtonPresser.transform.SetParent(Tools.Utils.RealRightController, false);
        MenuHandler.Instance.ButtonPresser.GetComponent<ButtonPresser>().isLeft                = false;
        KeyboardManager.Instance.NonDominantButtonPresser.GetComponent<ButtonPresser>().isLeft = true;
        ChangePointerPosition.ApplyCurrentPosition(MenuHandler.Instance.ButtonPresser, false);
        ChangePointerPosition.ApplyCurrentPosition(KeyboardManager.Instance.NonDominantButtonPresser, true);

        MenuHandler.Instance.Menu.transform.parent.SetParent(Tools.Utils.RealLeftController, false);
        MenuHandler.Instance.Menu.transform.parent.transform.localRotation = Plugin.Instance.MenuLocalRotationLeft;
        MenuHandler.Instance.Menu.transform.parent.localPosition           = Plugin.Instance.MenuLocalPositionLeft;

        CoroutineManager.Instance.StartCoroutine(MenuHandler.Instance.CloseMenu());
    }
}
