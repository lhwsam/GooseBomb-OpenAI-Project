using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace BombSwap
{
    [DefaultExecutionOrder(-1400)]
    [DisallowMultipleComponent]
    public sealed class PrototypeBgmPresenter : MonoBehaviour
    {
        private const int LobbySourceIndex = 0;
        private const int DungeonSourceStart = 1;
        private const int DungeonSourceCount = 4;
        private const int BossSourceStart = 5;
        private const int BossSourceCount = 3;
        private const int RuntimeSourceCountValue = 8;

        [SerializeField]
        private PrototypeBgmCatalogAsset catalog;

        private readonly AudioSource[] _sources = new AudioSource[RuntimeSourceCountValue];
        private readonly float[] _mixVolumes = new float[RuntimeSourceCountValue];
        private readonly float[] _fadeFrom = new float[RuntimeSourceCountValue];
        private readonly float[] _fadeTo = new float[RuntimeSourceCountValue];
        private readonly double[] _familyStartedAtDsp = new double[4];
        private readonly bool[] _familyRunning = new bool[4];
        private PrototypeGameSession _session;
        private PrototypeDungeonRoomBinder _roomBinder;
        private PrototypeBgmFamily _desiredFamily;
        private PrototypeBgmMix _desiredMix;
        private PrototypeBgmFamily _stopFamilyAfterFade;
        private double _fadeStartsAtDsp;
        private double _fadeEndsAtDsp;
        private bool _hasActiveFade;
        private bool _isPrimary;
        private bool _isAudioUnlocked;
        private bool _pendingStartedReport;
        private bool _hasReportedAudioStarted;
        private double _startedReportAtDsp;
        private ulong _boundSceneHandle;
        private bool _hasBoundScene;
        private float _pauseGain = 1f;
        private float _pauseTargetGain = 1f;

        public PrototypeBgmCatalogAsset Catalog => catalog;

        public bool IsPrimary => _isPrimary;

        public bool IsAudioUnlocked => _isAudioUnlocked;

        public PrototypeBgmFamily CurrentFamily { get; private set; }

        public PrototypeBgmFamily DesiredFamily => _desiredFamily;

        public int RuntimeSourceCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < _sources.Length; index++)
                {
                    if (_sources[index] != null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public bool HasRequiredReference => catalog != null;

        public void Configure(PrototypeBgmCatalogAsset authoredCatalog)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBgmPresenter before changing its catalog.");
            }

            catalog = authoredCatalog ?? throw new ArgumentNullException(nameof(authoredCatalog));
        }

        public void NotifyUserGesture()
        {
            if (!_isPrimary || _isAudioUnlocked)
            {
                return;
            }

            _isAudioUnlocked = true;
            InputSystem.onEvent -= OnInputEvent;
            StartDesiredFamily();
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            PrototypeBgmPresenter[] presenters = FindObjectsByType<PrototypeBgmPresenter>(
                FindObjectsInactive.Include);
            for (int index = 0; index < presenters.Length; index++)
            {
                PrototypeBgmPresenter other = presenters[index];
                if (other != this && other._isPrimary)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "PrototypeBgmPresenter requires an authored BGM catalog.");
            }
            var validationErrors = new List<string>();
            catalog.CollectValidationErrors(validationErrors);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException(validationErrors[0]);
            }

            _isPrimary = true;
            DontDestroyOnLoad(gameObject);
            CreateRuntimeSources();
            SceneManager.sceneLoaded += OnSceneLoaded;
            InputSystem.onEvent += OnInputEvent;
        }

        private void Start()
        {
            if (_isPrimary)
            {
                BindScene(SceneManager.GetActiveScene());
            }
        }

        private void Update()
        {
            if (!_isPrimary)
            {
                return;
            }

            double dspTime = AudioSettings.dspTime;
            UpdateFade(dspTime);
            UpdatePauseDuck();
            ApplyEffectiveVolumes();

            if (_pendingStartedReport && dspTime >= _startedReportAtDsp)
            {
                _pendingStartedReport = false;
                _hasReportedAudioStarted = true;
                WebGlHarnessReporter.Report("bgm-audio-started");
            }
        }

        private void OnDestroy()
        {
            if (!_isPrimary)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            InputSystem.onEvent -= OnInputEvent;
            DisconnectSession();
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice _)
        {
            if (!_isAudioUnlocked && eventPtr.GetFirstButtonPressOrNull() != null)
            {
                NotifyUserGesture();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            BindScene(scene);
        }

        private void BindScene(Scene scene)
        {
            ulong sceneHandle = scene.IsValid() ? scene.handle.GetRawData() : 0UL;
            if (!scene.IsValid() || !scene.isLoaded ||
                (_hasBoundScene && sceneHandle == _boundSceneHandle))
            {
                return;
            }

            _boundSceneHandle = sceneHandle;
            _hasBoundScene = true;
            DisconnectSession();
            PrototypeLobbyPresenter lobby = FindComponentInScene<PrototypeLobbyPresenter>(scene);
            if (lobby != null)
            {
                SetDesiredContext(PrototypeBgmFamily.Lobby, PrototypeBgmMixPolicy.Lobby);
                return;
            }

            _roomBinder = FindComponentInScene<PrototypeDungeonRoomBinder>(scene);
            _session = _roomBinder != null
                ? _roomBinder.RoomSession
                : FindComponentInScene<PrototypeGameSession>(scene);
            if (_session == null)
            {
                return;
            }

            _session.BossPatternTransitioned += OnBossPatternTransitioned;
            _session.RoomCleared += OnRoomCleared;
            _session.PauseStateChanged += OnPauseStateChanged;
            _session.PlayerDied += OnPlayerDied;
            _pauseTargetGain = _session.IsPaused ? catalog.PauseDuckGain : 1f;

            if ((_roomBinder != null && _roomBinder.RuntimeRoomType == RoomType.Boss) ||
                _session.HasBoss)
            {
                SetDesiredContext(
                    PrototypeBgmFamily.Boss,
                    PrototypeBgmMixPolicy.GetBossMix(_session.CurrentBossPhase));
            }
            else
            {
                SetDesiredContext(
                    PrototypeBgmFamily.Dungeon,
                    PrototypeBgmMixPolicy.GetDungeonMix(
                        _roomBinder != null
                            ? _roomBinder.RuntimeRoomType
                            : RoomType.Combat,
                        _session.IsRoomCleared));
            }

            if (_session.IsPlayerDead ||
                (_desiredFamily == PrototypeBgmFamily.Boss && _session.IsRoomCleared))
            {
                BeginTerminalFade();
            }
        }

        private void DisconnectSession()
        {
            if (_session != null)
            {
                _session.BossPatternTransitioned -= OnBossPatternTransitioned;
                _session.RoomCleared -= OnRoomCleared;
                _session.PauseStateChanged -= OnPauseStateChanged;
                _session.PlayerDied -= OnPlayerDied;
            }
            _session = null;
            _roomBinder = null;
            _pauseTargetGain = 1f;
        }

        private void SetDesiredContext(PrototypeBgmFamily family, PrototypeBgmMix mix)
        {
            _desiredFamily = family;
            _desiredMix = mix;
            if (!_isAudioUnlocked)
            {
                return;
            }

            if (CurrentFamily != family || !IsFamilyPlaying(family))
            {
                StartDesiredFamily();
                return;
            }

            double dspTime = AudioSettings.dspTime;
            double boundary = PrototypeBgmMixPolicy.GetNextBarBoundary(
                dspTime,
                _familyStartedAtDsp[(int)family],
                PrototypeBgmMixPolicy.GetBarSeconds(family),
                catalog.ScheduleLeadSeconds);
            BeginFade(
                CreateTargetVolumes(family, mix),
                boundary,
                catalog.CrossfadeSeconds,
                PrototypeBgmFamily.None);
        }

        private void StartDesiredFamily()
        {
            if (_desiredFamily == PrototypeBgmFamily.None)
            {
                return;
            }

            double startsAtDsp = AudioSettings.dspTime + catalog.ScheduleLeadSeconds;
            PrototypeBgmFamily previousFamily = CurrentFamily;
            if (IsFamilyPlaying(_desiredFamily))
            {
                StopFamily(_desiredFamily);
            }
            StartFamily(_desiredFamily, startsAtDsp);
            CurrentFamily = _desiredFamily;
            _familyStartedAtDsp[(int)_desiredFamily] = startsAtDsp;
            BeginFade(
                CreateTargetVolumes(_desiredFamily, _desiredMix),
                startsAtDsp,
                catalog.CrossfadeSeconds,
                previousFamily == _desiredFamily
                    ? PrototypeBgmFamily.None
                    : previousFamily);

            _pendingStartedReport = !_hasReportedAudioStarted;
            _startedReportAtDsp = startsAtDsp;
        }

        private void OnBossPatternTransitioned(BossPatternTransition transition)
        {
            if (_desiredFamily == PrototypeBgmFamily.Boss)
            {
                SetDesiredContext(
                    PrototypeBgmFamily.Boss,
                    PrototypeBgmMixPolicy.GetBossMix(transition.Phase));
            }
        }

        private void OnRoomCleared()
        {
            if (_desiredFamily == PrototypeBgmFamily.Boss)
            {
                BeginTerminalFade();
                return;
            }

            SetDesiredContext(
                PrototypeBgmFamily.Dungeon,
                PrototypeBgmMixPolicy.GetDungeonMix(
                    _roomBinder != null
                        ? _roomBinder.RuntimeRoomType
                        : RoomType.Combat,
                    true));
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            _pauseTargetGain = isPaused ? catalog.PauseDuckGain : 1f;
        }

        private void OnPlayerDied(PlayerDamageResult _)
        {
            BeginTerminalFade();
        }

        private void BeginTerminalFade()
        {
            if (!_isAudioUnlocked || CurrentFamily == PrototypeBgmFamily.None)
            {
                return;
            }

            BeginFade(
                new float[RuntimeSourceCountValue],
                AudioSettings.dspTime,
                catalog.CrossfadeSeconds,
                CurrentFamily);
        }

        private void CreateRuntimeSources()
        {
            AudioClip[] clips = catalog.GetRuntimeClips();
            for (int index = 0; index < clips.Length; index++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = catalog.BgmOutputGroup;
                source.clip = clips[index];
                source.volume = 0f;
                _sources[index] = source;
            }
        }

        private void StartFamily(PrototypeBgmFamily family, double startsAtDsp)
        {
            GetFamilyRange(family, out int start, out int count);
            for (int index = start; index < start + count; index++)
            {
                AudioSource source = _sources[index];
                source.Stop();
                source.timeSamples = 0;
                source.volume = 0f;
                source.PlayScheduled(startsAtDsp);
            }
            _familyRunning[(int)family] = true;
        }

        private void StopFamily(PrototypeBgmFamily family)
        {
            if (family == PrototypeBgmFamily.None)
            {
                return;
            }
            GetFamilyRange(family, out int start, out int count);
            for (int index = start; index < start + count; index++)
            {
                _sources[index].Stop();
                _mixVolumes[index] = 0f;
            }
            _familyRunning[(int)family] = false;
        }

        private bool IsFamilyPlaying(PrototypeBgmFamily family)
        {
            if (family == PrototypeBgmFamily.None)
            {
                return false;
            }
            return _familyRunning[(int)family];
        }

        private float[] CreateTargetVolumes(PrototypeBgmFamily family, PrototypeBgmMix mix)
        {
            var target = new float[RuntimeSourceCountValue];
            switch (family)
            {
                case PrototypeBgmFamily.Lobby:
                    target[LobbySourceIndex] = mix.BaseWeight;
                    break;
                case PrototypeBgmFamily.Dungeon:
                    target[DungeonSourceStart] = mix.BaseWeight;
                    target[DungeonSourceStart + 1] = mix.AccentWeight;
                    target[DungeonSourceStart + 2] = mix.DangerWeight;
                    target[DungeonSourceStart + 3] = mix.SanctuaryWeight;
                    break;
                case PrototypeBgmFamily.Boss:
                    target[BossSourceStart] = mix.BaseWeight;
                    target[BossSourceStart + 1] = mix.AccentWeight;
                    target[BossSourceStart + 2] = mix.DangerWeight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, null);
            }
            return target;
        }

        private void BeginFade(
            float[] target,
            double startsAtDsp,
            float durationSeconds,
            PrototypeBgmFamily stopFamilyAfterFade)
        {
            UpdateFade(AudioSettings.dspTime);
            for (int index = 0; index < RuntimeSourceCountValue; index++)
            {
                _fadeFrom[index] = _mixVolumes[index];
                _fadeTo[index] = target[index];
            }
            _fadeStartsAtDsp = startsAtDsp;
            _fadeEndsAtDsp = startsAtDsp + durationSeconds;
            _stopFamilyAfterFade = stopFamilyAfterFade;
            _hasActiveFade = true;
        }

        private void UpdateFade(double dspTime)
        {
            if (!_hasActiveFade || dspTime < _fadeStartsAtDsp)
            {
                return;
            }

            double duration = _fadeEndsAtDsp - _fadeStartsAtDsp;
            float progress = duration <= 0d
                ? 1f
                : Mathf.Clamp01((float)((dspTime - _fadeStartsAtDsp) / duration));
            for (int index = 0; index < RuntimeSourceCountValue; index++)
            {
                _mixVolumes[index] = Mathf.Lerp(_fadeFrom[index], _fadeTo[index], progress);
            }

            if (progress < 1f)
            {
                return;
            }

            _hasActiveFade = false;
            if (_stopFamilyAfterFade != PrototypeBgmFamily.None)
            {
                PrototypeBgmFamily familyToStop = _stopFamilyAfterFade;
                _stopFamilyAfterFade = PrototypeBgmFamily.None;
                StopFamily(familyToStop);
            }
        }

        private void UpdatePauseDuck()
        {
            if (Mathf.Approximately(_pauseGain, _pauseTargetGain))
            {
                _pauseGain = _pauseTargetGain;
                return;
            }

            float maximumDelta = Time.unscaledDeltaTime / catalog.PauseDuckFadeSeconds;
            _pauseGain = Mathf.MoveTowards(_pauseGain, _pauseTargetGain, maximumDelta);
        }

        private void ApplyEffectiveVolumes()
        {
            for (int index = 0; index < RuntimeSourceCountValue; index++)
            {
                _sources[index].volume = _mixVolumes[index] * _pauseGain;
            }
        }

        private static void GetFamilyRange(
            PrototypeBgmFamily family,
            out int start,
            out int count)
        {
            switch (family)
            {
                case PrototypeBgmFamily.Lobby:
                    start = LobbySourceIndex;
                    count = 1;
                    return;
                case PrototypeBgmFamily.Dungeon:
                    start = DungeonSourceStart;
                    count = DungeonSourceCount;
                    return;
                case PrototypeBgmFamily.Boss:
                    start = BossSourceStart;
                    count = BossSourceCount;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, null);
            }
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                T component = roots[rootIndex].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }
            return null;
        }
    }
}
