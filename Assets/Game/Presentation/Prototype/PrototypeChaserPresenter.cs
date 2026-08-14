using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeChaserPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Color deathColor = new Color(1f, 0.08f, 0.03f, 1f);

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

        public int DeathCount { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => _instance != null && _instance.activeSelf;

        public Color CurrentColor { get; private set; }

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
                session.ChaserMoved -= OnChaserMoved;
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
            _renderer = _instance.GetComponentInChildren<Renderer>(true);
            InitializeColor();
            _visualDuration = 1f / definition.CellsPerSecond;
            _visualTarget = ToPresentationPosition(session.CurrentChaserGridPosition);
            _visualStart = _visualTarget;
            _instance.transform.position = _visualTarget;
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
            _visualStart = _instance.transform.position;
            _visualTarget = ToPresentationPosition(step.To);
            _visualElapsed = 0f;
            _isInterpolating = true;
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
            _isInterpolating = false;
            ApplyColor(deathColor);
            _deathEndsAt = Time.unscaledTime + session.ChaserDefinition.DeathVisualSeconds;
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
                throw new InvalidOperationException("Chaser prefab renderer requires a material.");
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
                    "Chaser material requires a supported color property.");
            }

            _normalColor = material.GetColor(_colorPropertyId);
            CurrentColor = _normalColor;
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
                (Vector3.up * session.ChaserDefinition.VisualHeight);
        }
    }
}
