using System.Runtime.InteropServices;
using BombSwap.Core;

namespace BombSwap
{
    public static class WebGlHarnessReporter
    {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
        [DllImport("__Internal")]
        private static extern void BombSwapHarnessReport(string eventName);
#endif

        public static void Report(string eventName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            BombSwapHarnessReport(eventName);
#endif
        }

        public static void ReportPlayerCell(GridPosition position)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            BombSwapHarnessReport(
                $"player-cell-x-{position.X}-z-{position.Z}");
#endif
        }

        public static void ReportChaserCell(GridPosition position)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            BombSwapHarnessReport(
                $"chaser-cell-x-{position.X}-z-{position.Z}");
#endif
        }

        public static void ReportDungeonRoomReady(
            DungeonRoomNodeId roomId,
            RoomType roomType,
            bool combatEnabledForVisit,
            bool isCleared)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            string typeName;
            switch (roomType)
            {
                case RoomType.Combat:
                    typeName = "combat";
                    break;
                case RoomType.Start:
                    typeName = "start";
                    break;
                case RoomType.BombReward:
                    typeName = "bomb-reward";
                    break;
                case RoomType.BossAntechamber:
                    typeName = "boss-antechamber";
                    break;
                case RoomType.Boss:
                    typeName = "boss";
                    break;
                default:
                    typeName = "unknown";
                    break;
            }

            string stateName = combatEnabledForVisit
                ? "active"
                : isCleared
                    ? "cleared"
                    : "safe";
            BombSwapHarnessReport(
                $"dungeon-room-ready-{roomId.Value}-{typeName}-{stateName}");
#endif
        }
    }
}
