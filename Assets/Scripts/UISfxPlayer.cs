using UnityEngine;

/// <summary>
/// Проигрыватель UI-звуков (клик по кнопке и т.п.). Вешается на объект с AudioSource
/// (Play On Awake = off, Output = группа SFX микшера). Живёт между сценами, доступен
/// глобально через UISfxPlayer.Instance.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class UISfxPlayer : MonoBehaviour
{
    public static UISfxPlayer Instance { get; private set; }

    [Tooltip("Звук нажатия по кнопке по умолчанию.")]
    [SerializeField] AudioClip clickClip;

    AudioSource src;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        src = GetComponent<AudioSource>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Проиграть стандартный звук клика.</summary>
    public void PlayClick() => Play(clickClip);

    /// <summary>Проиграть произвольный звук через группу SFX.</summary>
    public void Play(AudioClip clip)
    {
        if (clip != null && src != null) src.PlayOneShot(clip);
    }
}
