using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD-индикаторы маяков. Цветная картинка = маяк можно поставить.
/// Ч/Б = либо маяк уже стоит, либо активен разлом (Q/E заблокированы).
/// Требует шейдер UI/Grayscale (см. Assets/Shaders/UIGrayscale.shader).
/// </summary>
[DisallowMultipleComponent]
public class BeaconHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] BeaconSystem beaconSystem;
    [Tooltip("UI Image для маяка A (картинка mayk1).")]
    [SerializeField] Image iconA;
    [Tooltip("UI Image для маяка B (картинка mayk2).")]
    [SerializeField] Image iconB;

    [Header("Shader")]
    [Tooltip("Шейдер UI/Grayscale. Если пусто — ищется по имени на старте.")]
    [SerializeField] Shader grayscaleShader;

    [Header("Look")]
    [Tooltip("Альфа иконки, когда маяк нельзя поставить (доп. визуальный сигнал).")]
    [Range(0f, 1f)] [SerializeField] float disabledAlpha = 0.6f;

    static readonly int SaturationId = Shader.PropertyToID("_Saturation");

    Material matA;
    Material matB;

    void Awake()
    {
        if (grayscaleShader == null) grayscaleShader = Shader.Find("UI/Grayscale");
        if (grayscaleShader == null)
        {
            Debug.LogError("[BeaconHUD] Не найден шейдер 'UI/Grayscale'. Положи UIGrayscale.shader в Assets/Shaders/ и/или добавь его в Always Included Shaders (Project Settings → Graphics).");
            return;
        }

        if (iconA != null)
        {
            matA = new Material(grayscaleShader);
            iconA.material = matA;
        }
        if (iconB != null)
        {
            matB = new Material(grayscaleShader);
            iconB.material = matB;
        }
    }

    void LateUpdate()
    {
        if (beaconSystem == null) return;
        ApplyState(iconA, matA, beaconSystem.CanPlaceBeaconA);
        ApplyState(iconB, matB, beaconSystem.CanPlaceBeaconB);
    }

    void ApplyState(Image icon, Material mat, bool canPlace)
    {
        if (icon == null) return;
        if (mat != null) mat.SetFloat(SaturationId, canPlace ? 1f : 0f);

        var c = icon.color;
        c.a = canPlace ? 1f : disabledAlpha;
        icon.color = c;
    }

    void OnDestroy()
    {
        if (matA != null) Destroy(matA);
        if (matB != null) Destroy(matB);
    }
}
