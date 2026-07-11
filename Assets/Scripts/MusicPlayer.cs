using UnityEngine;

/// <summary>
/// Фоновая музыка. Вешается на объект с AudioSource (loop = on, Play On Awake = on,
/// Output = группа Music микшера). По умолчанию переживает смену сцен, чтобы музыка
/// не обрывалась при переходе меню ↔ игра, и не дублировалась.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class MusicPlayer : MonoBehaviour
{
    [Tooltip("Не пересоздавать музыку при смене сцены (одна дорожка на всю игру).")]
    [SerializeField] bool persistAcrossScenes = true;

    static MusicPlayer instance;

    void Awake()
    {
        if (!persistAcrossScenes) return;

        if (instance != null && instance != this)
        {
            Destroy(gameObject); // в новой сцене уже играет музыка — этот дубликат не нужен
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
