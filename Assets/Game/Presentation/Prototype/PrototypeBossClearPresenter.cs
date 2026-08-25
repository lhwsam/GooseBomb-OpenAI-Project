using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBossClearPresenter : MonoBehaviour
    {
        public const float DefaultFocusedOrthographicSize = 4.7f;
        public const float DefaultFocusSeconds = 0.65f;
        public const float DefaultDeathAnimatorSpeed = 0.5f;
        public const float DefaultBurstIntervalSeconds = 0.45f;
        public const float DefaultFinalBurstHoldSeconds = 0.5f;
        public const float DefaultCoverSeconds = 0.4f;
        public const float DefaultCoverHoldSeconds = 0.18f;
        public const float DefaultRevealSeconds = 0.25f;
        public const float DefaultBurstShakeAmplitude = 0.07f;
        public const float DefaultBurstShakeDuration = 0.12f;
        public const float DefaultBurstShakeFrequency = 28f;
        public const float DefaultFinalShakeAmplitude = 0.2f;
        public const float DefaultFinalShakeDuration = 0.28f;
        public const float DefaultFinalShakeFrequency = 22f;

        private static readonly Vector3[] BurstOffsets =
        {
            new Vector3(-0.55f, 0.35f, 0.2f),
            new Vector3(0.5f, 0.7f, -0.15f),
            new Vector3(-0.2f, 1.05f, -0.1f),
            new Vector3(0.1f, 0.55f, 0.15f),
        };

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private PrototypeBossPresenter bossPresenter;

        [SerializeField]
        private PrototypeBombPresenter bombPresenter;

        [SerializeField]
        private PrototypeUserSettingsRuntime settings;

        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField]
        private PrototypeCameraShake cameraShake;

        [SerializeField]
        private PrototypeBossClearTransitionView transitionViewPrefab;

        [SerializeField]
        private PrototypeLocalVfxOverrides localVfxOverrides;

        [SerializeField]
        [Min(0.01f)]
        private float focusedOrthographicSize = DefaultFocusedOrthographicSize;

        [SerializeField]
        [Range(0.05f, 1f)]
        private float deathAnimatorSpeed = DefaultDeathAnimatorSpeed;

        [SerializeField]
        [Min(0f)]
        private float burstShakeAmplitude = DefaultBurstShakeAmplitude;

        [SerializeField]
        [Min(0f)]
        private float finalShakeAmplitude = DefaultFinalShakeAmplitude;

        private readonly List<GameObject> _activeBurstVfx = new List<GameObject>(4);
        private Sequence _clearSequence;
        private PrototypeBossClearTransitionView _transitionViewInstance;
        private Vector3 _cameraRestingPosition;
        private float _cameraRestingOrthographicSize;
        private bool _cameraStateCaptured;
        private bool _started;

        public event Action Completed;

        public PrototypeGameSession Session => session;

        public PrototypeBossPresenter BossPresenter => bossPresenter;

        public PrototypeBombPresenter BombPresenter => bombPresenter;

        public PrototypeUserSettingsRuntime Settings => settings;

        public Camera GameplayCamera => gameplayCamera;

        public PrototypeCameraShake CameraShake => cameraShake;

        public PrototypeBossClearTransitionView TransitionViewPrefab =>
            transitionViewPrefab;

        public PrototypeBossClearTransitionView TransitionViewInstance =>
            _transitionViewInstance;

        public float FocusedOrthographicSize => focusedOrthographicSize;

        public float DeathAnimatorSpeed => deathAnimatorSpeed;

        public bool HasStarted => _started;

        public bool IsPlaying =>
            _clearSequence != null && _clearSequence.IsActive() && !IsCompleted;

        public bool IsCompleted { get; private set; }

        public int BurstVfxPlayCount { get; private set; }

        public int ShakePlayCount { get; private set; }

        public int CompletionCount { get; private set; }

        public void Configure(
            PrototypeGameSession gameSession,
            PrototypeBossPresenter authoredBossPresenter,
            PrototypeBombPresenter authoredBombPresenter,
            PrototypeUserSettingsRuntime settingsRuntime,
            Camera authoredCamera,
            PrototypeCameraShake authoredCameraShake,
            PrototypeBossClearTransitionView authoredTransitionViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBossClearPresenter before changing its configuration.");
            }

            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            bossPresenter = authoredBossPresenter ??
                throw new ArgumentNullException(nameof(authoredBossPresenter));
            bombPresenter = authoredBombPresenter ??
                throw new ArgumentNullException(nameof(authoredBombPresenter));
            settings = settingsRuntime ?? throw new ArgumentNullException(nameof(settingsRuntime));
            gameplayCamera = authoredCamera ??
                throw new ArgumentNullException(nameof(authoredCamera));
            cameraShake = authoredCameraShake ??
                throw new ArgumentNullException(nameof(authoredCameraShake));
            transitionViewPrefab = authoredTransitionViewPrefab ??
                throw new ArgumentNullException(nameof(authoredTransitionViewPrefab));
        }

        public void ConfigureLocalVfxOverrides(
            PrototypeLocalVfxOverrides authoredOverrides)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBossClearPresenter before changing its VFX overrides.");
            }

            localVfxOverrides = authoredOverrides;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ValidateReferences();
            session.RoomCleared += OnRoomCleared;
            if (session.IsRoomCleared)
            {
                TryStartPresentation();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.RoomCleared -= OnRoomCleared;
            }
            KillSequence();
            RestoreCameraState();
            if (cameraShake != null)
            {
                cameraShake.Stop();
            }
            if (bossPresenter != null && !IsCompleted)
            {
                bossPresenter.CancelBossClearPresentation();
            }
            DestroyBurstVfx();
            DestroyTransitionView();
        }

        private void OnValidate()
        {
            focusedOrthographicSize = Mathf.Max(0.01f, focusedOrthographicSize);
            deathAnimatorSpeed = Mathf.Clamp(deathAnimatorSpeed, 0.05f, 1f);
            burstShakeAmplitude = Mathf.Max(0f, burstShakeAmplitude);
            finalShakeAmplitude = Mathf.Max(0f, finalShakeAmplitude);
        }

        private void OnRoomCleared()
        {
            TryStartPresentation();
        }

        private void TryStartPresentation()
        {
            if (_started || IsCompleted || !session.HasBoss ||
                session.IsPlayerDead || session.IsBossAlive)
            {
                return;
            }
            if (!gameplayCamera.orthographic)
            {
                throw new InvalidOperationException(
                    "Prototype boss clear presentation requires an orthographic gameplay camera.");
            }

            _started = true;
            session.enabled = false;
            localVfxOverrides ??= PrototypeLocalVfxOverrides.LoadOptional();
            CaptureCameraState();
            bombPresenter.HideAllForBossClear();
            bossPresenter.BeginBossClearPresentation(deathAnimatorSpeed);
            CreateTransitionView();

            Vector3 focusedPosition =
                PrototypeCameraFramingUtility.CalculateGroundFocusPosition(
                    gameplayCamera,
                    session.GridSpace.GridToWorld(
                        session.CurrentBossGridPosition));

            _clearSequence = DOTween.Sequence().SetUpdate(true);
            _clearSequence.Join(DOTween.To(
                    () => gameplayCamera.transform.position,
                    value => gameplayCamera.transform.position = value,
                    focusedPosition,
                    DefaultFocusSeconds)
                .SetEase(Ease.InOutCubic));
            _clearSequence.Join(DOTween.To(
                    () => gameplayCamera.orthographicSize,
                    value => gameplayCamera.orthographicSize = value,
                    focusedOrthographicSize,
                    DefaultFocusSeconds)
                .SetEase(Ease.InOutCubic));

            for (int index = 0; index < BurstOffsets.Length - 1; index++)
            {
                int burstIndex = index;
                _clearSequence.AppendCallback(() => PlayBurst(burstIndex, false));
                _clearSequence.AppendInterval(DefaultBurstIntervalSeconds);
            }

            _clearSequence.AppendCallback(() =>
                PlayBurst(BurstOffsets.Length - 1, true));
            _clearSequence.AppendInterval(DefaultFinalBurstHoldSeconds);
            _clearSequence.AppendCallback(
                bossPresenter.CompleteBossClearPresentation);
            _clearSequence.Append(
                _transitionViewInstance.CreateCloseTween(DefaultCoverSeconds));
            _clearSequence.AppendCallback(CompletePresentationUnderCover);
            _clearSequence.AppendInterval(DefaultCoverHoldSeconds);
            _clearSequence.Append(
                _transitionViewInstance.CreateRevealTween(DefaultRevealSeconds));
            _clearSequence.OnComplete(FinishSequence);
        }

        private void CreateTransitionView()
        {
            _transitionViewInstance = Instantiate(
                transitionViewPrefab,
                transform,
                false);
            _transitionViewInstance.name = transitionViewPrefab.name;
            if (!_transitionViewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated boss-clear transition view is missing required references.");
            }
            _transitionViewInstance.PrepareForClosing();
        }

        private void PlayBurst(int burstIndex, bool isFinal)
        {
            Vector3 center = session.GridSpace.GridToWorld(
                    session.CurrentBossGridPosition) +
                (Vector3.up * session.BossDefinition.VisualHeight);
            GameObject prefab = localVfxOverrides != null
                ? localVfxOverrides.CrossBombCenterExplosionVfxPrefab
                : null;
            GameObject instance = prefab != null
                ? Instantiate(prefab, bossPresenter.PresentationRoot, false)
                : CreateFallbackBurstVfx();
            instance.name = isFinal
                ? "PrototypeBossClearFinalBurst"
                : "PrototypeBossClearBurst_" + burstIndex;
            instance.transform.position = center + BurstOffsets[burstIndex];
            instance.SetActive(true);

            ParticleSystem[] systems =
                instance.GetComponentsInChildren<ParticleSystem>(true);
            float lifetime = RestartParticleSystems(systems);
            _activeBurstVfx.Add(instance);
            Destroy(instance, Mathf.Max(lifetime, 0.35f) + 0.1f);
            BurstVfxPlayCount++;

            float authoredAmplitude = isFinal
                ? finalShakeAmplitude
                : burstShakeAmplitude;
            float effectiveAmplitude = settings.ScaleScreenShake(authoredAmplitude);
            if (cameraShake.Play(
                effectiveAmplitude,
                isFinal ? DefaultFinalShakeDuration : DefaultBurstShakeDuration,
                isFinal ? DefaultFinalShakeFrequency : DefaultBurstShakeFrequency))
            {
                ShakePlayCount++;
            }
        }

        private void CompletePresentationUnderCover()
        {
            RestoreCameraState();
            IsCompleted = true;
            CompletionCount++;
            WebGlHarnessReporter.Report("boss-clear-presentation-completed");
            Completed?.Invoke();
        }

        private void FinishSequence()
        {
            _clearSequence = null;
            DestroyTransitionView();
        }

        private void CaptureCameraState()
        {
            _cameraRestingPosition = gameplayCamera.transform.position;
            _cameraRestingOrthographicSize = gameplayCamera.orthographicSize;
            _cameraStateCaptured = true;
            WebGlHarnessReporter.Report("boss-clear-presentation-started");
        }

        private void RestoreCameraState()
        {
            if (!_cameraStateCaptured || gameplayCamera == null)
            {
                return;
            }

            gameplayCamera.transform.position = _cameraRestingPosition;
            gameplayCamera.orthographicSize = _cameraRestingOrthographicSize;
            _cameraStateCaptured = false;
        }

        private void KillSequence()
        {
            if (_clearSequence == null)
            {
                return;
            }

            _clearSequence.Kill(false);
            _clearSequence = null;
        }

        private void DestroyTransitionView()
        {
            if (_transitionViewInstance != null)
            {
                Destroy(_transitionViewInstance.gameObject);
                _transitionViewInstance = null;
            }
        }

        private void DestroyBurstVfx()
        {
            for (int index = 0; index < _activeBurstVfx.Count; index++)
            {
                if (_activeBurstVfx[index] != null)
                {
                    Destroy(_activeBurstVfx[index]);
                }
            }
            _activeBurstVfx.Clear();
        }

        private void ValidateReferences()
        {
            if (session == null || bossPresenter == null ||
                bombPresenter == null || settings == null ||
                gameplayCamera == null || cameraShake == null ||
                transitionViewPrefab == null ||
                !transitionViewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeBossClearPresenter requires session, boss presenter, bomb presenter, settings, camera, camera shake, and transition-view references.");
            }
            if (bossPresenter.Session != session ||
                bombPresenter.Session != session ||
                cameraShake.ShakeTarget != gameplayCamera.transform)
            {
                throw new InvalidOperationException(
                    "Prototype boss-clear references must share the same session and gameplay camera.");
            }
        }

        private static float RestartParticleSystems(ParticleSystem[] systems)
        {
            float lifetime = 0.1f;
            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem system = systems[index];
                system.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(true);
                ParticleSystem.MainModule main = system.main;
                lifetime = Mathf.Max(
                    lifetime,
                    main.startDelay.constantMax +
                    main.duration +
                    main.startLifetime.constantMax);
            }
            return lifetime;
        }

        private static GameObject CreateFallbackBurstVfx()
        {
            var instance = new GameObject("PrototypeBossClearBurstFallback");
            ParticleSystem particleSystem = instance.AddComponent<ParticleSystem>();
            particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = 0.3f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.38f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.4f, 1f),
                new Color(1f, 0.18f, 0.03f, 0.85f));
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 18),
            });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;
            return instance;
        }
    }
}
