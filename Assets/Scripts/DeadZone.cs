using UnityEngine;

/// <summary>
/// «Бездна»/дырка в полу. Триггер-зона: когда игрок её касается, все активные
/// разломы разворачиваются обратно, а игрок возвращается на заданные координаты
/// (например, в начало уровня) с обнулённой скоростью.
///
/// Как использовать:
/// 1) Повесить на объект с Collider2D (Box/Polygon), у коллайдера включить Is Trigger
///    (скрипт включит сам через Reset).
/// 2) В поле Respawn Point вписать координаты, куда возвращать игрока.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class DeadZone : MonoBehaviour
{
    [Header("Куда возвращать игрока")]
    [Tooltip("Мировые координаты точки возрождения (например, старт уровня).")]
    [SerializeField] Vector2 respawnPoint;

    [Header("Ссылки (необязательно — найдутся сами)")]
    [Tooltip("Система маяков/разломов. Если пусто — берётся первая в сцене.")]
    [SerializeField] BeaconSystem beaconSystem;

    [Header("Кого считать игроком")]
    [Tooltip("Возвращать только объект с CatController. Выключи, если игрок — другой объект с Rigidbody2D.")]
    [SerializeField] bool onlyPlayerWithCatController = true;

    void Reset()
    {
        // Автоматически делаем коллайдер триггером при добавлении компонента.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        // Фильтр: это точно игрок?
        if (onlyPlayerWithCatController && rb.GetComponent<CatController>() == null)
            return;

        Respawn(rb);
    }

    void Respawn(Rigidbody2D rb)
    {
        // 1) Сначала разворачиваем все разломы — мир возвращается к исходной геометрии.
        if (beaconSystem == null) beaconSystem = FindFirstObjectByType<BeaconSystem>();
        if (beaconSystem != null) beaconSystem.ResetAll();

        // 2) Ставим игрока на точку возрождения и гасим инерцию.
        Vector3 target = new Vector3(respawnPoint.x, respawnPoint.y, rb.transform.position.z);
        rb.position = target;
        rb.transform.position = target;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    /// <summary>Удобство: записать в точку возрождения текущее положение этого объекта.</summary>
    [ContextMenu("Respawn Point = позиция этого объекта")]
    void SetRespawnToSelf()
    {
        respawnPoint = transform.position;
    }

    void OnDrawGizmos()
    {
        // Сам триггер — красной рамкой.
        var col = GetComponent<Collider2D>();
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        if (col != null) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

        // Точка возрождения — зелёный крест + линия от зоны к ней.
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        Vector3 rp = respawnPoint;
        Gizmos.DrawLine(rp + Vector3.left * 0.4f, rp + Vector3.right * 0.4f);
        Gizmos.DrawLine(rp + Vector3.down * 0.4f, rp + Vector3.up * 0.4f);
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.35f);
        Gizmos.DrawLine(transform.position, rp);
    }
}
