using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Весь игровой ввод через New Input System (мышь/тач + горячие клавиши).</summary>
[DisallowMultipleComponent]
public class GameInputController : MonoBehaviour
{
    const float SelectBufferSeconds = 0.2f;

    [SerializeField] InputActionAsset inputActions;

    InputActionMap _gameplay;
    InputAction _select;
    InputAction _pause;
    InputAction _hint;
    InputAction _refresh;
    InputAction _toggleMute;

    readonly List<RaycastResult> _raycastHits = new List<RaycastResult>(16);
    PointerEventData _pointerData;
    Vector2? _bufferedSelectPos;
    float _bufferedSelectUntil;

    void Awake()
    {
        if (inputActions == null)
            inputActions = Resources.Load<InputActionAsset>("MedivoriaGame");

        if (inputActions == null)
        {
            Debug.LogError("GameInputController: не найден MedivoriaGame.inputactions в Resources.");
            enabled = false;
            return;
        }

        _gameplay = inputActions.FindActionMap("Gameplay", true);
        _select = _gameplay.FindAction("Select", true);
        _pause = _gameplay.FindAction("Pause", true);
        _hint = _gameplay.FindAction("Hint", true);
        _refresh = _gameplay.FindAction("Refresh", true);
        _toggleMute = _gameplay.FindAction("ToggleMute", true);
    }

    void OnEnable()
    {
        if (_gameplay == null) return;

        _select.performed += OnSelectPerformed;
        _pause.performed += OnPausePerformed;
        _hint.performed += OnHintPerformed;
        _refresh.performed += OnRefreshPerformed;
        _toggleMute.performed += OnToggleMutePerformed;

        _gameplay.Enable();
    }

    void OnDisable()
    {
        if (_gameplay == null) return;

        _select.performed -= OnSelectPerformed;
        _pause.performed -= OnPausePerformed;
        _hint.performed -= OnHintPerformed;
        _refresh.performed -= OnRefreshPerformed;
        _toggleMute.performed -= OnToggleMutePerformed;

        _gameplay.Disable();
    }

    void Update()
    {
        if (!_bufferedSelectPos.HasValue) return;
        if (Time.unscaledTime > _bufferedSelectUntil)
        {
            _bufferedSelectPos = null;
            return;
        }

        if (GridManager.Instance == null || GridManager.Instance.IsBusy) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning()) return;

        TrySelectTileAt(_bufferedSelectPos.Value);
        _bufferedSelectPos = null;
    }

    void OnSelectPerformed(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning()) return;

        Vector2 screenPos = ReadScreenPosition(ctx);
        if (GridManager.Instance != null && GridManager.Instance.IsBusy)
        {
            _bufferedSelectPos = screenPos;
            _bufferedSelectUntil = Time.unscaledTime + SelectBufferSeconds;
            return;
        }

        TrySelectTileAt(screenPos);
    }

    void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null) GameManager.Instance.OnPauseButton();
    }

    void OnHintPerformed(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null) GameManager.Instance.OnHintButton();
    }

    void OnRefreshPerformed(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null) GameManager.Instance.OnRefreshButton();
    }

    void OnToggleMutePerformed(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null) GameManager.Instance.OnSoundButton();
    }

    static Vector2 ReadScreenPosition(InputAction.CallbackContext ctx)
    {
        if (ctx.control?.device is Touchscreen)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
        return Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
    }

    void TrySelectTileAt(Vector2 screenPos)
    {
        EventSystem es = EventSystem.current;
        if (es == null || GridManager.Instance == null) return;

        _pointerData ??= new PointerEventData(es);
        _pointerData.position = screenPos;
        _raycastHits.Clear();
        es.RaycastAll(_pointerData, _raycastHits);

        for (int i = 0; i < _raycastHits.Count; i++)
        {
            Tile tile = _raycastHits[i].gameObject.GetComponentInParent<Tile>();
            if (tile == null || tile.IsMatched) continue;
            GridManager.Instance.OnTileClicked(tile);
            return;
        }
    }
}
