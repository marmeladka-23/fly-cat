using UnityEngine;

/// <summary>
/// Триггер завершения игры. Вешается на объект (птичку) с Collider2D (Is Trigger).
/// Когда игрок касается хитбокса — показывает экран «Вы прошли игру» и (по желанию)
/// ставит игру на паузу.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class LevelComplete : MonoBehaviour
{
    [Header("Экран победы")]
    [Tooltip("UI-объект с надписью «Вы прошли игру». Изначально выключен — включится при касании.")]
    [SerializeField] GameObject winScreen;

    [Header("Поведение")]
    [Tooltip("Ставить игру на паузу при победе (Time.timeScale = 0).")]
    [SerializeField] bool pauseGame = true;

    bool done;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;

        var rb = other.attachedRigidbody;
        if (rb == null || rb.GetComponent<CatController>() == null) return; // касается именно игрок

        done = true;
        if (winScreen != null) winScreen.SetActive(true);
        if (pauseGame) Time.timeScale = 0f;
    }
}
