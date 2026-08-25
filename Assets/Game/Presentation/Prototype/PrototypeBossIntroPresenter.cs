using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BombSwap
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class PrototypeBossIntroPresenter : MonoBehaviour
    {
        public const float DefaultFocusedOrthographicSize = 5.2f;
        public const float DefaultDropHeight = 6f;
        public const float DefaultInitialStillSeconds = 0.2f;
        public const float DefaultSpawnAnticipationSeconds = 0.65f;
        public const float DefaultDropSeconds = 0.45f;
        public const float DefaultHudDelaySeconds = 0.12f;
        public const float DefaultHudPanelSeconds = 0.5f;
        public const float DefaultHudFillSeconds = 0.3f;
        public const float DefaultImpactHoldSeconds = 0.45f;
        public const float DefaultZoomOutSeconds = 0.75f;
        public const float DefaultPostZoomHoldSeconds = 0.25f;
        public const float DefaultLandingShakeAmplitude = 0.24f;
        public const float DefaultLandingShakeDuration = 0.32f;
        public const float DefaultLandingShakeFrequency = 22f;

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private PrototypeBossPresenter bossPresenter;

        [SerializeField]
        private PrototypeHealthHud healthHud;

        [SerializeField]
        private PrototypeUserSettingsRuntime settings;

        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField]
        private PrototypeCameraShake cameraShake;

        [SerializeField]
        private PrototypeLocalVfxOverrides localVfxOverrides;

        [SerializeField]
        [Min(0.01f)]
        private float focusedOrthographicSize = DefaultFocusedOrthographicSize;

        [SerializeField]
        [Min(0.01f)]
        private float dropHeight = DefaultDropHeight;

        [SerializeField]
        [Min(0f)]
        private float landingShakeAmplitude = DefaultLandingShakeAmplitude;

        [SerializeField]
        [Min(0f)]
        private float landingShakeDuration = DefaultLandingShakeDuration;

        [SerializeField]
        [Min(0f)]
        private float landingShakeFrequency = DefaultLandingShakeFrequency;

        private readonly List<GameObject> _activeVfxAnchors = new List<GameObject>(2);
        private Sequence _introSequence;
        private Vector3 _authoredCameraPosition;
        private float _authoredOrthographicSize;
        private bool _cameraStateCaptured;
        private bool _started;
        private bool _completed;

        public PrototypeGameSession Session => session;

        public PrototypeBossPresenter BossPresenter => bossPresenter;

        public PrototypeHealthHud HealthHud => healthHud;

        public PrototypeUserSettingsRuntime Settings => settings;

        public Camera GameplayCamera => gameplayCamera;

        public PrototypeCameraShake CameraShake => cameraShake;

        public float FocusedOrthographicSize => focusedOrthographicSize;

        public float DropHeight => dropHeight;

        public bool IsPlaying =>
            _introSequence != null && _introSequence.IsActive() && !_completed;

        public bool IsCompleted => _completed;

        public int SpawnVfxPlayCount { get; private set; }

        public int LightningVfxPlayCount { get; private set; }

        public int LandingShakePlayCount { get; private set; }

        public void Configure(
            PrototypeGameSession gameSession,
            PrototypeBossPresenter authoredBossPresenter,
            PrototypeHealthHud authoredHealthHud,
            PrototypeUserSettingsRuntime settingsRuntime,
            Camera authoredCamera,
            PrototypeCameraShake authoredCameraShake)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBossIntroPresenter before changing its configuration.");
            }

            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            bossPresenter = authoredBossPresenter ??
                throw new ArgumentNullException(nameof(authoredBossPresenter));
            healthHud = authoredHealthHud ??
                throw new ArgumentNullException(nameof(authoredHealthHud));
            settings = settingsRuntime ??
                throw new ArgumentNullException(nameof(settingsRuntime));
            gameplayCamera = authoredCamera ??
                throw new ArgumentNullException(nameof(authoredCamera));
            cameraShake = authoredCameraShake ??
                throw new ArgumentNullException(nameof(authoredCameraShake));
        }

        public void ConfigureLocalVfxOverrides(
            PrototypeLocalVfxOverrides authoredOverrides)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBossIntroPresenter before changing its VFX overrides.");
            }

            localVfxOverrides = authoredOverrides;
        }

        private void Awake()
        {
            ValidateReferences();
            session.PrepareBossIntroGate();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ValidateReferences();
            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                StartIntroIfNeeded();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.Ready -= OnSessionReady;
            }
            KillIntroSequence();
            if (!_completed)
            {
                RestoreCameraState();
            }
            if (cameraShake != null)
            {
                cameraShake.Stop();
            }
            DestroyActiveVfx();
            _started = false;
        }

        private void OnValidate()
        {
            focusedOrthographicSize = Mathf.Max(0.01f, focusedOrthographicSize);
            dropHeight = Mathf.Max(0.01f, dropHeight);
            landingShakeAmplitude = Mathf.Max(0f, landingShakeAmplitude);
            landingShakeDuration = Mathf.Max(0f, landingShakeDuration);
            landingShakeFrequency = Mathf.Max(0f, landingShakeFrequency);
        }

        private void OnSessionReady()
        {
            StartIntroIfNeeded();
        }

        private void StartIntroIfNeeded()
        {
            if (_started || _completed || !session.HasBoss ||
                !session.IsBossIntroPending)
            {
                return;
            }
            if (!gameplayCamera.orthographic)
            {
                throw new InvalidOperationException(
                    "Prototype boss intro requires an orthographic gameplay camera.");
            }

            _started = true;
            localVfxOverrides ??= PrototypeLocalVfxOverrides.LoadOptional();
            CaptureCameraState();
            bossPresenter.PrepareBossIntro(dropHeight);
            healthHud.PrepareBossIntro();
            FocusCameraOnBoss();

            _introSequence = DOTween.Sequence();
            _introSequence.AppendInterval(DefaultInitialStillSeconds);
            _introSequence.AppendCallback(PlaySpawnVfx);
            _introSequence.AppendInterval(DefaultSpawnAnticipationSeconds);
            _introSequence.AppendCallback(BeginBossDrop);
            _introSequence.Append(DOTween.To(
                    () => 0f,
                    bossPresenter.SetBossIntroDropProgress,
                    1f,
                    DefaultDropSeconds)
                .SetEase(Ease.InQuad));
            _introSequence.AppendCallback(HandleBossLanding);
            _introSequence.AppendInterval(DefaultHudDelaySeconds);
            _introSequence.Append(healthHud.CreateBossIntroReveal(
                DefaultHudPanelSeconds,
                DefaultHudFillSeconds));
            _introSequence.AppendInterval(DefaultImpactHoldSeconds);
            _introSequence.Append(CreateCameraReturnTween());
            _introSequence.AppendInterval(DefaultPostZoomHoldSeconds);
            _introSequence.AppendCallback(CompleteIntro);
        }

        private void BeginBossDrop()
        {
            bossPresenter.RevealBossForIntro();
            PlayLightningVfx();
        }

        private void HandleBossLanding()
        {
            bossPresenter.CompleteBossIntroLanding();
            float effectiveAmplitude = settings.ScaleScreenShake(
                landingShakeAmplitude);
            if (cameraShake.Play(
                effectiveAmplitude,
                landingShakeDuration,
                landingShakeFrequency))
            {
                LandingShakePlayCount++;
            }
        }

        private Tween CreateCameraReturnTween()
        {
            Sequence cameraReturn = DOTween.Sequence();
            cameraReturn.Join(DOTween.To(
                    () => gameplayCamera.transform.position,
                    value => gameplayCamera.transform.position = value,
                    _authoredCameraPosition,
                    DefaultZoomOutSeconds)
                .SetEase(Ease.InOutCubic));
            cameraReturn.Join(DOTween.To(
                    () => gameplayCamera.orthographicSize,
                    value => gameplayCamera.orthographicSize = value,
                    _authoredOrthographicSize,
                    DefaultZoomOutSeconds)
                .SetEase(Ease.InOutCubic));
            return cameraReturn;
        }

        private void CompleteIntro()
        {
            RestoreCameraState();
            _completed = true;
            _introSequence = null;
            if (!session.BeginBossCombat())
            {
                throw new InvalidOperationException(
                    "Boss intro completed without a pending combat gate.");
            }
        }

        private void CaptureCameraState()
        {
            _authoredCameraPosition = gameplayCamera.transform.position;
            _authoredOrthographicSize = gameplayCamera.orthographicSize;
            _cameraStateCaptured = true;
        }

        private void FocusCameraOnBoss()
        {
            Vector3 bossGroundPosition = session.GridSpace.GridToWorld(
                session.CurrentBossGridPosition);
            Vector3 forward = gameplayCamera.transform.forward;
            Vector3 focusedPosition = _authoredCameraPosition;
            if (Mathf.Abs(forward.y) > 0.0001f)
            {
                float distanceToGround =
                    (bossGroundPosition.y - _authoredCameraPosition.y) / forward.y;
                Vector3 currentGroundFocus =
                    _authoredCameraPosition + (forward * distanceToGround);
                focusedPosition += bossGroundPosition - currentGroundFocus;
            }
            else
            {
                focusedPosition.x = bossGroundPosition.x;
                focusedPosition.z = bossGroundPosition.z;
            }

            gameplayCamera.transform.position = focusedPosition;
            gameplayCamera.orthographicSize = focusedOrthographicSize;
        }

        private void RestoreCameraState()
        {
            if (!_cameraStateCaptured || gameplayCamera == null)
            {
                return;
            }

            gameplayCamera.transform.position = _authoredCameraPosition;
            gameplayCamera.orthographicSize = _authoredOrthographicSize;
        }

        private void PlaySpawnVfx()
        {
            GameObject prefab = localVfxOverrides != null
                ? localVfxOverrides.BossIntroSpawnVfxPrefab
                : null;
            if (PlayOneShotVfx(prefab, "PrototypeBossIntroSpawnVfx"))
            {
                SpawnVfxPlayCount++;
            }
        }

        private void PlayLightningVfx()
        {
            GameObject prefab = localVfxOverrides != null
                ? localVfxOverrides.BossIntroLightningVfxPrefab
                : null;
            if (PlayOneShotVfx(prefab, "PrototypeBossIntroLightningVfx"))
            {
                LightningVfxPlayCount++;
            }
        }

        private bool PlayOneShotVfx(GameObject prefab, string anchorName)
        {
            if (prefab == null)
            {
                return false;
            }

            var anchor = new GameObject(anchorName);
            anchor.transform.SetParent(bossPresenter.PresentationRoot, false);
            anchor.transform.position = session.GridSpace.GridToWorld(
                session.CurrentBossGridPosition);
            GameObject instance = Instantiate(prefab, anchor.transform, false);
            instance.name = prefab.name;
            instance.SetActive(true);
            _activeVfxAnchors.Add(anchor);
            Destroy(anchor, GetParticleLifetime(instance) + 0.25f);
            return true;
        }

        private void DestroyActiveVfx()
        {
            for (int index = 0; index < _activeVfxAnchors.Count; index++)
            {
                if (_activeVfxAnchors[index] != null)
                {
                    Destroy(_activeVfxAnchors[index]);
                }
            }
            _activeVfxAnchors.Clear();
        }

        private void KillIntroSequence()
        {
            if (_introSequence == null)
            {
                return;
            }

            _introSequence.Kill(false);
            _introSequence = null;
        }

        private void ValidateReferences()
        {
            if (session == null || bossPresenter == null || healthHud == null ||
                settings == null || gameplayCamera == null || cameraShake == null)
            {
                throw new InvalidOperationException(
                    "PrototypeBossIntroPresenter requires session, boss presenter, health HUD, settings, camera, and camera-shake references.");
            }
            if (bossPresenter.Session != session || healthHud.Session != session ||
                cameraShake.ShakeTarget != gameplayCamera.transform)
            {
                throw new InvalidOperationException(
                    "Prototype boss intro references must share the same session and gameplay camera.");
            }
        }

        private static float GetParticleLifetime(GameObject instance)
        {
            float lifetime = 0.1f;
            ParticleSystem[] systems =
                instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem.MainModule main = systems[index].main;
                lifetime = Mathf.Max(
                    lifetime,
                    main.startDelay.constantMax +
                    main.duration +
                    main.startLifetime.constantMax);
            }
            return lifetime;
        }
    }
}
