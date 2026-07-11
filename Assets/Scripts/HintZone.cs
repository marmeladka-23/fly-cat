using UnityEngine;

/// <summary>
/// Зона-подсказка «по пути». Триггер: когда игрок входит, показывает текст
/// (сюжет или обучение) в общей плашке HintUI, либо включает свой объект-подсказку
/// в мире. Расставляй такие зоны по уровню при левел-дизайне.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class HintZone : MonoBehaviour
{
    [Header("Текст (сюжет/обучение)")]
    [TextArea(2, 5)]
    [SerializeField] string message;

    [Header("Поведение")]
    [Tooltip("Показать только один раз за прохождение (чтобы сюжет не повторялся).")]
    [SerializeField] bool showOnce = true;
    [Tooltip("Скрыть через N секунд после входа. 0 = скрывать, когда игрок выходит из зоны.")]
    [SerializeField] float autoHideSeconds = 0f;

    [Header("Необязательно: свой объект-подсказка в мире")]
    [Tooltip("Если задан — вместо общей плашки включается/выключается этот объект (напр. диегетичный спрайт 'нажми F').")]
    [SerializeField] GameObject worldHint;

    bool used;
    float hideAt = -1f;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (showOnce && used) return;

        used = true;
        ShowHint();
        hideAt = autoHideSeconds > 0f ? Time.time + autoHideSeconds : -1f;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (autoHideSeconds > 0f) return; // скрытие по таймеру — выход из зоны игнорируем
        if (!IsPlayer(other)) return;
        HideHint();
    }

    void Update()
    {
        if (hideAt > 0f && Time.time >= hideAt)
        {
            hideAt = -1f;
            HideHint();
        }
    }

    static bool IsPlayer(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        return rb != null && rb.GetComponent<CatController>() != null;
    }

    void ShowHint()
    {
        if (worldHint != null) worldHint.SetActive(true);
        else if (HintUI.Instance != null) HintUI.Instance.Show(message);
    }

    void HideHint()
    {
        if (worldHint != null) worldHint.SetActive(false);
        else if (HintUI.Instance != null) HintUI.Instance.Hide();
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<Collider2D>();
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.85f);
        if (c != null) Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);

        // Подпись позицией — чтобы в сцене было видно, где «говорилка».
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.35f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.down * 0.5f);
    }
}
