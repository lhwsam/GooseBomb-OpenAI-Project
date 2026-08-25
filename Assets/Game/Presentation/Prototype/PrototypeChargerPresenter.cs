using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeChargerPresenter : MonoBehaviour
    {
        private static readonly int IsMovingParameterId = Animator.StringToHash("IsMoving");
        private static readonly int TrackParameterId = Animator.StringToHash("Track");
        private static readonly int TelegraphParameterId = Animator.StringToHash("Telegraph");
        private static readonly int ChargeParameterId = Animator.StringToHash("Charge");
        private static readonly int RecoverParameterId = Animator.StringToHash("Recover");
        private static readonly int DieParameterId = Animator.StringToHash("Die");
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        private GameObject _instance;
        private Animator _animator;
        private PrototypeHologramFeedback _hologramFeedback;
        private PrototypeLocalHologramOverrides _localHologramOverrides;
        private float _deathRemaining;
        private bool _isShowingDeath;
        private readonly List<GameObject> _telegraphCells = new List<GameObject>();

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject Instance => _instance;

        public int MoveCount { get; private set; }

        public int StateChangeCount { get; private set; }

        public int DeathCount { get; private set; }

        public Animator Animator => _animator;

        public PrototypeHologramFeedback HologramFeedback => _hologramFeedback;

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => _instance != null && _instance.activeSelf;

        public int ActiveTelegraphCellCount { get; private set; }

        public bool UsesHologramTelegraph { get; private set; }

        public ChargerEnemyState CurrentState { get; private set; }

        public void Configure(PrototypeGameSession gameSession, Transform visualRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeChargerPresenter before changing its runtime configuration.");
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
                    "PrototypeChargerPresenter requires session and presentation-root references.");
            }

            _localHologramOverrides =
                PrototypeLocalHologramOverrides.LoadOptional();

            session.ChargerAdvanced += OnChargerAdvanced;
            session.EnemyDamaged += OnEnemyDamaged;
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
                session.ChargerAdvanced -= OnChargerAdvanced;
                session.EnemyDamaged -= OnEnemyDamaged;
                session.EnemyDied -= OnEnemyDied;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.Ready -= OnSessionReady;
            }
            if (_instance != null)
            {
                Destroy(_instance);
            }
            foreach (GameObject telegraphCell in _telegraphCells)
            {
                if (telegraphCell != null)
                {
                    Destroy(telegraphCell);
                }
            }

            _instance = null;
            _hologramFeedback = null;
            _localHologramOverrides = null;
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
            _animator = null;
            _telegraphCells.Clear();
            ActiveTelegraphCellCount = 0;
            UsesHologramTelegraph = false;
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
                    session.CurrentChargerMovementPosition) +
                    (Vector3.up * session.ChargerDefinition.VisualHeight);
            }

            SyncLocomotionAnimation();

            if (_isShowingDeath && _instance != null)
            {
                _deathRemaining -= Time.unscaledDeltaTime;
                if (_deathRemaining <= 0f)
                {
                    _instance.SetActive(false);
                    _isShowingDeath = false;
                }
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
            if (!session.HasCharger)
            {
                IsInitialized = true;
                CurrentState = ChargerEnemyState.Track;
                return;
            }

            PrototypeChargerDefinitionAsset definition = session.ChargerDefinition;
            definition.ValidatePresentationReferences();
            _instance = Instantiate(definition.ChargerPrefab, presentationRoot);
            _instance.name = "PrototypeChargerVisual";
            _hologramFeedback =
                PrototypeHologramFeedback.CreateHitFeedback(_instance);
            if (_hologramFeedback != null)
            {
                _hologramFeedback.SetPaused(session.IsPaused);
            }
            _animator = _instance.GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _animator.speed = session.IsPaused ? 0f : 1f;
                _animator.SetBool(IsMovingParameterId, false);
            }
            _instance.transform.position = ToPresentationPosition(
                session.CurrentChargerGridPosition);
            _instance.SetActive(session.IsChargerAlive);
            CurrentState = session.CurrentChargerState;
            ApplyAnimationState(CurrentState);
            if (CurrentState == ChargerEnemyState.Telegraph)
            {
                ShowTelegraphLane(
                    session.CurrentChargerGridPosition,
                    session.CurrentChargerLockedDirection,
                    session.CurrentChargerLockedChargeDistance);
            }
            IsInitialized = true;
        }

        private void OnChargerAdvanced(ChargerEnemyAdvanceResult result)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (result.ActorId != session.ChargerActorId)
            {
                throw new InvalidOperationException(
                    "Prototype charger presenter received another actor's update.");
            }

            if (result.HasStateTransition)
            {
                StateChangeCount++;
                CurrentState = result.State;
                ApplyAnimationState(CurrentState);
                if (CurrentState == ChargerEnemyState.Telegraph)
                {
                    ShowTelegraphLane(
                        session.CurrentChargerGridPosition,
                        result.Direction,
                        result.LockedChargeDistance);
                }
                else
                {
                    HideTelegraphLane();
                }
            }
            if (result.HasMovement)
            {
                MoveCount++;
                Vector3 facing = ToPresentationPosition(result.Movement.To) -
                    ToPresentationPosition(result.Movement.From);
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.0001f)
                {
                    _instance.transform.rotation =
                        Quaternion.LookRotation(facing.normalized, Vector3.up);
                }
            }
        }

        private void OnEnemyDied(EnemyDamageResult damage)
        {
            if (!session.HasCharger || damage.ActorId != session.ChargerActorId)
            {
                return;
            }
            if (!IsInitialized)
            {
                InitializePresentation();
            }

            DeathCount++;
            HideTelegraphLane();
            if (_animator != null)
            {
                _animator.SetBool(IsMovingParameterId, false);
                ResetLivingTriggers();
                _animator.SetTrigger(DieParameterId);
            }
            _deathRemaining = session.ChargerDefinition.DeathVisualSeconds;
            _isShowingDeath = true;
        }

        private void OnEnemyDamaged(EnemyDamageResult damage)
        {
            if (damage.ActorId == session.ChargerActorId &&
                _hologramFeedback != null)
            {
                _hologramFeedback.TriggerHitBlink();
            }
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (_animator != null)
            {
                _animator.speed = isPaused ? 0f : 1f;
            }
            if (_hologramFeedback != null)
            {
                _hologramFeedback.SetPaused(isPaused);
            }
        }

        private void ApplyAnimationState(ChargerEnemyState state)
        {
            if (_animator == null)
            {
                return;
            }

            ResetLivingTriggers();
            switch (state)
            {
                case ChargerEnemyState.Track:
                    _animator.SetBool(IsMovingParameterId, false);
                    _animator.SetTrigger(TrackParameterId);
                    break;
                case ChargerEnemyState.Telegraph:
                    _animator.SetBool(IsMovingParameterId, false);
                    _animator.SetTrigger(TelegraphParameterId);
                    break;
                case ChargerEnemyState.Charge:
                    _animator.SetBool(IsMovingParameterId, false);
                    _animator.SetTrigger(ChargeParameterId);
                    break;
                case ChargerEnemyState.Recover:
                    _animator.SetBool(IsMovingParameterId, false);
                    _animator.SetTrigger(RecoverParameterId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void ResetLivingTriggers()
        {
            _animator.ResetTrigger(TrackParameterId);
            _animator.ResetTrigger(TelegraphParameterId);
            _animator.ResetTrigger(ChargeParameterId);
            _animator.ResetTrigger(RecoverParameterId);
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
                session.CurrentChargerState == ChargerEnemyState.Track &&
                session.CurrentChargerLocomotionState == EnemyLocomotionState.Moving);
        }

        private void ShowTelegraphLane(
            GridPosition origin,
            CardinalDirection direction,
            int distance)
        {
            HideTelegraphLane();
            if (direction == CardinalDirection.None || distance <= 0)
            {
                return;
            }

            EnsureTelegraphCapacity(distance);
            GridPosition cell = origin;
            for (int index = 0; index < distance; index++)
            {
                cell = Offset(cell, direction);
                GameObject visual = _telegraphCells[index];
                visual.transform.position = session.GridSpace.GridToWorld(cell) +
                    (Vector3.up * 0.03f);
                visual.SetActive(true);
            }

            ActiveTelegraphCellCount = distance;
        }

        private void HideTelegraphLane()
        {
            for (int index = 0; index < ActiveTelegraphCellCount; index++)
            {
                if (_telegraphCells[index] != null)
                {
                    _telegraphCells[index].SetActive(false);
                }
            }

            ActiveTelegraphCellCount = 0;
        }

        private void EnsureTelegraphCapacity(int required)
        {
            PrototypeChargerDefinitionAsset definition = session.ChargerDefinition;
            while (_telegraphCells.Count < required)
            {
                GameObject visual = Instantiate(
                    definition.TelegraphCellPrefab,
                    presentationRoot);
                visual.name = $"PrototypeChargerTelegraphCell{_telegraphCells.Count}";
                Material hologramMaterial = _localHologramOverrides != null
                    ? _localHologramOverrides.BombRangeHologramMaterial
                    : null;
                UsesHologramTelegraph |=
                    PrototypeHologramTelegraphStyle.Apply(
                        visual,
                        hologramMaterial);
                visual.SetActive(false);
                _telegraphCells.Add(visual);
            }
        }

        private static GridPosition Offset(
            GridPosition current,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return current.Offset(0, 1);
                case CardinalDirection.East:
                    return current.Offset(1, 0);
                case CardinalDirection.South:
                    return current.Offset(0, -1);
                case CardinalDirection.West:
                    return current.Offset(-1, 0);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Telegraph lane requires a cardinal direction.");
            }
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.ChargerDefinition.VisualHeight);
        }
    }
}
