import assert from "node:assert/strict";
import {
  PLAYTEST_LOG_SCHEMA,
  PLAYTEST_SUMMARY_SCHEMA,
  analyzePlaytestLog,
  renderPlaytestLogMarkdown,
} from "./PlaytestLogAnalyzer.mjs";

function createPayload() {
  const names = [
    "dungeon-room-ready-1-start-safe",
    "player-health-current-5",
    "minimap-visible-rooms-2",
    "minimap-visible-connections-1",
    "dungeon-room-ready-2-combat-active",
    "secret-wall-revealed-room-2-direction-west",
    "minimap-visible-rooms-4",
    "minimap-visible-connections-3",
    "dungeon-room-ready-10-secret-safe",
    "secret-reward-collected-3",
    "bomb-reward-selected-prototype-area",
    "dungeon-room-ready-8-recovery-safe",
    "player-health-recovered-2",
    "recovery-consumed-room-8",
    "dungeon-room-ready-7-boss-active",
    "room-ready-prototype-boss-arena",
    "boss-pattern-telegraph",
    "boss-moved",
    "boss-damaged",
    "boss-damaged-phase-one-state-telegraph-source-player-bomb-definition-prototype-cross-health-9",
    "boss-player-damaged-phase-one-pattern-fixed-charge-health-4",
    "boss-pattern-overheat-recovery",
    "boss-phase-two",
    "boss-self-destruct-spawned",
    "self-destruct-detonated",
    "boss-damaged",
    "boss-damaged-phase-two-state-execute-source-self-destruct-definition-prototype-self-destruct-blast-health-7",
    "boss-phase-last-stand",
    "boss-damaged",
    "boss-damaged-phase-last-stand-state-recovery-source-player-bomb-definition-prototype-area-health-0",
    "boss-defeated",
    "run-completed",
    "run-restart-requested",
    "dungeon-run-restarted",
    "dungeon-room-ready-1-start-safe",
  ];
  return {
    schemaVersion: PLAYTEST_LOG_SCHEMA,
    generatedAt: "2026-08-16T06:30:00.000Z",
    build: {
      productName: "Bomb Swap",
      productVersion: "0.1.0",
    },
    eventCount: names.length,
    events: names.map((name, index) => ({
      name,
      timestamp: 1_000 + index * 250,
    })),
  };
}

const payload = createPayload();
const summary = analyzePlaytestLog(payload);
assert.equal(summary.schemaVersion, PLAYTEST_SUMMARY_SCHEMA);
assert.equal(summary.source.eventCount, payload.eventCount);
assert.equal(
  summary.source.durationMilliseconds,
  (payload.eventCount - 1) * 250,
);
assert.deepEqual(summary.runs, {
  detectedRuns: 2,
  startRoomReadyEvents: 2,
  restartRequests: 1,
  restarts: 1,
  completed: 1,
  failed: 0,
  failureCauses: [],
});
assert.deepEqual(summary.rooms.uniqueRoomIds, [1, 2, 10, 8, 7]);
assert.equal(summary.secret.roomEntries, 1);
assert.deepEqual(summary.secret.rewardAmounts, [3]);
assert.equal(summary.minimap.maxVisibleRooms, 4);
assert.equal(summary.minimap.maxVisibleConnections, 3);
assert.deepEqual(summary.recovery.recoveredAmounts, [2]);
assert.equal(summary.recovery.consumed, 1);
assert.equal(summary.boss.telegraphs, 1);
assert.equal(summary.boss.moves, 1);
assert.equal(summary.boss.phaseTwo, 1);
assert.equal(summary.boss.lastStand, 1);
assert.equal(summary.boss.defeated, 1);
assert.equal(summary.boss.overheats, 1);
assert.equal(summary.boss.encounters.length, 1);
assert.equal(summary.boss.encounters[0].startKind, "dungeon");
assert.equal(summary.boss.encounters[0].outcome, "defeated");
assert.ok(summary.boss.encounters[0].durationMilliseconds > 0);
assert.equal(summary.boss.damage.totalEvents, 3);
assert.equal(summary.boss.damage.classifiedEvents, 3);
assert.equal(summary.boss.damage.unclassifiedEvents, 0);
assert.deepEqual(summary.boss.damage.byPhase, {
  one: 1,
  two: 1,
  "last-stand": 1,
});
assert.equal(summary.boss.damage.byState.telegraph, 1);
assert.equal(summary.boss.damage.byState.execute, 1);
assert.equal(summary.boss.damage.byState.recovery, 1);
assert.equal(summary.boss.damage.bySource["player-bomb"], 2);
assert.equal(summary.boss.damage.bySource["self-destruct"], 1);
assert.deepEqual(summary.boss.damage.playerBombDefinitionSequence, [
  "prototype-cross",
  "prototype-area",
]);
assert.equal(summary.boss.damage.playerBombDefinitionAlternations, 1);
assert.equal(summary.boss.playerPatternDamage.classifiedEvents, 1);
assert.equal(summary.boss.playerPatternDamage.byPhase.one, 1);
assert.equal(summary.boss.playerPatternDamage.byPattern["fixed-charge"], 1);
assert.equal(summary.boss.selfDestruct.spawned, 1);
assert.equal(summary.boss.selfDestruct.detonatedDuringEncounter, 1);
assert.equal(summary.boss.selfDestruct.bossHits, 1);
assert.deepEqual(summary.bombs.selectedRewards, ["prototype-area"]);
assert.ok(summary.significantTimeline.length > 0);

const partialBossCapture = createPayload();
partialBossCapture.events = partialBossCapture.events.filter(
  (event) =>
    event.name !== "dungeon-room-ready-7-boss-active" &&
    event.name !== "room-ready-prototype-boss-arena",
);
partialBossCapture.eventCount = partialBossCapture.events.length;
const partialBossSummary = analyzePlaytestLog(partialBossCapture);
assert.equal(partialBossSummary.boss.encounters.length, 0);
assert.equal(partialBossSummary.boss.damage.playerBombDefinitionAlternations, 1);

const startRoomRevisit = createPayload();
startRoomRevisit.events.push({
  name: "dungeon-room-ready-1-start-safe",
  timestamp: startRoomRevisit.events.at(-1).timestamp + 250,
});
startRoomRevisit.eventCount = startRoomRevisit.events.length;
const revisitSummary = analyzePlaytestLog(startRoomRevisit);
assert.equal(revisitSummary.runs.detectedRuns, 2);
assert.equal(revisitSummary.runs.startRoomReadyEvents, 3);

const markdown = renderPlaytestLogMarkdown(summary);
for (const requiredText of [
  "# Bomb Swap 플레이테스트 로그 요약",
  `사건: ${payload.eventCount}개`,
  "고유 방 5개 (1, 2, 10, 8, 7)",
  "비밀방 입장: 1회 / cache 보상: 3",
  "Recovery 입장: 1회 / 소비: 1회 / 회복량: 2",
  "## 보스 프로토콜 자동 계측",
  "상태별 보스 피해: telegraph 1 / execute 1 / recovery 1 / defeated 0",
  "플레이어 폭탄 정의 교대 적중: 1회",
  "보스 자폭병: 소환 1회 / 전투 중 폭발 1회 / 보스 적중 1회",
  "This summary proves emitted runtime events, not player intent or enjoyment.",
]) {
  assert.ok(markdown.includes(requiredText), `Markdown is missing: ${requiredText}`);
}

const mismatchedCount = createPayload();
mismatchedCount.eventCount += 1;
assert.throws(
  () => analyzePlaytestLog(mismatchedCount),
  /does not match events length/,
);

const backwardsTime = createPayload();
backwardsTime.events[5].timestamp = 0;
assert.throws(
  () => analyzePlaytestLog(backwardsTime),
  /must not move backwards/,
);

const wrongSchema = createPayload();
wrongSchema.schemaVersion = "bombswap/playtest-log@2";
assert.throws(
  () => analyzePlaytestLog(wrongSchema),
  /schemaVersion must be bombswap\/playtest-log@1/,
);

const nonCanonicalGeneratedAt = createPayload();
nonCanonicalGeneratedAt.generatedAt = "2026-08-16";
assert.throws(
  () => analyzePlaytestLog(nonCanonicalGeneratedAt),
  /canonical ISO 8601 UTC timestamp/,
);

const malformedEvent = createPayload();
malformedEvent.events[0] = "probe-ready";
assert.throws(
  () => analyzePlaytestLog(malformedEvent),
  /events\[0\] must be an object/,
);

process.stdout.write("BOMBSWAP_PLAYTEST_LOG_ANALYZER_TEST|passed\n");
