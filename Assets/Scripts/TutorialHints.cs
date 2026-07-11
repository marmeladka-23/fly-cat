using UnityEngine;

/// <summary>
/// Поэтапные подсказки для обучения механике разлома.
/// Стадии переключаются один раз в одну сторону:
///   Super  — "поставьте маяки на Q/E"
///   Find   — "схлопните пространство на F" (после того как оба маяка поставлены)
///   Search — финальная подсказка (после первого успешного схлопывания)
/// Сами объекты подсказок — спрайты в мире, привязанные к фону сцены, а не к камере.
/// Появление и исчезновение — плавно через альфу SpriteRenderer.
/// </summary>
[DisallowMultipleComponent]
public class TutorialHints : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] BeaconSystem beaconSystem;

    [Header("Hints (world-space sprites)")]
    [Tooltip("Подсказка про маяки на Q/E.")]
    [SerializeField] GameObject superHint;
    [Tooltip("Подсказка про схлопывание на F.")]
    [SerializeField] GameObject findHint;
    [Tooltip("Финальная подсказка.")]
    [SerializeField] GameObject searchHint;

    [Header("Fade")]
    [Tooltip("Длительность плавного появления/исчезновения, секунды.")]
    [SerializeField] float fadeDuration = 0.6f;

    enum Stage { Super, Find, Search }
    Stage stage = Stage.Super;

    HintFader superFader;
    HintFader findFader;
    HintFader searchFader;

    void Awake()
    {
        superFader  = new HintFader(superHint);
        findFader   = new HintFader(findHint);
        searchFader = new HintFader(searchHint);
    }

    void Start()
    {
        // На старте показываем только активную стадию, без анимации.
        superFader.SetInstant(stage == Stage.Super);
        findFader.SetInstant(stage == Stage.Find);
        searchFader.SetInstant(stage == Stage.Search);
    }

    void Update()
    {
        if (beaconSystem != null)
        {
            if (stage == Stage.Super)
            {
                if (beaconSystem.BeaconAPlaced && beaconSystem.BeaconBPlaced)
                {
                    stage = Stage.Find;
                    ApplyStage();
                }
            }
            else if (stage == Stage.Find)
            {
                if (beaconSystem.RiftActive)
                {
                    stage = Stage.Search;
                    ApplyStage();
                }
            }
        }

        float dt = Time.deltaTime;
        superFader.Tick(dt, fadeDuration);
        findFader.Tick(dt, fadeDuration);
        searchFader.Tick(dt, fadeDuration);
    }

    void ApplyStage()
    {
        superFader.SetTarget(stage == Stage.Super);
        findFader.SetTarget(stage == Stage.Find);
        searchFader.SetTarget(stage == Stage.Search);
    }

    /// <summary>
    /// Плавно гонит альфу SpriteRenderer'ов внутри подсказки к целевому значению.
    /// GameObject деактивируется только после полного исчезновения, чтобы не платить за рендер.
    /// </summary>
    class HintFader
    {
        readonly GameObject root;
        readonly SpriteRenderer[] renderers;
        float alpha;
        float target;

        public HintFader(GameObject go)
        {
            root = go;
            renderers = go != null ? go.GetComponentsInChildren<SpriteRenderer>(true) : System.Array.Empty<SpriteRenderer>();
        }

        public void SetInstant(bool visible)
        {
            if (root == null) return;
            target = visible ? 1f : 0f;
            alpha = target;
            root.SetActive(visible);
            Apply();
        }

        public void SetTarget(bool visible)
        {
            if (root == null) return;
            target = visible ? 1f : 0f;
            if (visible && !root.activeSelf) root.SetActive(true);
        }

        public void Tick(float dt, float duration)
        {
            if (root == null || Mathf.Approximately(alpha, target)) return;

            float step = duration > 0f ? dt / duration : 1f;
            alpha = Mathf.MoveTowards(alpha, target, step);
            Apply();

            if (alpha <= 0f && target <= 0f && root.activeSelf) root.SetActive(false);
        }

        void Apply()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                var c = r.color;
                c.a = alpha;
                r.color = c;
            }
        }
    }
}
