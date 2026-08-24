using System;
using TMPro;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInstructionView : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private TextMeshProUGUI instructionLabel;

        public Canvas Canvas => canvas;

        public TextMeshProUGUI InstructionLabel => instructionLabel;

        public bool HasRequiredReferences =>
            canvas != null &&
            instructionLabel != null;

        public void BindAuthoredView(
            Canvas authoredCanvas,
            TextMeshProUGUI authoredInstructionLabel)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Instruction view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ??
                throw new ArgumentNullException(nameof(authoredCanvas));
            instructionLabel = authoredInstructionLabel ??
                throw new ArgumentNullException(nameof(authoredInstructionLabel));
        }
    }
}
