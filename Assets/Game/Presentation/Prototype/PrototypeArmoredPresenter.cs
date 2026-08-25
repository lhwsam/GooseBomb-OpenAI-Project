using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeArmoredPresenter : MonoBehaviour
    {
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        private GameObject _instance;
        private Vector3 _armoredScale;
        private Vector3 _visualStart;
        private Vector3 _visualTarget;
        private float _visualElapsed;
        private float _visualDuration;
        private float _deathEndsAt;
        private bool _isInterpolating;
        private bool _isShowingDeath;
        private readonly List<GameObject> _panicTelegraphCells = new List<GameObject>();

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject Instance => _instance;

        public int MoveCount { get; private set; }

        public int StateChangeCount { get; private set; }

        public int BehaviorChangeCount { get; private set; }

        public int DeathCount { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => _instance != null && _instance.activeSelf;

        public int ActivePanicTelegraphCellCount { get; private set; }

        public ArmoredEnemyState CurrentState { get; private set; }

        public ArmoredEnemyBehaviorState CurrentBehaviorState { get; private set; }

        public void Configure(PrototypeGameSession gameSession, Transform visualRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeArmoredPresenter before changing its runtime configuration.");
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
                    "PrototypeArmoredPresenter requires session and presentation-root references.");
            }

            session.ArmoredAdvanced += OnArmoredAdvanced;
            session.ArmoredStateChanged += OnArmoredStateChanged;
            session.EnemyDied += OnEnemyDied;
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
                session.ArmoredAdvanced -= OnArmoredAdvanced;
                session.ArmoredStateChanged -= OnArmoredStateChanged;
                session.EnemyDied -= OnEnemyDied;
                session.Ready -= OnSessionReady;
            }
            if (_instance != null)
            {
                Destroy(_instance);
            }
            foreach (GameObject telegraphCell in _panicTelegraphCells)
            {
                if (telegraphCell != null)
                {
                    Destroy(telegraphCell);
                }
            }

            _instance = null;
            _panicTelegraphCells.Clear();
            ActivePanicTelegraphCellCount = 0;
            IsInitialized = false;
            _isInterpolating = false;
            _isShowingDeath = false;
        }

        private void Update()
        {
            if (_isInterpolating && _instance != null)
            {
                _visualElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(_visualElapsed / _visualDuration);
                _instance.transform.position = Vector3.LerpUnclamped(
                    _visualStart,
                    _visualTarget,
                    progress);
                if (progress >= 1f)
                {
                    _instance.transform.position = _visualTarget;
                    _isInterpolating = false;
                }
            }

            if (_isShowingDeath && _instance != null && Time.unscaledTime >= _deathEndsAt)
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
            if (!session.HasArmored)
            {
                IsInitialized = true;
                CurrentState = ArmoredEnemyState.Armored;
                CurrentBehaviorState = ArmoredEnemyBehaviorState.Guard;
                return;
            }

            PrototypeArmoredDefinitionAsset definition = session.ArmoredDefinition;
            definition.ValidatePresentationReferences();
            _instance = Instantiate(definition.ArmoredPrefab, presentationRoot);
            _instance.name = "PrototypeArmoredVisual";
            _armoredScale = _instance.transform.localScale;
            _visualTarget = ToPresentationPosition(session.CurrentArmoredGridPosition);
            _visualStart = _visualTarget;
            _instance.transform.position = _visualTarget;
            _instance.SetActive(session.IsArmoredAlive);
            CurrentState = session.CurrentArmoredState;
            CurrentBehaviorState = session.CurrentArmoredBehaviorState;
            ApplyState(CurrentState);
            if (CurrentBehaviorState == ArmoredEnemyBehaviorState.PanicTelegraph)
            {
                ShowPanicTelegraph();
            }
            IsInitialized = true;
        }

        private void OnArmoredAdvanced(ArmoredEnemyAdvanceResult result)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (result.ActorId != session.ArmoredActorId)
            {
                throw new InvalidOperationException(
                    "Prototype armored presenter received another actor's update.");
            }

            if (result.HasStateTransition)
            {
                BehaviorChangeCount++;
                CurrentBehaviorState = result.State;
                if (CurrentBehaviorState == ArmoredEnemyBehaviorState.PanicTelegraph)
                {
                    ShowPanicTelegraph();
                }
                else
                {
                    HidePanicTelegraph();
                }
            }
            if (result.HasMovement)
            {
                MoveCount++;
                SetMovementDuration(result.PreviousState);
                _visualStart = _instance.transform.position;
                _visualTarget = ToPresentationPosition(result.Movement.To);
                _visualElapsed = 0f;
                _isInterpolating = true;
            }
        }

        private void OnArmoredStateChanged(ArmoredEnemyDamageResult result)
        {
            if (!result.HasStateTransition)
            {
                return;
            }
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (result.Damage.ActorId != session.ArmoredActorId)
            {
                throw new InvalidOperationException(
                    "Prototype armored presenter received another actor's state change.");
            }

            StateChangeCount++;
            if (result.HasBehaviorTransition)
            {
                BehaviorChangeCount++;
            }
            CurrentState = result.CurrentState;
            ApplyState(CurrentState);
            CurrentBehaviorState = result.CurrentBehaviorState;
            if (result.ArmorWasBroken &&
                CurrentBehaviorState == ArmoredEnemyBehaviorState.PanicTelegraph)
            {
                ShowPanicTelegraph();
            }
            else if (CurrentBehaviorState != ArmoredEnemyBehaviorState.PanicTelegraph)
            {
                HidePanicTelegraph();
            }
        }

        private void OnEnemyDied(EnemyDamageResult damage)
        {
            if (!session.HasArmored || damage.ActorId != session.ArmoredActorId)
            {
                return;
            }
            if (!IsInitialized)
            {
                InitializePresentation();
            }

            DeathCount++;
            _isInterpolating = false;
            HidePanicTelegraph();
            _deathEndsAt = Time.unscaledTime + session.ArmoredDefinition.DeathVisualSeconds;
            _isShowingDeath = true;
        }

        private void ApplyState(ArmoredEnemyState state)
        {
            switch (state)
            {
                case ArmoredEnemyState.Armored:
                    _instance.transform.localScale = _armoredScale;
                    SetMovementDuration(state);
                    break;
                case ArmoredEnemyState.Broken:
                    _instance.transform.localScale = Vector3.Scale(
                        _armoredScale,
                        new Vector3(0.82f, 0.72f, 0.82f));
                    SetMovementDuration(state);
                    break;
                case ArmoredEnemyState.Dead:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void SetMovementDuration(ArmoredEnemyState state)
        {
            float cellsPerSecond = state == ArmoredEnemyState.Broken
                ? session.ArmoredDefinition.BrokenCellsPerSecond
                : session.ArmoredDefinition.ArmoredCellsPerSecond;
            _visualDuration = 1f / cellsPerSecond;
        }

        private void SetMovementDuration(ArmoredEnemyBehaviorState behaviorState)
        {
            float cellsPerSecond;
            switch (behaviorState)
            {
                case ArmoredEnemyBehaviorState.Guard:
                    cellsPerSecond = session.ArmoredDefinition.ArmoredCellsPerSecond;
                    break;
                case ArmoredEnemyBehaviorState.PanicRun:
                    cellsPerSecond = session.ArmoredDefinition.PanicCellsPerSecond;
                    break;
                default:
                    cellsPerSecond = session.ArmoredDefinition.BrokenCellsPerSecond;
                    break;
            }

            _visualDuration = 1f / cellsPerSecond;
        }

        private void ShowPanicTelegraph()
        {
            HidePanicTelegraph();
            int cellCount = session.CurrentArmoredPanicPathCellCount;
            if (cellCount <= 0)
            {
                return;
            }

            EnsurePanicTelegraphCapacity(cellCount);
            for (int index = 0; index < cellCount; index++)
            {
                GameObject visual = _panicTelegraphCells[index];
                GridPosition cell = session.GetCurrentArmoredPanicPathCell(index);
                visual.transform.position = session.GridSpace.GridToWorld(cell) +
                    (Vector3.up * 0.04f);
                visual.SetActive(true);
            }

            ActivePanicTelegraphCellCount = cellCount;
        }

        private void HidePanicTelegraph()
        {
            for (int index = 0; index < ActivePanicTelegraphCellCount; index++)
            {
                if (_panicTelegraphCells[index] != null)
                {
                    _panicTelegraphCells[index].SetActive(false);
                }
            }

            ActivePanicTelegraphCellCount = 0;
        }

        private void EnsurePanicTelegraphCapacity(int required)
        {
            PrototypeArmoredDefinitionAsset definition = session.ArmoredDefinition;
            while (_panicTelegraphCells.Count < required)
            {
                GameObject visual = Instantiate(
                    definition.PanicTelegraphCellPrefab,
                    presentationRoot);
                visual.name = $"PrototypeArmoredPanicTelegraphCell{_panicTelegraphCells.Count}";
                visual.SetActive(false);
                _panicTelegraphCells.Add(visual);
            }
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.ArmoredDefinition.VisualHeight);
        }
    }
}
