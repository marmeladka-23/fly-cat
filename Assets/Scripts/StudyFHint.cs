using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Подсказка study_F: исчезает, когда игрок ПЕРВЫЙ раз нажимает F.
/// Вешается прямо на объект study_F.
/// </summary>
[DisallowMultipleComponent]
public class StudyFHint : MonoBehaviour
{
    [Tooltip("Плавно растворить за N секунд. 0 = исчезнуть мгновенно.")]
    [SerializeField] float fadeDuration = 0.4f;

    bool fading;
    float alpha = 1f;
    SpriteRenderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    void Update()
    {
        if (!fading)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame)
            {
                if (fadeDuration <= 0f) { gameObject.SetActive(false); return; }
                fading = true;
            }
            return;
        }

        // Плавное исчезновение.
        alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime / fadeDuration);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var c = renderers[i].color;
            c.a = alpha;
            renderers[i].color = c;
        }
        if (alpha <= 0f) gameObject.SetActive(false);
    }
}
