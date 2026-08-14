using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeChargerPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Color telegraphColor = new Color(1f, 0.82f, 0.08f, 1f);

        [SerializeField]
        private Color chargeColor = new Color(1f, 0.12f, 0.04f, 1f);

        [SerializeField]
        private Color recoverColor = new Color(0.42f, 0.46f, 0.5f, 1f);

        [SerializeField]
        private Color deathColor = new Color(0.14f, 0.02f, 0.02f, 1f);

        private GameObject _instance;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;
        private Color _normalColor;
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

        public ChargerEnemyState CurrentState { get; private set; }

        public Color CurrentColor { get; private set; }

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

            session.ChargerAdvanced += OnChargerAdvanced;
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
                session.ChargerAdvanced -= OnChargerAdvanced;
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
            _renderer = _instance.GetComponentInChildren<Renderer>(true);
            InitializeColor();
            _visualDuration = 1f / definition.ChargeCellsPerSecond;
            _visualTarget = ToPresentationPosition(session.CurrentChargerGridPosition);
            _visualStart = _visualTarget;
            _instance.transform.position = _visualTarget;
            _instance.SetActive(session.IsChargerAlive);
            CurrentState = session.CurrentChargerState;
            ApplyStateColor(CurrentState);
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
                ApplyStateColor(CurrentState);
            }
            if (result.HasMovement)
            {
                MoveCount++;
                _visualStart = _instance.transform.position;
                _visualTarget = ToPresentationPosition(result.Movement.To);
                _visualElapsed = 0f;
                _isInterpolating = true;
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
            _isInterpolating = false;
            ApplyColor(deathColor);
            _deathEndsAt = Time.unscaledTime + session.ChargerDefinition.DeathVisualSeconds;
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
                throw new InvalidOperationException("Charger prefab renderer requires a material.");
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
                    "Charger material requires a supported color property.");
            }

            _normalColor = material.GetColor(_colorPropertyId);
            CurrentColor = _normalColor;
        }

        private void ApplyStateColor(ChargerEnemyState state)
        {
            switch (state)
            {
                case ChargerEnemyState.Track:
                    ApplyColor(_normalColor);
                    break;
                case ChargerEnemyState.Telegraph:
                    ApplyColor(telegraphColor);
                    break;
                case ChargerEnemyState.Charge:
                    ApplyColor(chargeColor);
                    break;
                case ChargerEnemyState.Recover:
                    ApplyColor(recoverColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
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
                (Vector3.up * session.ChargerDefinition.VisualHeight);
        }
    }
}
