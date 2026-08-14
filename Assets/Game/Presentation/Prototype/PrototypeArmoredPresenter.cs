using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeArmoredPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Color brokenColor = new Color(1f, 0.34f, 0.06f, 1f);

        [SerializeField]
        private Color deathColor = new Color(0.16f, 0.02f, 0.01f, 1f);

        private GameObject _instance;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;
        private Color _armoredColor;
        private Vector3 _armoredScale;
        private Vector3 _visualStart;
        private Vector3 _visualTarget;
        private float _visualElapsed;
        private float _visualDuration;
        private float _deathEndsAt;
        private bool _isInterpolating;
        private bool _isShowingDeath;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject Instance => _instance;

        public int MoveCount { get; private set; }

        public int StateChangeCount { get; private set; }

        public int DeathCount { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => _instance != null && _instance.activeSelf;

        public ArmoredEnemyState CurrentState { get; private set; }

        public Color CurrentColor { get; private set; }

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

            session.ArmoredMoved += OnArmoredMoved;
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
                session.ArmoredMoved -= OnArmoredMoved;
                session.ArmoredStateChanged -= OnArmoredStateChanged;
                session.EnemyDied -= OnEnemyDied;
                session.Ready -= OnSessionReady;
            }
            if (_instance != null)
            {
                Destroy(_instance);
            }

            _instance = null;
            _renderer = null;
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
                return;
            }

            PrototypeArmoredDefinitionAsset definition = session.ArmoredDefinition;
            definition.ValidatePresentationReferences();
            _instance = Instantiate(definition.ArmoredPrefab, presentationRoot);
            _instance.name = "PrototypeArmoredVisual";
            _renderer = _instance.GetComponentInChildren<Renderer>(true);
            InitializeColor();
            _armoredScale = _instance.transform.localScale;
            _visualTarget = ToPresentationPosition(session.CurrentArmoredGridPosition);
            _visualStart = _visualTarget;
            _instance.transform.position = _visualTarget;
            _instance.SetActive(session.IsArmoredAlive);
            CurrentState = session.CurrentArmoredState;
            ApplyState(CurrentState);
            IsInitialized = true;
        }

        private void OnArmoredMoved(EnemyMovementStep step)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (step.ActorId != session.ArmoredActorId)
            {
                throw new InvalidOperationException(
                    "Prototype armored presenter received another actor's movement.");
            }

            MoveCount++;
            SetMovementDuration(CurrentState);
            _visualStart = _instance.transform.position;
            _visualTarget = ToPresentationPosition(step.To);
            _visualElapsed = 0f;
            _isInterpolating = true;
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
            CurrentState = result.CurrentState;
            ApplyState(CurrentState);
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
            ApplyColor(deathColor);
            _deathEndsAt = Time.unscaledTime + session.ArmoredDefinition.DeathVisualSeconds;
            _isShowingDeath = true;
        }

        private void InitializeColor()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            Material material = _renderer.sharedMaterial;
            if (material == null)
            {
                throw new InvalidOperationException("Armored enemy prefab renderer requires a material.");
            }
            if (material.HasProperty(BaseColorId))
            {
                _colorPropertyId = BaseColorId;
            }
            else if (material.HasProperty(ColorId))
            {
                _colorPropertyId = ColorId;
            }
            else
            {
                throw new InvalidOperationException(
                    "Armored enemy material requires a supported color property.");
            }

            _armoredColor = material.GetColor(_colorPropertyId);
            CurrentColor = _armoredColor;
        }

        private void ApplyState(ArmoredEnemyState state)
        {
            switch (state)
            {
                case ArmoredEnemyState.Armored:
                    ApplyColor(_armoredColor);
                    _instance.transform.localScale = _armoredScale;
                    SetMovementDuration(state);
                    break;
                case ArmoredEnemyState.Broken:
                    ApplyColor(brokenColor);
                    _instance.transform.localScale = Vector3.Scale(
                        _armoredScale,
                        new Vector3(0.82f, 0.72f, 0.82f));
                    SetMovementDuration(state);
                    break;
                case ArmoredEnemyState.Dead:
                    ApplyColor(deathColor);
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

        private void ApplyColor(Color color)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_colorPropertyId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
            CurrentColor = color;
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.ArmoredDefinition.VisualHeight);
        }
    }
}
