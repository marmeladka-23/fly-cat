using UnityEngine;

/// <summary>
/// Простая покадровая анимация кота из отдельных спрайтов — без Animator.
/// Читает состояние CatController: в воздухе → прыжок, идёт по земле → ходьба,
/// стоит → idle. Поворот кота уже делает CatController (флип localScale.x).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class CatSpriteAnimator : MonoBehaviour
{
    [Header("Кадры")]
    [Tooltip("Кот стоит на месте.")]
    [SerializeField] Sprite idleSprite;
    [Tooltip("Кадры ходьбы — переключаются по кругу. Хватит и 2 штук.")]
    [SerializeField] Sprite[] walkSprites;
    [Tooltip("Кот в воздухе (прыжок/падение).")]
    [SerializeField] Sprite jumpSprite;

    [Header("Настройки")]
    [Tooltip("Кадров ходьбы в секунду.")]
    [SerializeField] float walkFps = 8f;
    [Tooltip("Считать, что кот идёт, если скорость по X больше этого.")]
    [SerializeField] float moveThreshold = 0.1f;

    [Header("Ссылки")]
    [Tooltip("Если пусто — берётся с этого же объекта или родителя.")]
    [SerializeField] CatController cat;

    SpriteRenderer sr;
    float walkTimer;
    int walkIndex;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cat == null) cat = GetComponentInParent<CatController>();
    }

    void Update()
    {
        if (cat == null) return;

        // В воздухе — кадр прыжка.
        if (!cat.IsOnGround)
        {
            if (jumpSprite != null) sr.sprite = jumpSprite;
            walkTimer = 0f;
            return;
        }

        // На земле и движется — крутим ходьбу.
        if (cat.HorizontalSpeed > moveThreshold && walkSprites != null && walkSprites.Length > 0)
        {
            walkTimer += Time.deltaTime * Mathf.Max(0.01f, walkFps);
            if (walkTimer >= 1f)
            {
                walkTimer -= 1f;
                walkIndex = (walkIndex + 1) % walkSprites.Length;
            }
            sr.sprite = walkSprites[walkIndex];
            return;
        }

        // Стоит.
        if (idleSprite != null) sr.sprite = idleSprite;
        walkTimer = 0f;
        walkIndex = 0;
    }
}
