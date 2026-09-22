using hamburbur.GUI;
using hamburbur.Mod_Backend;

namespace hamburbur.Mods.Categories;

[hamburburmod("Favourite Mods", "Go to the favourite mods category", ButtonType.Category, AccessSetting.Public,
        EnabledType.Disabled, 0)]
public class FavouriteMods : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance.SetCategory(Buttons.FavouriteModsCategory);
}
