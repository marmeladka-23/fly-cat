using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;

    [Header("Dead Zone (в мировых единицах от центра камеры)")]
    [Tooltip("Полуширина зоны по X. В этих пределах камера не двигается по горизонтали.")]
    [SerializeField] float deadZoneHalfWidth = 2.5f;
    [Tooltip("Полувысота зоны по Y. Шире, чем X — чтобы прыжки не дёргали камеру.")]
    [SerializeField] float deadZoneHalfHeight = 1.5f;

    [Header("Smoothing")]
    [Tooltip("Время сглаживания по X (сек). Больше — мягче и инертнее.")]
    [SerializeField] float smoothTimeX = 0.18f;
    [Tooltip("Время сглаживания по Y (сек). Обычно чуть больше X.")]
    [SerializeField] float smoothTimeY = 0.25f;
    [SerializeField] float maxSpeed = 30f;

    [Header("Look Ahead (опережение по движению)")]
    [Tooltip("Насколько камера смотрит вперёд по направлению движения.")]
    [SerializeField] float lookAheadDistance = 1.5f;
    [Tooltip("Как быстро меняется опережение при смене направления.")]
    [SerializeField] float lookAheadSmooth = 0.4f;
    [Tooltip("Минимальная скорость цели, при которой включается опережение.")]
    [SerializeField] float lookAheadSpeedThreshold = 0.5f;

    [Header("Offset")]
    [SerializeField] Vector2 worldOffset = Vector2.zero;

    [Header("Bounds (опционально — границы уровня)")]
    [SerializeField] bool useBounds = false;
    [SerializeField] Vector2 boundsMin = new Vector2(-50f, -50f);
    [SerializeField] Vector2 boundsMax = new Vector2(50f, 50f);

    Camera cam;
    Vector3 velocity;
    Vector3 lastTargetPos;
    float currentLookAhead;
    float lookAheadVel;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (target != null) lastTargetPos = target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 camPos = transform.position;
        Vector3 targetPos = target.position + (Vector3)worldOffset;

        // --- Опережение по горизонтальной скорости цели ---
        float targetVelX = (target.position.x - lastTargetPos.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastTargetPos = target.position;

        float desiredLookAhead = 0f;
        if (Mathf.Abs(targetVelX) > lookAheadSpeedThreshold)
            desiredLookAhead = Mathf.Sign(targetVelX) * lookAheadDistance;

        currentLookAhead = Mathf.SmoothDamp(currentLookAhead, desiredLookAhead, ref lookAheadVel, lookAheadSmooth);

        Vector3 anchor = new Vector3(targetPos.x + currentLookAhead, targetPos.y, camPos.z);

        // --- Мёртвая зона: двигаем только ту ось, по которой цель вышла за прямоугольник ---
        float dx = anchor.x - camPos.x;
        float dy = anchor.y - camPos.y;

        float desiredX = camPos.x;
        float desiredY = camPos.y;

        if (dx > deadZoneHalfWidth) desiredX = anchor.x - deadZoneHalfWidth;
        else if (dx < -deadZoneHalfWidth) desiredX = anchor.x + deadZoneHalfWidth;

        if (dy > deadZoneHalfHeight) desiredY = anchor.y - deadZoneHalfHeight;
        else if (dy < -deadZoneHalfHeight) desiredY = anchor.y + deadZoneHalfHeight;

        // --- Плавное сглаживание отдельно по осям ---
        Vector3 desired = new Vector3(desiredX, desiredY, camPos.z);
        Vector3 newPos = camPos;
        float vx = velocity.x, vy = velocity.y;
        newPos.x = Mathf.SmoothDamp(camPos.x, desired.x, ref vx, smoothTimeX, maxSpeed);
        newPos.y = Mathf.SmoothDamp(camPos.y, desired.y, ref vy, smoothTimeY, maxSpeed);
        velocity = new Vector3(vx, vy, 0f);

        // --- Опциональные границы уровня ---
        if (useBounds && cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            newPos.x = Mathf.Clamp(newPos.x, boundsMin.x + halfW, boundsMax.x - halfW);
            newPos.y = Mathf.Clamp(newPos.y, boundsMin.y + halfH, boundsMax.y - halfH);
        }

        transform.position = newPos;
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 p = target.position + (Vector3)worldOffset;
        p.z = transform.position.z;
        transform.position = p;
        velocity = Vector3.zero;
        currentLookAhead = 0f;
        lookAheadVel = 0f;
        lastTargetPos = target.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Vector3 c = transform.position;
        Vector3 size = new Vector3(deadZoneHalfWidth * 2f, deadZoneHalfHeight * 2f, 0f);
        Gizmos.DrawWireCube(new Vector3(c.x, c.y, 0f), size);

        if (useBounds)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Vector3 bc = (boundsMin + boundsMax) * 0.5f;
            Vector3 bs = boundsMax - boundsMin;
            Gizmos.DrawWireCube(new Vector3(bc.x, bc.y, 0f), new Vector3(bs.x, bs.y, 0f));
        }
    }
}
