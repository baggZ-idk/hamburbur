using System.Text;
using hamburbur.GUI;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

internal static class SlapBattlesSettings
{
    internal static string Text = "SLAP";
}

[hamburburmod(            "Set Wall Text",      "Type words to spell with networked walls",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSetText : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Enter wall text (up to 12 characters)",
            SetText, null));

    private static void SetText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        StringBuilder result = new();
        int           pixels = 0;
        foreach (char character in input.ToUpperInvariant())
        {
            if (result.Length >= 12) break;
            if (character == ' ')
            {
                result.Append(character);

                continue;
            }

            if (!Tools.Utils.BITFont.TryGetValue(character, out Vector2Int[] glyph) ||
                pixels + glyph.Length > 90) continue;

            pixels += glyph.Length;
            result.Append(character);
        }

        string value                                   = result.ToString().Trim();
        if (value.Length > 0) SlapBattlesSettings.Text = value;
    }
}

[hamburburmod(                "Spell Wall Text Gun", "Shoot a surface to spell text with walls visible to everyone",
        ButtonType.Togglable, AccessSetting.Public,  EnabledType.Disabled, 0)]
public class SlapBattlesSpellGun : SlapBattlesGun
{
    protected override float RepeatDelay => 0f;

    protected override void Shoot()
    {
        if (Gun.Hit.collider == null || GorillaTagger.Instance?.bodyCollider == null) return;
        Vector3 point        = Gun.Hit.point;
        Vector3 towardPlayer = GorillaTagger.Instance.bodyCollider.transform.position - point;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude < 0.01f) towardPlayer = Vector3.forward;
        Quaternion facing = Quaternion.LookRotation(-towardPlayer.normalized, Vector3.up);
        SlapBattlesNetwork.Spell(SlapBattlesSettings.Text, point, facing);
    }
}
