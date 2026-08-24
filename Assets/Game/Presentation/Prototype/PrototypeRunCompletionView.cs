using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeRunCompletionView : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private TextMeshProUGUI titleLabel;

        [SerializeField]
        private TextMeshProUGUI failureCauseLabel;

        [SerializeField]
        private TextMeshProUGUI statusLabel;

        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private Button lobbyButton;

        [Header("Runtime state colors")]
        [SerializeField]
        private Color completedTitleColor =
            new Color(0.22f, 0.95f, 0.5f, 1f);

        [SerializeField]
        private Color failedTitleColor =
            new Color(1f, 0.32f, 0.26f, 1f);

        public Canvas Canvas => canvas;

        public TextMeshProUGUI TitleLabel => titleLabel;

        public TextMeshProUGUI FailureCauseLabel => failureCauseLabel;

        public TextMeshProUGUI StatusLabel => statusLabel;

        public Button RestartButton => restartButton;

        public Button LobbyButton => lobbyButton;

        public Color CompletedTitleColor => completedTitleColor;

        public Color FailedTitleColor => failedTitleColor;

        public bool HasRequiredReferences =>
            canvas != null &&
            titleLabel != null &&
            failureCauseLabel != null &&
            statusLabel != null &&
            restartButton != null &&
            lobbyButton != null &&
            restartButton != lobbyButton;

        public void BindAuthoredView(
            Canvas authoredCanvas,
            TextMeshProUGUI authoredTitleLabel,
            TextMeshProUGUI authoredFailureCauseLabel,
            TextMeshProUGUI authoredStatusLabel,
            Button authoredRestartButton,
            Button authoredLobbyButton)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Run completion view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ??
                throw new ArgumentNullException(nameof(authoredCanvas));
            titleLabel = authoredTitleLabel ??
                throw new ArgumentNullException(nameof(authoredTitleLabel));
            failureCauseLabel = authoredFailureCauseLabel ??
                throw new ArgumentNullException(nameof(authoredFailureCauseLabel));
            statusLabel = authoredStatusLabel ??
                throw new ArgumentNullException(nameof(authoredStatusLabel));
            restartButton = authoredRestartButton ??
                throw new ArgumentNullException(nameof(authoredRestartButton));
            lobbyButton = authoredLobbyButton ??
                throw new ArgumentNullException(nameof(authoredLobbyButton));
        }
    }
}
