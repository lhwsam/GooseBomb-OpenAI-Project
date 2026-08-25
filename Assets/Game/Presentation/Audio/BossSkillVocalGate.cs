using BombSwap.Core;

namespace BombSwap
{
    public sealed class BossSkillVocalGate
    {
        private bool _isParityWaveSequenceActive;
        private bool _playOnNextParityExecute;

        public bool ShouldPlay(BossBattleState state, BossPatternKind pattern)
        {
            if (state == BossBattleState.Telegraph)
            {
                if (pattern == BossPatternKind.ParityWave)
                {
                    if (!_isParityWaveSequenceActive)
                    {
                        _isParityWaveSequenceActive = true;
                        _playOnNextParityExecute = true;
                    }
                }
                else
                {
                    Reset();
                }

                return false;
            }

            if (state != BossBattleState.Execute)
            {
                return false;
            }
            if (pattern != BossPatternKind.ParityWave)
            {
                return true;
            }
            if (!_playOnNextParityExecute)
            {
                return false;
            }

            _playOnNextParityExecute = false;
            return true;
        }

        public void Reset()
        {
            _isParityWaveSequenceActive = false;
            _playOnNextParityExecute = false;
        }
    }
}
