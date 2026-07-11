using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Единая плашка для показа текста подсказок/сюжета. Вешается на UI-объект
/// с CanvasGroup (для плавного появления) и текстовым полем. Зоны HintZone
/// обращаются к ней через HintUI.Instance и шлют строки.
/// </summary>
[DisallowMultipleComponent]
public class HintUI : MonoBehaviour
{
    public static HintUI Instance { get; private set; }

    [Tooltip("CanvasGroup на плашке — через его alpha делается плавное появление/скрытие.")]
    [SerializeField] CanvasGroup group;
    [Tooltip("Текстовое поле плашки (обычный Text; при желании замени на TMP).")]
    [SerializeField] Text label;
    [SerializeField] float fadeDuration = 0.3f;

    float targetAlpha;

    void Awake()
    {
        Instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 0f;
        targetAlpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (group == null) return;
        // unscaledDeltaTime — чтобы работало и на паузе (Time.timeScale = 0).
        float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, step);
    }

    /// <summary>Показать текст.</summary>
    public void Show(string message)
    {
        if (label != null) label.text = message;
        targetAlpha = 1f;
    }

    /// <summary>Плавно скрыть плашку.</summary>
    public void Hide()
    {
        targetAlpha = 0f;
    }
}
