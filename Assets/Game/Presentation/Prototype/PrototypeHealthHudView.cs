using System;
using TMPro;
using UnityEngine;
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
        private Image playerHealthFill;

        [SerializeField]
        private Image bossHealthFill;

        [SerializeField]
        private TextMeshProUGUI playerHealthLabel;

        [SerializeField]
        private TextMeshProUGUI bossHealthLabel;

        [SerializeField]
        private TextMeshProUGUI combatRewardLabel;

        public Canvas Canvas => canvas;

        public GameObject BossPanel => bossPanel;

        public Image PlayerHealthFill => playerHealthFill;

        public Image BossHealthFill => bossHealthFill;

        public TextMeshProUGUI PlayerHealthLabel => playerHealthLabel;

        public TextMeshProUGUI BossHealthLabel => bossHealthLabel;

        public TextMeshProUGUI CombatRewardLabel => combatRewardLabel;

        public bool HasRequiredReferences =>
            canvas != null &&
            bossPanel != null &&
            playerHealthFill != null &&
            bossHealthFill != null &&
            playerHealthLabel != null &&
            bossHealthLabel != null &&
            combatRewardLabel != null;

        public void BindAuthoredView(
            Canvas authoredCanvas,
            GameObject authoredBossPanel,
            Image authoredPlayerHealthFill,
            Image authoredBossHealthFill,
            TextMeshProUGUI authoredPlayerHealthLabel,
            TextMeshProUGUI authoredBossHealthLabel,
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
            playerHealthFill = authoredPlayerHealthFill ??
                throw new ArgumentNullException(nameof(authoredPlayerHealthFill));
            bossHealthFill = authoredBossHealthFill ??
                throw new ArgumentNullException(nameof(authoredBossHealthFill));
            playerHealthLabel = authoredPlayerHealthLabel ??
                throw new ArgumentNullException(nameof(authoredPlayerHealthLabel));
            bossHealthLabel = authoredBossHealthLabel ??
                throw new ArgumentNullException(nameof(authoredBossHealthLabel));
            combatRewardLabel = authoredCombatRewardLabel ??
                throw new ArgumentNullException(nameof(authoredCombatRewardLabel));
        }
    }
}
