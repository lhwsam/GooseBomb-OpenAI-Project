using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class CharacterFootstepAnimationEventRelay : MonoBehaviour
    {
        private CharacterFootstepAudio _footsteps;

        private void Awake()
        {
            if (_footsteps == null)
            {
                _footsteps = GetComponentInParent<CharacterFootstepAudio>();
            }
        }

        public void Configure(CharacterFootstepAudio footsteps)
        {
            _footsteps = footsteps;
        }

        public void PlayFootstep()
        {
            _footsteps?.PlayFootstep();
        }
    }
}
