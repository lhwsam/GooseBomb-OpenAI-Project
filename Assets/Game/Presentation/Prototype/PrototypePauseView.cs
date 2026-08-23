using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePauseView : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private GameObject menu;

        [SerializeField]
        private TextMeshProUGUI statusLabel;

        [SerializeField]
        private Button resumeButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private PrototypeSettingsPanelPresenter settingsPanel;

        public Canvas Canvas => canvas;

        public GameObject Menu => menu;

        public TextMeshProUGUI StatusLabel => statusLabel;

        public Button ResumeButton => resumeButton;

        public Button SettingsButton => settingsButton;

        public PrototypeSettingsPanelPresenter SettingsPanel => settingsPanel;

        public bool HasRequiredReferences =>
            canvas != null &&
            menu != null &&
            resumeButton != null &&
            settingsButton != null &&
            settingsPanel != null &&
            settingsPanel.HasAuthoredViewReferences;

        public void BindAuthoredView(
            Canvas authoredCanvas,
            GameObject authoredMenu,
            TextMeshProUGUI authoredStatusLabel,
            Button authoredResumeButton,
            Button authoredSettingsButton,
            PrototypeSettingsPanelPresenter authoredSettingsPanel)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Pause view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ?? throw new ArgumentNullException(nameof(authoredCanvas));
            menu = authoredMenu ?? throw new ArgumentNullException(nameof(authoredMenu));
            statusLabel = authoredStatusLabel;
            resumeButton = authoredResumeButton ??
                throw new ArgumentNullException(nameof(authoredResumeButton));
            settingsButton = authoredSettingsButton ??
                throw new ArgumentNullException(nameof(authoredSettingsButton));
            settingsPanel = authoredSettingsPanel ??
                throw new ArgumentNullException(nameof(authoredSettingsPanel));
        }
    }
}
