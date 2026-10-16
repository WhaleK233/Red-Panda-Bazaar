using StardewModdingAPI;

namespace Red_Panda_Bazaar_Code.Compatibility.ModApi;

/// <summary>本 mod 使用的 Generic Mod Config Menu API 最小接口。</summary>
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    void AddNumberOption(
        IManifest mod,
        Func<float> getValue,
        Action<float> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null);
}
