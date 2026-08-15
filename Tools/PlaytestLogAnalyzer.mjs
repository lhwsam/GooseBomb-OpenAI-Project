export const PLAYTEST_LOG_SCHEMA = "bombswap/playtest-log@1";
export const PLAYTEST_SUMMARY_SCHEMA = "bombswap/playtest-summary@1";

function requireCondition(condition, message) {
  if (!condition) throw new Error(message);
}

function requireNonEmptyString(value, label) {
  requireCondition(
    typeof value === "string" && value.trim().length > 0,
    `${label} must be a non-empty string.`,
  );
}

function matchesFrom(events, pattern, valueSelector = (match) => match[1]) {
  const values = [];
  for (const event of events) {
    const match = pattern.exec(event.name);
    if (match) values.push(valueSelector(match));
  }
  return values;
}

function countExact(events, name) {
  return events.reduce(
    (count, event) => count + (event.name === name ? 1 : 0),
    0,
  );
}

function maxOrZero(values) {
  return values.length > 0 ? Math.max(...values) : 0;
}

function isSignificantEvent(name) {
  return /^(?:dungeon-room-ready-|dungeon-run-restarted$|secret-wall-revealed-|secret-reward-collected-|bomb-reward-selected-|player-health-recovered-|recovery-consumed-|boss-pattern-|boss-move-target-|boss-moved$|boss-phase-two$|boss-defeated$|run-completed$|run-failed(?:-|$))/.test(name);
}

function validateEvents(rawEvents) {
  requireCondition(Array.isArray(rawEvents), "events must be an array.");
  requireCondition(rawEvents.length > 0, "events must not be empty.");

  let previousTimestamp = -Infinity;
  return rawEvents.map((event, index) => {
    requireCondition(
      event !== null && typeof event === "object" && !Array.isArray(event),
      `events[${index}] must be an object.`,
    );
    requireNonEmptyString(event.name, `events[${index}].name`);
    requireCondition(
      Number.isSafeInteger(event.timestamp) && event.timestamp >= 0,
      `events[${index}].timestamp must be a non-negative safe integer.`,
    );
    requireCondition(
      event.timestamp >= previousTimestamp,
      `events[${index}].timestamp must not move backwards.`,
    );
    previousTimestamp = event.timestamp;
    return { name: event.name, timestamp: event.timestamp };
  });
}

export function analyzePlaytestLog(payload) {
  requireCondition(
    payload !== null && typeof payload === "object" && !Array.isArray(payload),
    "Playtest log root must be an object.",
  );
  requireCondition(
    payload.schemaVersion === PLAYTEST_LOG_SCHEMA,
    `schemaVersion must be ${PLAYTEST_LOG_SCHEMA}.`,
  );
  requireNonEmptyString(payload.generatedAt, "generatedAt");
  const generatedAtTimestamp = Date.parse(payload.generatedAt);
  requireCondition(
    Number.isFinite(generatedAtTimestamp) &&
      new Date(generatedAtTimestamp).toISOString() === payload.generatedAt,
    "generatedAt must be a canonical ISO 8601 UTC timestamp.",
  );
  requireCondition(
    payload.build !== null &&
      typeof payload.build === "object" &&
      !Array.isArray(payload.build),
    "build must be an object.",
  );
  requireNonEmptyString(payload.build.productName, "build.productName");
  requireNonEmptyString(payload.build.productVersion, "build.productVersion");
  requireCondition(
    Number.isInteger(payload.eventCount) && payload.eventCount > 0,
    "eventCount must be a positive integer.",
  );

  const events = validateEvents(payload.events);
  requireCondition(
    payload.eventCount === events.length,
    `eventCount ${payload.eventCount} does not match events length ${events.length}.`,
  );

  const firstTimestamp = events[0].timestamp;
  const lastTimestamp = events.at(-1).timestamp;
  const roomVisits = [];
  for (let index = 0; index < events.length; index += 1) {
    const event = events[index];
    const match = /^dungeon-room-ready-(\d+)-(.+)-(active|cleared|safe)$/.exec(
      event.name,
    );
    if (!match) continue;
    roomVisits.push({
      eventIndex: index,
      roomId: Number(match[1]),
      roomType: match[2],
      state: match[3],
      timestamp: event.timestamp,
      elapsedMilliseconds: event.timestamp - firstTimestamp,
    });
  }

  const failureCauses = matchesFrom(
    events,
    /^run-failed-cause-(.+)$/,
  );
  const selectedBombs = matchesFrom(
    events,
    /^bomb-reward-selected-(.+)$/,
  );
  const recoveredAmounts = matchesFrom(
    events,
    /^player-health-recovered-(\d+)$/,
    (match) => Number(match[1]),
  );
  const secretRewardAmounts = matchesFrom(
    events,
    /^secret-reward-collected-(\d+)$/,
    (match) => Number(match[1]),
  );
  const visibleRoomCounts = matchesFrom(
    events,
    /^minimap-visible-rooms-(\d+)$/,
    (match) => Number(match[1]),
  );
  const visibleConnectionCounts = matchesFrom(
    events,
    /^minimap-visible-connections-(\d+)$/,
    (match) => Number(match[1]),
  );
  const uniqueRoomIds = [...new Set(roomVisits.map((visit) => visit.roomId))];

  return {
    schemaVersion: PLAYTEST_SUMMARY_SCHEMA,
    source: {
      schemaVersion: payload.schemaVersion,
      generatedAt: payload.generatedAt,
      build: {
        productName: payload.build.productName,
        productVersion: payload.build.productVersion,
      },
      eventCount: events.length,
      firstEventTimestamp: firstTimestamp,
      lastEventTimestamp: lastTimestamp,
      durationMilliseconds: lastTimestamp - firstTimestamp,
    },
    runs: {
      detectedRuns: 1 + countExact(events, "dungeon-run-restarted"),
      startRoomReadyEvents: countExact(
        events,
        "dungeon-room-ready-1-start-safe",
      ),
      restartRequests: countExact(events, "run-restart-requested"),
      restarts: countExact(events, "dungeon-run-restarted"),
      completed: countExact(events, "run-completed"),
      failed: countExact(events, "run-failed"),
      failureCauses,
    },
    rooms: {
      visitCount: roomVisits.length,
      uniqueRoomCount: uniqueRoomIds.length,
      uniqueRoomIds,
      visits: roomVisits,
    },
    bombs: {
      selectedRewards: selectedBombs,
      swaps: countExact(events, "swap-bomb"),
      slotOneActivations: countExact(events, "active-bomb-slot-1"),
    },
    secret: {
      wallsRevealed: matchesFrom(
        events,
        /^secret-wall-revealed-(.+)$/,
      ),
      roomEntries: roomVisits.filter((visit) => visit.roomType === "secret").length,
      rewardAmounts: secretRewardAmounts,
    },
    minimap: {
      maxVisibleRooms: maxOrZero(visibleRoomCounts),
      maxVisibleConnections: maxOrZero(visibleConnectionCounts),
      lastVisibleRooms: visibleRoomCounts.at(-1) ?? 0,
      lastVisibleConnections: visibleConnectionCounts.at(-1) ?? 0,
    },
    recovery: {
      roomEntries: roomVisits.filter((visit) => visit.roomType === "recovery").length,
      consumed: matchesFrom(events, /^recovery-consumed-room-(\d+)$/).length,
      recoveredAmounts,
    },
    boss: {
      roomEntries: roomVisits.filter((visit) => visit.roomType === "boss").length,
      telegraphs: countExact(events, "boss-pattern-telegraph"),
      moves: countExact(events, "boss-moved"),
      phaseTwo: countExact(events, "boss-phase-two"),
      defeated: countExact(events, "boss-defeated"),
    },
    significantTimeline: events
      .map((event, eventIndex) => ({
        eventIndex,
        name: event.name,
        timestamp: event.timestamp,
        elapsedMilliseconds: event.timestamp - firstTimestamp,
      }))
      .filter((event) => isSignificantEvent(event.name)),
    interpretationLimits: [
      "This summary proves emitted runtime events, not player intent or enjoyment.",
      "Missing events can mean the route was not visited; they are not automatically defects.",
      "Use observation notes and the fixed-build session template for Keep/Change/Drop decisions.",
    ],
  };
}

function markdownValue(value) {
  if (Array.isArray(value)) return value.length > 0 ? value.join(", ") : "없음";
  return String(value).replaceAll("|", "\\|").replaceAll("\n", " ");
}

function yesNo(count) {
  return count > 0 ? `예 (${count})` : "아니오 (0)";
}

export function renderPlaytestLogMarkdown(summary) {
  requireCondition(
    summary?.schemaVersion === PLAYTEST_SUMMARY_SCHEMA,
    `summary schemaVersion must be ${PLAYTEST_SUMMARY_SCHEMA}.`,
  );

  const lines = [
    "# Bomb Swap 플레이테스트 로그 요약",
    "",
    `- 원본 schema: \`${summary.source.schemaVersion}\``,
    `- 생성 시각: ${summary.source.generatedAt}`,
    `- 빌드: ${markdownValue(summary.source.build.productName)} ${markdownValue(summary.source.build.productVersion)}`,
    `- 사건: ${summary.source.eventCount}개`,
    `- 기록 구간: ${(summary.source.durationMilliseconds / 1000).toFixed(3)}초`,
    "",
    "## 런 결과",
    "",
    `- 감지된 런 ${summary.runs.detectedRuns}회, 재시작 ${summary.runs.restarts}회`,
    `- 시작방 준비 marker ${summary.runs.startRoomReadyEvents}회`,
    `- 완료 ${summary.runs.completed}회, 실패 ${summary.runs.failed}회`,
    `- 실패 원인: ${markdownValue(summary.runs.failureCauses)}`,
    "",
    "## 자동 사건 요약",
    "",
    `- 방문 room marker: ${summary.rooms.visitCount}회 / 고유 방 ${summary.rooms.uniqueRoomCount}개 (${markdownValue(summary.rooms.uniqueRoomIds)})`,
    `- 선택한 폭탄 보상: ${markdownValue(summary.bombs.selectedRewards)}`,
    `- 비밀벽 공개: ${markdownValue(summary.secret.wallsRevealed)}`,
    `- 비밀방 입장: ${summary.secret.roomEntries}회 / cache 보상: ${markdownValue(summary.secret.rewardAmounts)}`,
    `- 미니맵 최대 공개: 방 ${summary.minimap.maxVisibleRooms}개 / 연결 ${summary.minimap.maxVisibleConnections}개`,
    `- Recovery 입장: ${summary.recovery.roomEntries}회 / 소비: ${summary.recovery.consumed}회 / 회복량: ${markdownValue(summary.recovery.recoveredAmounts)}`,
    `- 보스 입장: ${summary.boss.roomEntries}회 / Telegraph ${summary.boss.telegraphs}회 / 이동 ${summary.boss.moves}회`,
    `- 보스 2페이즈: ${yesNo(summary.boss.phaseTwo)} / 격파: ${yesNo(summary.boss.defeated)}`,
    "",
    "## 방문 방 순서",
    "",
    "| # | 방 | 종류 | 상태 | 경과 시간 |",
    "|---:|---:|---|---|---:|",
  ];

  for (let index = 0; index < summary.rooms.visits.length; index += 1) {
    const visit = summary.rooms.visits[index];
    lines.push(
      `| ${index + 1} | ${visit.roomId} | ${markdownValue(visit.roomType)} | ` +
      `${markdownValue(visit.state)} | ${(visit.elapsedMilliseconds / 1000).toFixed(3)}초 |`,
    );
  }
  if (summary.rooms.visits.length === 0) {
    lines.push("| - | - | - | - | - |");
  }

  lines.push(
    "",
    "## 해석 제한",
    "",
    ...summary.interpretationLimits.map((limit) => `- ${limit}`),
    "",
  );
  return `${lines.join("\n")}\n`;
}
