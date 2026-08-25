using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BombSwap
{
    public enum PrototypePlayerDeathCause
    {
        Unknown = 0,
        BombExplosion = 1,
        ChaserContact = 2,
        ChargerCharge = 3,
        ArmoredContact = 4,
        EnemyContact = 5,
        BossAttack = 6,
    }

    public static class PrototypePlayerDeathCauseFormatter
    {
        public static PrototypePlayerDeathCause Resolve(
            PlayerDamageSourceKind sourceKind,
            ActorId sourceActorId,
            ActorId chaserActorId,
            ActorId chargerActorId,
            ActorId armoredActorId)
        {
            switch (sourceKind)
            {
                case PlayerDamageSourceKind.Explosion:
                    return PrototypePlayerDeathCause.BombExplosion;
                case PlayerDamageSourceKind.EnemyContact:
                    if (sourceActorId == chaserActorId)
                    {
                        return PrototypePlayerDeathCause.ChaserContact;
                    }
                    if (sourceActorId == chargerActorId)
                    {
                        return PrototypePlayerDeathCause.ChargerCharge;
                    }
                    if (sourceActorId == armoredActorId)
                    {
                        return PrototypePlayerDeathCause.ArmoredContact;
                    }
                    return PrototypePlayerDeathCause.EnemyContact;
                case PlayerDamageSourceKind.BossPattern:
                    return PrototypePlayerDeathCause.BossAttack;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sourceKind),
                        sourceKind,
                        "Unsupported player death source kind.");
            }
        }

        public static string GetDisplayText(PrototypePlayerDeathCause cause)
        {
            switch (cause)
            {
                case PrototypePlayerDeathCause.Unknown:
                    return "CAUSE: UNKNOWN";
                case PrototypePlayerDeathCause.BombExplosion:
                    return "CAUSE: BOMB EXPLOSION";
                case PrototypePlayerDeathCause.ChaserContact:
                    return "CAUSE: CHASER CONTACT";
                case PrototypePlayerDeathCause.ChargerCharge:
                    return "CAUSE: CHARGER CHARGE";
                case PrototypePlayerDeathCause.ArmoredContact:
                    return "CAUSE: ARMORED ENEMY CONTACT";
                case PrototypePlayerDeathCause.EnemyContact:
                    return "CAUSE: ENEMY CONTACT";
                case PrototypePlayerDeathCause.BossAttack:
                    return "CAUSE: BOSS ATTACK";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cause),
                        cause,
                        "Unsupported prototype player death cause.");
            }
        }

        public static string GetHarnessEvent(PrototypePlayerDeathCause cause)
        {
            switch (cause)
            {
                case PrototypePlayerDeathCause.Unknown:
                    return "run-failed-cause-unknown";
                case PrototypePlayerDeathCause.BombExplosion:
                    return "run-failed-cause-bomb-explosion";
                case PrototypePlayerDeathCause.ChaserContact:
                    return "run-failed-cause-chaser-contact";
                case PrototypePlayerDeathCause.ChargerCharge:
                    return "run-failed-cause-charger-charge";
                case PrototypePlayerDeathCause.ArmoredContact:
                    return "run-failed-cause-armored-contact";
                case PrototypePlayerDeathCause.EnemyContact:
                    return "run-failed-cause-enemy-contact";
                case PrototypePlayerDeathCause.BossAttack:
                    return "run-failed-cause-boss-attack";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cause),
                        cause,
                        "Unsupported prototype player death cause.");
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeRunCompletionPresenter : MonoBehaviour
    {
        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        [SerializeField]
        private BombSwapInputReader inputReader;

        [SerializeField]
        private PrototypeRunCompletionView viewPrefab;

        private PrototypeRunCompletionView _viewInstance;
        private TextMeshProUGUI _failureCauseLabel;
        private Button _restartButton;
        private Button _lobbyButton;
        private bool _checkResultNextFrame;
        private bool _restartRequested;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public BombSwapInputReader InputReader => inputReader;

        public PrototypeRunCompletionView ViewPrefab => viewPrefab;

        public PrototypeRunCompletionView ViewInstance => _viewInstance;

        public bool IsVisible { get; private set; }

        public int CompletionCount { get; private set; }

        public int FailureCount { get; private set; }

        public int RestartRequestCount { get; private set; }

        public int LobbyRequestCount { get; private set; }

        public Button RestartButton => _restartButton;

        public Button LobbyButton => _lobbyButton;

        public PrototypePlayerDeathCause? FailureCause { get; private set; }

        public string FailureCauseText =>
            _failureCauseLabel != null ? _failureCauseLabel.text : string.Empty;

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            BombSwapInputReader authoredInputReader)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeRunCompletionPresenter before changing its configuration.");
            }

            roomBinder = authoredRoomBinder ??
                throw new ArgumentNullException(nameof(authoredRoomBinder));
            inputReader = authoredInputReader ??
                throw new ArgumentNullException(nameof(authoredInputReader));
        }

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            BombSwapInputReader authoredInputReader,
            PrototypeRunCompletionView authoredViewPrefab)
        {
            Configure(authoredRoomBinder, authoredInputReader);
            BindViewPrefab(authoredViewPrefab);
        }

        public void BindViewPrefab(
            PrototypeRunCompletionView authoredViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeRunCompletionPresenter before changing its view prefab.");
            }

            viewPrefab = authoredViewPrefab ??
                throw new ArgumentNullException(nameof(authoredViewPrefab));
        }

        public void RequestRestart()
        {
            if (!IsVisible || _restartRequested)
            {
                return;
            }

            PrototypeDungeonRunHost host = roomBinder.RunHost;
            if (host == null || host.RunSession == null || !host.RunSession.IsFinished)
            {
                throw new InvalidOperationException(
                    "Run restart requires a completed or failed primary dungeon run.");
            }

            _restartRequested = true;
            RestartRequestCount++;
            SetButtonsInteractable(false);
            WebGlHarnessReporter.Report("run-restart-requested");
            try
            {
                host.RestartFinishedRun();
            }
            catch
            {
                _restartRequested = false;
                SetButtonsInteractable(true);
                throw;
            }
        }

        public void RequestReturnToLobby()
        {
            if (!IsVisible || _restartRequested)
            {
                return;
            }

            PrototypeDungeonRunHost host = roomBinder.RunHost;
            if (host == null || host.RunSession == null || !host.RunSession.IsFinished)
            {
                throw new InvalidOperationException(
                    "Lobby return requires a completed or failed primary dungeon run.");
            }

            _restartRequested = true;
            LobbyRequestCount++;
            SetButtonsInteractable(false);
            try
            {
                host.ExitFinishedRunToScene(
                    PrototypeLobbyPresenter.DefaultLobbySceneName);
            }
            catch
            {
                _restartRequested = false;
                SetButtonsInteractable(true);
                throw;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (roomBinder == null || roomBinder.RoomSession == null ||
                inputReader == null || viewPrefab == null ||
                !viewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeRunCompletionPresenter requires room-binder, input-reader, and view-prefab references.");
            }

            roomBinder.RoomSession.Ready += OnSessionReady;
            roomBinder.RoomSession.RoomCleared += OnRoomCleared;
            roomBinder.RoomSession.PlayerDied += OnPlayerDied;
            inputReader.CommandIssued += OnCommandIssued;
            if (roomBinder.RoomSession.IsReady)
            {
                _checkResultNextFrame = true;
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                TryShowResult();
            }
        }

        private void LateUpdate()
        {
            if (!_checkResultNextFrame)
            {
                return;
            }

            _checkResultNextFrame = false;
            TryShowResult();
        }

        private void OnDisable()
        {
            if (roomBinder != null && roomBinder.RoomSession != null)
            {
                roomBinder.RoomSession.Ready -= OnSessionReady;
                roomBinder.RoomSession.RoomCleared -= OnRoomCleared;
                roomBinder.RoomSession.PlayerDied -= OnPlayerDied;
            }
            if (inputReader != null)
            {
                inputReader.CommandIssued -= OnCommandIssued;
            }
        }

        private void OnSessionReady()
        {
            _checkResultNextFrame = true;
        }

        private void OnRoomCleared()
        {
            _checkResultNextFrame = true;
        }

        private void OnPlayerDied(PlayerDamageResult _)
        {
            _checkResultNextFrame = true;
        }

        private void OnCommandIssued(PlayerCommand command)
        {
            if (command.Kind == PlayerCommandKind.RestartRun)
            {
                RequestRestart();
            }
        }

        private void TryShowResult()
        {
            if (IsVisible)
            {
                return;
            }

            PrototypeDungeonRunHost host = roomBinder.RunHost;
            PrototypeGameSession session = roomBinder.RoomSession;
            if (host == null || host.RunSession == null)
            {
                return;
            }

            bool failed = host.RunSession.IsFailed;
            bool completed = host.RunSession.IsComplete &&
                roomBinder.RuntimeRoomType == RoomType.Boss &&
                session.IsRoomCleared;
            if (!failed && !completed)
            {
                return;
            }

            if (failed)
            {
                PlayerDamageResult? failureDamage = host.RunSession.FailureDamage;
                if (!failureDamage.HasValue)
                {
                    throw new InvalidOperationException(
                        "A failed dungeon run is missing its fatal damage result.");
                }

                FailureCause = PrototypePlayerDeathCauseFormatter.Resolve(
                    failureDamage.Value.SourceKind,
                    failureDamage.Value.SourceActorId,
                    session.ChaserActorId,
                    session.ChargerActorId,
                    session.ArmoredActorId);
            }
            else
            {
                FailureCause = null;
            }

            CreateUi(failed);
            IsVisible = true;
            if (failed)
            {
                FailureCount++;
            }
            else
            {
                CompletionCount++;
            }
            session.enabled = false;
            WebGlHarnessReporter.Report(failed ? "run-failed" : "run-completed");
            if (failed)
            {
                WebGlHarnessReporter.Report(
                    PrototypePlayerDeathCauseFormatter.GetHarnessEvent(
                        FailureCause.Value));
            }
        }

        private void CreateUi(bool failed)
        {
            _viewInstance = Instantiate(viewPrefab, transform, false);
            _viewInstance.name = viewPrefab.name;
            if (!_viewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated run completion view is missing required references.");
            }

            TextMeshProUGUI title = _viewInstance.TitleLabel;
            title.text = failed ? "RUN FAILED" : "FLOOR CLEARED";
            title.color = failed
                ? _viewInstance.FailedTitleColor
                : _viewInstance.CompletedTitleColor;

            _failureCauseLabel = _viewInstance.FailureCauseLabel;
            _failureCauseLabel.gameObject.SetActive(failed);
            if (failed)
            {
                _failureCauseLabel.text =
                    PrototypePlayerDeathCauseFormatter.GetDisplayText(
                        FailureCause.Value);
            }

            _restartButton = _viewInstance.RestartButton;
            _restartButton.onClick.AddListener(RequestRestart);

            _lobbyButton = _viewInstance.LobbyButton;
            _lobbyButton.onClick.AddListener(RequestReturnToLobby);

            EventSystem eventSystem = PrototypeUiFactory.EnsureEventSystem();
            eventSystem.SetSelectedGameObject(_restartButton.gameObject);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_restartButton != null)
            {
                _restartButton.interactable = interactable;
            }
            if (_lobbyButton != null)
            {
                _lobbyButton.interactable = interactable;
            }
        }
    }
}
