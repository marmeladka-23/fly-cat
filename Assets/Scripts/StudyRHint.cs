using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Подсказка study_R: плавно появляется, когда игрок подходит близко, и
/// исчезает НАВСЕГДА, когда игрок первый раз нажимает R. Вешается на объект study_R.
/// </summary>
[DisallowMultipleComponent]
public class StudyRHint : MonoBehaviour
{
    [Tooltip("На каком расстоянии до игрока подсказка появляется.")]
    [SerializeField] float showRadius = 3f;
    [Tooltip("Плавность появления/исчезновения, секунды. 0 = мгновенно.")]
    [SerializeField] float fadeDuration = 0.4f;
    [Tooltip("Игрок. Если пусто — берётся объект с CatController.")]
    [SerializeField] Transform player;

    SpriteRenderer[] renderers;
    float alpha;
    bool done; // R уже нажимали — подсказка отработана

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        alpha = 0f;          // стартуем невидимыми — покажемся при приближении
        ApplyAlpha();
    }

    void Start()
    {
        if (player == null)
        {
            var cat = FindFirstObjectByType<CatController>();
            if (cat != null) player = cat.transform;
        }
    }

    void Update()
    {
        // Первое нажатие R — прячем навсегда.
        if (!done)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) done = true;
        }

        // Цель по прозрачности: 0 если отработали или далеко, 1 если близко.
        float target = (!done && player != null && IsNear()) ? 1f : 0f;

        alpha = fadeDuration <= 0f ? target : Mathf.MoveTowards(alpha, target, Time.deltaTime / fadeDuration);
        ApplyAlpha();

        if (done && alpha <= 0f) gameObject.SetActive(false);
    }

    bool IsNear()
    {
        Vector2 a = transform.position;
        Vector2 b = player.position;
        return (a - b).sqrMagnitude <= showRadius * showRadius;
    }

    void ApplyAlpha()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var c = renderers[i].color;
            c.a = alpha;
            renderers[i].color = c;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, showRadius);
    }
}
