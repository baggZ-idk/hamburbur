using hamburbur.Misc;
using hamburbur.Mod_Backend;

namespace hamburbur.Mods.Settings;

[hamburburmod("Disable Stump Poster", "Disables the random ass poster inside of stump.", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlaughterPosterThingie : hamburburmod
{
    protected override void OnEnable()
    {
        if (HamburburPromotionManager.Instance.RandomAssPoster)
            HamburburPromotionManager.Instance.RandomAssPoster.gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        if (HamburburPromotionManager.Instance.RandomAssPoster)
            HamburburPromotionManager.Instance.RandomAssPoster.gameObject.SetActive(true);
    }
}