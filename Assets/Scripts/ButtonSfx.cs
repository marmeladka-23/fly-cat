using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Автоматически добавляет звук нажатия к кнопке — вешать на объект с Button,
/// тогда не нужно вручную прописывать звук в OnClick каждой кнопки.
/// </summary>
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class ButtonSfx : MonoBehaviour
{
    [Tooltip("Свой звук для этой кнопки. Пусто — используется стандартный клик из UISfxPlayer.")]
    [SerializeField] AudioClip overrideClip;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Play);
    }

    void Play()
    {
        if (UISfxPlayer.Instance == null) return;
        if (overrideClip != null) UISfxPlayer.Instance.Play(overrideClip);
        else UISfxPlayer.Instance.PlayClick();
    }
}
