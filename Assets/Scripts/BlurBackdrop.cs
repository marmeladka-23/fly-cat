using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Размытая «подложка» для UI-панели (эффект матового стекла).
/// Вешается на RawImage, растянутый на весь экран. Когда объект включается
/// (панель открывается), делает снимок текущего экрана, размывает его
/// уменьшением-увеличением и показывает под панелью.
///
/// Рассчитан на паузу (Time.timeScale = 0): картинка статична, пока панель открыта.
/// Для живого размытия движущейся игры нужен ScriptableRendererFeature — это сложнее.
/// </summary>
[RequireComponent(typeof(RawImage))]
[DisallowMultipleComponent]
public class BlurBackdrop : MonoBehaviour
{
    [Header("Сила размытия")]
    [Tooltip("Сколько раз уменьшать снимок. Больше — сильнее и мягче блюр (и дешевле).")]
    [Range(1, 6)] [SerializeField] int iterations = 4;

    RawImage rawImage;
    RenderTexture result;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rawImage.color = Color.white; // не тонируем сам блюр; тон делай отдельным Image сверху
    }

    void OnEnable() { Capture(); }      // панель включилась — свежий снимок
    void OnDisable() { ReleaseResult(); }

    void Capture()
    {
        // Снимок экрана. Панель в этом кадре ещё не нарисована, так что берётся игра/HUD.
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

        var chain = new List<RenderTexture>();
        RenderTexture current = RenderTexture.GetTemporary(shot.width, shot.height, 0);
        Graphics.Blit(shot, current);
        Destroy(shot);
        chain.Add(current);

        // Вниз по пирамиде: каждый шаг усредняет соседние пиксели.
        for (int i = 0; i < iterations; i++)
        {
            int w = Mathf.Max(2, current.width / 2);
            int h = Mathf.Max(2, current.height / 2);
            var down = RenderTexture.GetTemporary(w, h, 0);
            Graphics.Blit(current, down);
            chain.Add(down);
            current = down;
        }

        // Вверх по пирамиде: билинейное сглаживание даёт мягкое размытие.
        for (int i = chain.Count - 2; i >= 0; i--)
        {
            Graphics.Blit(current, chain[i]);
            current = chain[i];
        }

        // Копируем итог в постоянную текстуру для показа.
        ReleaseResult();
        result = new RenderTexture(current.width, current.height, 0) { filterMode = FilterMode.Bilinear };
        Graphics.Blit(current, result);
        rawImage.texture = result;

        foreach (var rt in chain) RenderTexture.ReleaseTemporary(rt);
    }

    void ReleaseResult()
    {
        if (result != null)
        {
            result.Release();
            Destroy(result);
            result = null;
        }
    }
}
