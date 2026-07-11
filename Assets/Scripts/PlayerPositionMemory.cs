using UnityEngine;

/// <summary>
/// Простое сохранение позиции игрока в рамках одного запуска игры.
/// Вешается на игрока (кота). Позиция помнится, пока приложение запущено;
/// после перезапуска игры сбрасывается. Инерция (скорость) НЕ сохраняется —
/// при восстановлении гасится.
/// </summary>
[DisallowMultipleComponent]
public class PlayerPositionMemory : MonoBehaviour
{
    // Статик — переживает смену сцены. null = сохранения ещё нет (новый запуск).
    static Vector3? savedPosition;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (!savedPosition.HasValue) return; // первый вход — оставляем позицию из сцены

        Vector3 pos = savedPosition.Value;
        pos.z = transform.position.z; // не трогаем глубину

        transform.position = pos;
        if (rb != null)
        {
            rb.position = pos;
            rb.linearVelocity = Vector2.zero; // гасим инерцию
            rb.angularVelocity = 0f;
        }
    }

    /// <summary>Запомнить текущую позицию игрока. Зовётся при выходе в главное меню.</summary>
    public void Save()
    {
        savedPosition = transform.position;
    }

    /// <summary>Сбросить сохранение — например, для кнопки «Новая игра».</summary>
    public static void Clear()
    {
        savedPosition = null;
    }
}
