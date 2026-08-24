using System;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonMinimapRoomView : MonoBehaviour
    {
        [SerializeField]
        private Image roomImage;

        [SerializeField]
        private Image roomIconImage;

        [Header("Room backgrounds")]
        [SerializeField]
        private Sprite currentRoomBackground;

        [SerializeField]
        private Sprite otherRoomBackground;

        [Header("Room icons")]
        [SerializeField]
        private Sprite unknownRoomIcon;

        [SerializeField]
        private Sprite startRoomIcon;

        [SerializeField]
        private Sprite combatRoomIcon;

        [SerializeField]
        private Sprite bombRewardRoomIcon;

        [SerializeField]
        private Sprite recoveryRoomIcon;

        [SerializeField]
        private Sprite secretRoomIcon;

        [SerializeField]
        private Sprite bossRoomIcon;

        public Image RoomImage => roomImage;

        public Image RoomIconImage => roomIconImage;

        public Sprite CurrentRoomBackground => currentRoomBackground;

        public Sprite OtherRoomBackground => otherRoomBackground;

        public bool HasRequiredReferences =>
            roomImage != null &&
            roomIconImage != null &&
            currentRoomBackground != null &&
            otherRoomBackground != null &&
            unknownRoomIcon != null &&
            startRoomIcon != null &&
            combatRoomIcon != null &&
            bombRewardRoomIcon != null &&
            recoveryRoomIcon != null &&
            secretRoomIcon != null &&
            bossRoomIcon != null;

        public void Render(bool isCurrent, RoomType? knownRoomType)
        {
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Minimap room view is missing authored image or sprite references.");
            }

            roomImage.sprite = isCurrent
                ? currentRoomBackground
                : otherRoomBackground;

            Sprite icon = GetIcon(knownRoomType);
            roomIconImage.sprite = icon;
            roomIconImage.gameObject.SetActive(icon != null);
        }

        public Sprite GetIcon(RoomType? knownRoomType)
        {
            if (!knownRoomType.HasValue)
            {
                return unknownRoomIcon;
            }

            switch (knownRoomType.Value)
            {
                case RoomType.Start:
                    return startRoomIcon;
                case RoomType.Combat:
                    return combatRoomIcon;
                case RoomType.BombReward:
                    return bombRewardRoomIcon;
                case RoomType.Recovery:
                    return recoveryRoomIcon;
                case RoomType.Secret:
                    return secretRoomIcon;
                case RoomType.Boss:
                    return bossRoomIcon;
                case RoomType.BossAntechamber:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(knownRoomType),
                        knownRoomType,
                        "Unsupported minimap room type.");
            }
        }

        public void BindAuthoredView(
            Image authoredRoomImage,
            Image authoredRoomIconImage,
            Sprite authoredCurrentRoomBackground,
            Sprite authoredOtherRoomBackground,
            Sprite authoredUnknownRoomIcon,
            Sprite authoredStartRoomIcon,
            Sprite authoredCombatRoomIcon,
            Sprite authoredBombRewardRoomIcon,
            Sprite authoredRecoveryRoomIcon,
            Sprite authoredSecretRoomIcon,
            Sprite authoredBossRoomIcon)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Minimap room view can only be authored outside Play Mode.");
            }

            roomImage = authoredRoomImage ??
                throw new ArgumentNullException(nameof(authoredRoomImage));
            roomIconImage = authoredRoomIconImage ??
                throw new ArgumentNullException(nameof(authoredRoomIconImage));
            currentRoomBackground = authoredCurrentRoomBackground ??
                throw new ArgumentNullException(
                    nameof(authoredCurrentRoomBackground));
            otherRoomBackground = authoredOtherRoomBackground ??
                throw new ArgumentNullException(
                    nameof(authoredOtherRoomBackground));
            unknownRoomIcon = authoredUnknownRoomIcon ??
                throw new ArgumentNullException(nameof(authoredUnknownRoomIcon));
            startRoomIcon = authoredStartRoomIcon ??
                throw new ArgumentNullException(nameof(authoredStartRoomIcon));
            combatRoomIcon = authoredCombatRoomIcon ??
                throw new ArgumentNullException(nameof(authoredCombatRoomIcon));
            bombRewardRoomIcon = authoredBombRewardRoomIcon ??
                throw new ArgumentNullException(nameof(authoredBombRewardRoomIcon));
            recoveryRoomIcon = authoredRecoveryRoomIcon ??
                throw new ArgumentNullException(nameof(authoredRecoveryRoomIcon));
            secretRoomIcon = authoredSecretRoomIcon ??
                throw new ArgumentNullException(nameof(authoredSecretRoomIcon));
            bossRoomIcon = authoredBossRoomIcon ??
                throw new ArgumentNullException(nameof(authoredBossRoomIcon));
        }
    }
}
