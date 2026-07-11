using UnityEngine;

/// <summary>
/// Параллакс для 2D-фонов (луна, звёзды, дальние силуэты).
/// Чем выше followFactor, тем «дальше» слой и тем сильнее он повторяет движение камеры.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)] // после CameraFollow2D (у того порядок 0), чтобы читать уже обновлённую позицию камеры
public class ParallaxLayer2D : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Камера-наблюдатель. Если пусто — берётся Camera.main на старте.")]
    [SerializeField] Camera referenceCamera;

    [Header("Follow Factor (1 = намертво приклеен к камере, 0 = реальный мир)")]
    [Tooltip("Доля движения камеры по X, которую повторяет слой. Для далёкой луны ~0.9, для звёзд ~0.95.")]
    [Range(0f, 1f)] [SerializeField] float followFactorX = 0.9f;

    [Tooltip("Раздельный коэффициент по Y (обычно чуть выше, чем по X — чтобы прыжки не качали небо).")]
    [Range(0f, 1f)] [SerializeField] float followFactorY = 0.95f;

    [Header("Idle Drift (медленный собственный сдвиг)")]
    [Tooltip("Постоянная скорость дрейфа (мир/сек). Делает небо «живым», даже когда игрок стоит. Например (0.02, 0) — еле заметное движение вправо.")]
    [SerializeField] Vector2 driftSpeed = Vector2.zero;

    [Header("Smoothing")]
    [Tooltip("Если > 0 — целевая позиция сглаживается SmoothDamp. Обычно не нужно, камера уже сглажена. Полезно при followFactor < 0.5.")]
    [SerializeField] float smoothTime = 0f;

    Vector3 startLayerPos;
    Vector3 startCamPos;
    Vector3 smoothVel;
    bool initialized;

    void Start()
    {
        if (referenceCamera == null) referenceCamera = Camera.main;
        startLayerPos = transform.position;
        if (referenceCamera != null)
        {
            startCamPos = referenceCamera.transform.position;
            initialized = true;
        }
    }

    void LateUpdate()
    {
        if (!initialized)
        {
            if (referenceCamera == null) referenceCamera = Camera.main;
            if (referenceCamera == null) return;
            startCamPos = referenceCamera.transform.position;
            initialized = true;
        }

        Vector3 camDelta = referenceCamera.transform.position - startCamPos;
        Vector2 drift = driftSpeed * Time.time;

        // followFactor — какую часть движения камеры повторяет слой.
        // 1 → слой движется вместе с камерой (визуально стоит на месте, «бесконечная даль»).
        // 0 → слой остаётся в мире (камера обгоняет его на полную, объект «пролетает» мимо).
        float targetX = startLayerPos.x + camDelta.x * followFactorX + drift.x;
        float targetY = startLayerPos.y + camDelta.y * followFactorY + drift.y;
        Vector3 target = new Vector3(targetX, targetY, transform.position.z);

        if (smoothTime > 0f)
            transform.position = Vector3.SmoothDamp(transform.position, target, ref smoothVel, smoothTime);
        else
            transform.position = target;
    }

    /// <summary>Зафиксировать текущие позиции камеры и слоя как «исходные» (например, после телепорта).</summary>
    public void Rebase()
    {
        startLayerPos = transform.position;
        if (referenceCamera != null) startCamPos = referenceCamera.transform.position;
        smoothVel = Vector3.zero;
    }
}
