using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RevealVisionController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] Key toggleKey = Key.R;

    [Header("Color Adjustments (обесцвечивание сцены)")]
    [Tooltip("-100 = полностью чёрно-белое, 0 = без эффекта.")]
    [Range(-100f, 0f)]
    [SerializeField] float saturation = -100f;
    [Tooltip("Контраст в режиме видения. 0 = без изменений.")]
    [Range(-100f, 100f)]
    [SerializeField] float contrast = 0f;
    [Tooltip("Подсветка / затемнение сцены в стопах экспозиции.")]
    [Range(-2f, 2f)]
    [SerializeField] float postExposure = 0f;
    [Tooltip("Лёгкая тонировка (например, холодный синий). Альфа = сила.")]
    [SerializeField] Color colorFilter = Color.white;

    [Header("Volume")]
    [Tooltip("Приоритет Volume — выше существующих, чтобы перекрыть.")]
    [SerializeField] float volumePriority = 100f;

    [Header("Smoothing")]
    [Tooltip("Время появления/исчезания эффекта.")]
    [SerializeField] float fadeDuration = 0.2f;

    [Header("Portal Activation")]
    [Tooltip("Если кот уже стоял в портале на момент включения видения — телепортнёт его через эту задержку.")]
    [SerializeField] float standingTeleportDelay = 0.2f;

    bool active;
    Volume volume;
    VolumeProfile profile;
    ColorAdjustments colorAdj;

    void Awake()
    {
        BuildVolume();
        Portal.VisionActive = false;
        RefreshPortals(false);
        if (volume != null) volume.weight = 0f;
    }

    void OnDestroy()
    {
        Portal.VisionActive = false;
        if (profile != null) Destroy(profile);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame) Toggle();

        if (volume != null)
        {
            float target = active ? 1f : 0f;
            float step = Time.unscaledDeltaTime / Mathf.Max(fadeDuration, 0.0001f);
            volume.weight = Mathf.MoveTowards(volume.weight, target, step);
        }
    }

    public void Toggle() => SetActive(!active);

    public void SetActive(bool on)
    {
        active = on;
        Portal.VisionActive = on;
        RefreshPortals(on);
        if (on) Portal.OnVisionActivated(standingTeleportDelay);
    }

    static void RefreshPortals(bool visible)
    {
        var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        foreach (var p in portals) p.SetVisible(visible);
    }

    void BuildVolume()
    {
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "RevealVisionProfile";

        colorAdj = profile.Add<ColorAdjustments>(true);
        ApplyValues();

        var volumeGO = new GameObject("RevealVisionVolume");
        volumeGO.transform.SetParent(transform, false);
        volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = volumePriority;
        volume.profile = profile;
        volume.weight = 0f;
    }

    void ApplyValues()
    {
        if (colorAdj == null) return;
        colorAdj.saturation.overrideState = true;
        colorAdj.saturation.value = saturation;
        colorAdj.contrast.overrideState = true;
        colorAdj.contrast.value = contrast;
        colorAdj.postExposure.overrideState = true;
        colorAdj.postExposure.value = postExposure;
        colorAdj.colorFilter.overrideState = true;
        colorAdj.colorFilter.value = colorFilter;
    }

    void OnValidate()
    {
        if (volume != null) volume.priority = volumePriority;
        ApplyValues();
    }
}
