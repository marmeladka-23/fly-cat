using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Оверлей настроек, общий для меню и игры. Открывается поверх экрана,
/// ставит игру на паузу (Time.timeScale = 0), закрывается по «Назад» или Esc.
/// Внутри: громкость музыки и звуков + выход в главное меню (только в игре).
///
/// Скрипт вешается на ВСЕГДА АКТИВНЫЙ объект (например, на сам Canvas или
/// отдельный "SettingsManager"), а поле Panel указывает на корневую панель,
/// которая включается/выключается. Панель по умолчанию должна быть выключена.
/// </summary>
[DisallowMultipleComponent]
public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Корневой объект панели настроек. Изначально выключен (галка слева от имени снята).")]
    [SerializeField] GameObject panel;
    [Tooltip("Ползунок громкости музыки (Slider, диапазон 0..1).")]
    [SerializeField] Slider musicSlider;
    [Tooltip("Ползунок громкости звуков (Slider, диапазон 0..1).")]
    [SerializeField] Slider sfxSlider;
    [Tooltip("Кнопка «В главное меню». Автоматически скрывается, когда мы уже в меню.")]
    [SerializeField] GameObject mainMenuButton;

    [Header("Сцены")]
    [Tooltip("Имя сцены главного меню. Нужно и для кнопки «В главное меню», и чтобы понять, где мы сейчас.")]
    [SerializeField] string mainMenuSceneName = "MainMenu";

    [Header("Звук (можно оставить пустым — тогда громкость хранится в PlayerPrefs)")]
    [Tooltip("AudioMixer с exposed-параметрами громкости. Появится, когда добавишь звук/музыку.")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] string musicParam = "MusicVolume";
    [SerializeField] string sfxParam = "SFXVolume";

    const string MusicPref = "settings.musicVolume";
    const string SfxPref = "settings.sfxVolume";

    bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        // Загружаем сохранённые значения и применяем к миксеру (если он есть) —
        // чтобы громкость работала даже до открытия настроек.
        float music = PlayerPrefs.GetFloat(MusicPref, 1f);
        float sfx = PlayerPrefs.GetFloat(SfxPref, 1f);
        ApplyMusic(music);
        ApplySfx(sfx);

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        // Esc — открыть/закрыть настройки (работает и на паузе, т.к. Update не зависит от timeScale).
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    /// <summary>Открыть настройки и поставить игру на паузу.</summary>
    public void Open()
    {
        if (panel == null) return;

        // Кнопку выхода в меню показываем только когда мы НЕ в сцене меню.
        if (mainMenuButton != null)
        {
            bool inMenu = SceneManager.GetActiveScene().name == mainMenuSceneName;
            mainMenuButton.SetActive(!inMenu);
        }

        panel.SetActive(true);
        Time.timeScale = 0f; // пауза
    }

    /// <summary>Кнопка «Назад» — закрыть настройки и снять паузу.</summary>
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f; // снять паузу
        PlayerPrefs.Save();
    }

    /// <summary>Кнопка «В главное меню».</summary>
    public void ExitToMainMenu()
    {
        // Запоминаем позицию игрока, чтобы при следующем «Играть» он оказался на месте.
        var mem = FindFirstObjectByType<PlayerPositionMemory>();
        if (mem != null) mem.Save();

        Time.timeScale = 1f; // обязательно снять паузу перед загрузкой сцены
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // --- Громкость -----------------------------------------------------------

    void OnMusicChanged(float value)
    {
        ApplyMusic(value);
        PlayerPrefs.SetFloat(MusicPref, value);
    }

    void OnSfxChanged(float value)
    {
        ApplySfx(value);
        PlayerPrefs.SetFloat(SfxPref, value);
    }

    void ApplyMusic(float value) => ApplyToMixer(musicParam, value);
    void ApplySfx(float value) => ApplyToMixer(sfxParam, value);

    // Ползунок линейный 0..1, а миксер работает в децибелах — переводим.
    void ApplyToMixer(string param, float value)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;
        float dB = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mixer.SetFloat(param, dB);
    }

    void OnDestroy()
    {
        // На всякий случай не оставляем игру на паузе при выгрузке сцены.
        Time.timeScale = 1f;
    }
}
