using UnityEngine;

public static class Atmosphere
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.shadows = ShadowQuality.Disable;

        if (Object.FindObjectOfType<DayNightCycle>() == null)
            new GameObject("DayNightCycle").AddComponent<DayNightCycle>();

        if (Object.FindObjectOfType<DebugOverlay>() == null)
            new GameObject("DebugOverlay").AddComponent<DebugOverlay>();
    }
}
