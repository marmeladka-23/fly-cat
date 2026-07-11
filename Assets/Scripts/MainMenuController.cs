using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Логика кнопок главного меню. Сам UI (Canvas, кнопки) собирается вручную
/// в редакторе, а эти методы вешаются на события OnClick кнопок.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Header("Сцены")]
    [Tooltip("Имя игровой сцены, которая грузится по кнопке «Играть». Должна быть в Build Settings.")]
    [SerializeField] string gameSceneName = "main";

    /// <summary>Кнопка «Играть» — грузит игровую сцену.</summary>
    public void OnPlay()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[MainMenu] Не задано имя игровой сцены.");
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Кнопка «Выход» — закрывает игру.</summary>
    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
