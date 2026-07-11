using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class Portal : MonoBehaviour
{
    [Header("Pairing")]
    [Tooltip("Парный портал. Перетащи сюда другой Portal или используй меню Tools → Portals → Create Portal Pair.")]
    public Portal linkedPortal;

    [Header("Visual (показывается только в режиме видения)")]
    [SerializeField] SpriteRenderer visual;
    [Tooltip("Цвет портала в режиме видения.")]
    [SerializeField] Color visualColor = new Color(0.55f, 0.9f, 1f, 0.85f);

    [Header("Teleport")]
    [Tooltip("Сохранять скорость Rigidbody2D при телепорте.")]
    [SerializeField] bool preserveVelocity = true;

    public static bool VisionActive { get; set; }

    // Все живые порталы — для рассылки события «видение включилось».
    static readonly List<Portal> allPortals = new List<Portal>();

    // Rigidbody-цели, которые сейчас находятся внутри триггера. Пока тут — повторно не телепортируем.
    readonly HashSet<Rigidbody2D> insideRbs = new HashSet<Rigidbody2D>();

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>(true);
        if (visual != null) visual.color = visualColor;
        SetVisible(false);
    }

    void OnEnable() { allPortals.Add(this); }
    void OnDisable() { allPortals.Remove(this); }

    /// <summary>
    /// Вызывается контроллером, когда включается режим видения.
    /// Каждый портал, в котором уже кто-то стоит, через delay секунд телепортирует его.
    /// </summary>
    public static void OnVisionActivated(float delay)
    {
        foreach (var p in allPortals)
        {
            if (p == null || p.linkedPortal == null) continue;
            // Копируем, потому что во время делея insideRbs может меняться.
            var snapshot = new List<Rigidbody2D>(p.insideRbs);
            foreach (var rb in snapshot)
            {
                if (rb == null) continue;
                p.StartCoroutine(p.DelayedTeleport(rb, delay));
            }
        }
    }

    IEnumerator DelayedTeleport(Rigidbody2D rb, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (!VisionActive) yield break;
        if (rb == null) yield break;
        if (linkedPortal == null) yield break;
        // Тело могло выйти из портала за время ожидания — тогда отменяем.
        if (!insideRbs.Contains(rb)) yield break;

        Teleport(rb);
    }

    public void SetVisible(bool on)
    {
        if (visual != null) visual.enabled = on;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        // Если уже зарегистрирован как «внутри» — это либо вход после телепорта (нас сюда переместили),
        // либо побочный enter того же тела. В любом случае не телепортируем повторно.
        bool isNewEntry = insideRbs.Add(rb);
        if (!isNewEntry) return;

        if (!VisionActive) return;
        if (linkedPortal == null) return;

        Teleport(rb);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        insideRbs.Remove(rb);
    }

    void Teleport(Rigidbody2D rb)
    {
        // Заранее метим тело как «внутри» на той стороне — чтобы её OnTriggerEnter
        // не выстрелил телепортом обратно сразу после прибытия.
        linkedPortal.insideRbs.Add(rb);

        Vector3 dest = linkedPortal.transform.position;
        Vector3 oldPos = rb.transform.position;
        dest.z = oldPos.z;

        rb.position = dest;
        rb.transform.position = dest;

        if (!preserveVelocity) rb.linearVelocity = Vector2.zero;
    }

    [ContextMenu("Link with other selected Portal")]
    void ContextLink()
    {
#if UNITY_EDITOR
        foreach (var go in UnityEditor.Selection.gameObjects)
        {
            var p = go.GetComponent<Portal>();
            if (p != null && p != this)
            {
                linkedPortal = p;
                p.linkedPortal = this;
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(p);
                Debug.Log($"Linked {name} ↔ {p.name}", this);
                return;
            }
        }
        Debug.LogWarning("Выдели в иерархии оба портала и вызови команду снова.", this);
#endif
    }

    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        Bounds b = col != null ? col.bounds : new Bounds(transform.position, Vector3.one);

        Gizmos.color = linkedPortal != null
            ? new Color(0.4f, 0.9f, 1f, 0.9f)
            : new Color(1f, 0.45f, 0.45f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);

        if (linkedPortal != null)
        {
            Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.45f);
            Gizmos.DrawLine(transform.position, linkedPortal.transform.position);
        }
    }
}
