using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeChaserPresenter : MonoBehaviour
    {
        private static readonly int IsMovingParameterId = Animator.StringToHash("IsMoving");
        private static readonly int AttackParameterId = Animator.StringToHash("Attack");
        private static readonly int DieParameterId = Animator.StringToHash("Die");
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        private GameObject _instance;
        private Animator _animator;
        private float _deathEndsAt;
        private bool _isShowingDeath;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject Instance => _instance;

        public int MoveCount { get; private set; }

        public int DeathCount { get; private set; }

        public int AttackAnimationCount { get; private set; }

        public Animator Animator => _animator;

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => _instance != null && _instance.activeSelf;

        public void Configure(PrototypeGameSession gameSession, Transform visualRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeChaserPresenter before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (visualRoot == null)
            {
                throw new ArgumentNullException(nameof(visualRoot));
            }

            session = gameSession;
            presentationRoot = visualRoot;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || presentationRoot == null)
            {
                throw new InvalidOperationException(
                    "PrototypeChaserPresenter requires session and presentation-root references.");
            }

            session.ChaserMoved += OnChaserMoved;
            session.PlayerDamaged += OnPlayerDamaged;
            session.EnemyDied += OnEnemyDied;
            session.PauseStateChanged += OnPauseStateChanged;
            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                InitializePresentation();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.ChaserMoved -= OnChaserMoved;
                session.PlayerDamaged -= OnPlayerDamaged;
                session.EnemyDied -= OnEnemyDied;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.Ready -= OnSessionReady;
            }
            if (_instance != null)
            {
                Destroy(_instance);
            }

            _instance = null;
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
            _animator = null;
            IsInitialized = false;
            _isShowingDeath = false;
        }

        private void Update()
        {
            if (session != null && session.IsPaused)
            {
                return;
            }

            if (_instance != null && !_isShowingDeath)
            {
                _instance.transform.position = session.GridSpace.GridToWorld(
                    session.CurrentChaserMovementPosition) +
                    (Vector3.up * session.ChaserDefinition.VisualHeight);
            }

            SyncLocomotionAnimation();

            if (_isShowingDeath && Time.unscaledTime >= _deathEndsAt)
            {
                _instance.SetActive(false);
                _isShowingDeath = false;
            }
        }

        private void OnSessionReady()
        {
            InitializePresentation();
        }

        private void InitializePresentation()
        {
            if (IsInitialized)
            {
                return;
            }
            if (!session.HasChaser)
            {
                IsInitialized = true;
                return;
            }

            PrototypeChaserDefinitionAsset definition = session.ChaserDefinition;
            definition.ValidatePresentationReferences();
            _instance = Instantiate(definition.ChaserPrefab, presentationRoot);
            _instance.name = "PrototypeChaserVisual";
            _animator = _instance.GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _animator.speed = session.IsPaused ? 0f : 1f;
                _animator.SetBool(IsMovingParameterId, false);
            }
            _instance.transform.position = ToPresentationPosition(
                session.CurrentChaserGridPosition);
            _instance.SetActive(session.IsChaserAlive);
            IsInitialized = true;
        }

        private void OnChaserMoved(EnemyMovementStep step)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (step.ActorId != session.ChaserActorId)
            {
                throw new InvalidOperationException(
                    "Prototype chaser presenter received another actor's movement.");
            }

            MoveCount++;
            Vector3 facing = ToPresentationPosition(step.To) -
                ToPresentationPosition(step.From);
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                _instance.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            }
        }

        private void OnPlayerDamaged(PlayerDamageResult damage)
        {
            if (!damage.WasApplied ||
                damage.SourceKind != PlayerDamageSourceKind.EnemyContact ||
                damage.SourceActorId != session.ChaserActorId)
            {
                return;
            }

            AttackAnimationCount++;
            if (_animator != null)
            {
                _animator.SetTrigger(AttackParameterId);
            }
        }

        private void OnEnemyDied(EnemyDamageResult damage)
        {
            if (damage.ActorId != session.ChaserActorId)
            {
                return;
            }
            if (!IsInitialized)
            {
                InitializePresentation();
            }

            DeathCount++;
            if (_animator != null)
            {
                _animator.SetBool(IsMovingParameterId, false);
                _animator.ResetTrigger(AttackParameterId);
                _animator.SetTrigger(DieParameterId);
            }
            _deathEndsAt = Time.unscaledTime + session.ChaserDefinition.DeathVisualSeconds;
            _isShowingDeath = true;
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (_animator != null)
            {
                _animator.speed = isPaused ? 0f : 1f;
            }
        }

        private void SetMovingAnimation(bool isMoving)
        {
            if (_animator != null)
            {
                _animator.SetBool(IsMovingParameterId, isMoving);
            }
        }

        private void SyncLocomotionAnimation()
        {
            if (_isShowingDeath)
            {
                SetMovingAnimation(false);
                return;
            }

            SetMovingAnimation(
                session.CurrentChaserLocomotionState == EnemyLocomotionState.Moving);
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.ChaserDefinition.VisualHeight);
        }
    }
}
