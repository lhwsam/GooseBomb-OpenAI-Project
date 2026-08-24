using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHealthHudView : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private GameObject bossPanel;

        [SerializeField]
        private RectTransform playerHeartContainer;

        [SerializeField]
        private PrototypeHealthHeartView playerHeartPrefab;

        [SerializeField]
        private Image bossHealthFill;

        [FormerlySerializedAs("bossHealthLabel")]
        [SerializeField]
        private TextMeshProUGUI bossNameLabel;

        [SerializeField]
        private TextMeshProUGUI bossPhaseLabel;

        [SerializeField]
        private TextMeshProUGUI bossHealthValueLabel;

        [SerializeField]
        private TextMeshProUGUI combatRewardLabel;

        public Canvas Canvas => canvas;

        public GameObject BossPanel => bossPanel;

        public RectTransform PlayerHeartContainer => playerHeartContainer;

        public PrototypeHealthHeartView PlayerHeartPrefab => playerHeartPrefab;

        public Image BossHealthFill => bossHealthFill;

        public TextMeshProUGUI BossNameLabel => bossNameLabel;

        public TextMeshProUGUI BossPhaseLabel => bossPhaseLabel;

        public TextMeshProUGUI BossHealthValueLabel => bossHealthValueLabel;

        public TextMeshProUGUI CombatRewardLabel => combatRewardLabel;

        public bool HasRequiredReferences =>
            canvas != null &&
            bossPanel != null &&
            playerHeartContainer != null &&
            playerHeartPrefab != null &&
            playerHeartPrefab.HasRequiredReferences &&
            bossHealthFill != null &&
            bossNameLabel != null &&
            bossPhaseLabel != null &&
            bossHealthValueLabel != null &&
            bossNameLabel != bossPhaseLabel &&
            bossNameLabel != bossHealthValueLabel &&
            bossPhaseLabel != bossHealthValueLabel &&
            combatRewardLabel != null;

        public void BindAuthoredView(
            Canvas authoredCanvas,
            GameObject authoredBossPanel,
            RectTransform authoredPlayerHeartContainer,
            PrototypeHealthHeartView authoredPlayerHeartPrefab,
            Image authoredBossHealthFill,
            TextMeshProUGUI authoredBossNameLabel,
            TextMeshProUGUI authoredBossPhaseLabel,
            TextMeshProUGUI authoredBossHealthValueLabel,
            TextMeshProUGUI authoredCombatRewardLabel)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Health HUD view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ?? throw new ArgumentNullException(nameof(authoredCanvas));
            bossPanel = authoredBossPanel ??
                throw new ArgumentNullException(nameof(authoredBossPanel));
            playerHeartContainer = authoredPlayerHeartContainer ??
                throw new ArgumentNullException(nameof(authoredPlayerHeartContainer));
            playerHeartPrefab = authoredPlayerHeartPrefab ??
                throw new ArgumentNullException(nameof(authoredPlayerHeartPrefab));
            bossHealthFill = authoredBossHealthFill ??
                throw new ArgumentNullException(nameof(authoredBossHealthFill));
            bossNameLabel = authoredBossNameLabel ??
                throw new ArgumentNullException(nameof(authoredBossNameLabel));
            bossPhaseLabel = authoredBossPhaseLabel ??
                throw new ArgumentNullException(nameof(authoredBossPhaseLabel));
            bossHealthValueLabel = authoredBossHealthValueLabel ??
                throw new ArgumentNullException(nameof(authoredBossHealthValueLabel));
            if (bossNameLabel == bossPhaseLabel ||
                bossNameLabel == bossHealthValueLabel ||
                bossPhaseLabel == bossHealthValueLabel)
            {
                throw new ArgumentException(
                    "Boss name, phase, and health value labels must be distinct.");
            }
            combatRewardLabel = authoredCombatRewardLabel ??
                throw new ArgumentNullException(nameof(authoredCombatRewardLabel));
        }
    }
}
