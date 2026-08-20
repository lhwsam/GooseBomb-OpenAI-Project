using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePausePresenter : MonoBehaviour
    {
        private PrototypeGameSession _session;
        private GameObject _canvasObject;
        private TextMeshProUGUI _statusLabel;
        private bool _isSubscribed;

        public PrototypeGameSession Session => _session;

        public bool IsVisible { get; private set; }

        public int ShowCount { get; private set; }

        public int HideCount { get; private set; }

        public string StatusText =>
            _statusLabel != null ? _statusLabel.text : string.Empty;

        public void Configure(PrototypeGameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (_session == session)
            {
                if (isActiveAndEnabled)
                {
                    Subscribe();
                    SetVisible(session.IsPaused);
                }
                return;
            }

            Unsubscribe();
            _session = session;
            if (isActiveAndEnabled)
            {
                Subscribe();
                SetVisible(session.IsPaused);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _session == null)
            {
                return;
            }

            Subscribe();
            SetVisible(_session.IsPaused);
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetVisible(false);
        }

        private void Subscribe()
        {
            if (_isSubscribed || _session == null)
            {
                return;
            }

            _session.PauseStateChanged += OnPauseStateChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _session == null)
            {
                return;
            }

            _session.PauseStateChanged -= OnPauseStateChanged;
            _isSubscribed = false;
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            SetVisible(isPaused);
        }

        private void SetVisible(bool visible)
        {
            if (visible)
            {
                EnsureUi();
            }

            if (_canvasObject != null)
            {
                _canvasObject.SetActive(visible);
            }
            if (IsVisible == visible)
            {
                return;
            }

            IsVisible = visible;
            if (visible)
            {
                ShowCount++;
            }
            else
            {
                HideCount++;
            }
        }

        private void EnsureUi()
        {
            if (_canvasObject != null)
            {
                return;
            }

            _canvasObject = new GameObject(
                "PrototypePauseCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = CreateRect("Backdrop", _canvasObject.transform);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.015f, 0.02f, 0.04f, 0.72f);
            backdropImage.raycastTarget = false;

            TextMeshProUGUI title = PrototypeUiFactory.CreateText(
                "Title",
                backdrop,
                56f,
                TextAlignmentOptions.Center,
                FontStyles.Bold,
                TextWrappingModes.Normal);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.48f);
            title.rectTransform.anchorMax = new Vector2(0.9f, 0.68f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.text = "PAUSED";
            title.color = new Color(0.35f, 0.82f, 1f, 1f);

            _statusLabel = PrototypeUiFactory.CreateText(
                "Resume",
                backdrop,
                26f,
                TextAlignmentOptions.Center,
                FontStyles.Normal,
                TextWrappingModes.Normal);
            _statusLabel.rectTransform.anchorMin = new Vector2(0.1f, 0.34f);
            _statusLabel.rectTransform.anchorMax = new Vector2(0.9f, 0.48f);
            _statusLabel.rectTransform.offsetMin = Vector2.zero;
            _statusLabel.rectTransform.offsetMax = Vector2.zero;
            _statusLabel.text = "ESC / GAMEPAD START - RESUME";
            _statusLabel.color = Color.white;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

    }
}
