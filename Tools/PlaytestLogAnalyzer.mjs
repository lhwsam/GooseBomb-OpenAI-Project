export const PLAYTEST_LOG_SCHEMA = "bombswap/playtest-log@1";
export const PLAYTEST_SUMMARY_SCHEMA = "bombswap/playtest-summary@2";

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
  return /^(?:lobby-ready$|lobby-start-requested$|run-lobby-requested$|dungeon-room-ready-|dungeon-run-restarted$|secret-wall-revealed-|secret-reward-collected-|bomb-reward-selected-|player-health-recovered-|recovery-consumed-|boss-pattern-|boss-move-target-|boss-moved$|boss-phase-|boss-damaged(?:-|$)|boss-player-damaged-|boss-defeated$|player-died$|player-death-presentation-(?:started|completed)$|run-completed$|run-failed(?:-|$))/.test(name);
}

function countValues(items, selector, expectedValues = []) {
  const counts = Object.fromEntries(expectedValues.map((value) => [value, 0]));
  for (const item of items) {
    const value = selector(item);
    counts[value] = (counts[value] ?? 0) + 1;
  }
  return counts;
}

function bossEncounterStart(name) {
  const dungeonMatch = /^dungeon-room-ready-(\d+)-boss-active$/.exec(name);
  if (dungeonMatch) {
    return { kind: "dungeon", roomId: Number(dungeonMatch[1]) };
  }
  if (name === "room-ready-prototype-boss-arena") {
    return { kind: "standalone", roomId: null };
  }
  return null;
}

function buildBossEncounters(events, firstTimestamp) {
  const encounters = [];
  let current = null;

  const finish = (outcome, endEventIndex, endTimestamp) => {
    current.outcome = outcome;
    current.endEventIndex = outcome === "incomplete" ? null : endEventIndex;
    current.endTimestamp = outcome === "incomplete" ? null : endTimestamp;
    current.durationMilliseconds =
      outcome === "incomplete" ? null : endTimestamp - current.startTimestamp;
    current.observedEndEventIndex = endEventIndex;
    current.observedDurationMilliseconds = endTimestamp - current.startTimestamp;
    delete current.sawBossEvent;
    encounters.push(current);
    current = null;
  };

  for (let index = 0; index < events.length; index += 1) {
    const event = events[index];
    const start = bossEncounterStart(event.name);
    if (start) {
      const isDungeonStandaloneAlias =
        current !== null &&
        current.startKind === "dungeon" &&
        start.kind === "standalone" &&
        !current.sawBossEvent;
      if (isDungeonStandaloneAlias) continue;
      if (current !== null) {
        const previous = events[Math.max(0, index - 1)];
        finish("reentered", index - 1, previous.timestamp);
      }
      current = {
        encounterIndex: encounters.length,
        startKind: start.kind,
        roomId: start.roomId,
        startEventIndex: index,
        startTimestamp: event.timestamp,
        startElapsedMilliseconds: event.timestamp - firstTimestamp,
        sawBossEvent: false,
      };
      continue;
    }

    if (current === null) continue;
    if (event.name.startsWith("boss-")) current.sawBossEvent = true;
    if (event.name === "boss-defeated") {
      finish("defeated", index, event.timestamp);
    } else if (event.name === "player-died" || event.name === "run-failed") {
      finish("player-defeated", index, event.timestamp);
    } else if (event.name === "dungeon-run-restarted") {
      finish("restarted", index, event.timestamp);
    }
  }

  if (current !== null) {
    finish("incomplete", events.length - 1, events.at(-1).timestamp);
  }
  return encounters;
}

function encounterIndexForEvent(eventIndex, encounters) {
  for (const encounter of encounters) {
    if (
      eventIndex >= encounter.startEventIndex &&
      eventIndex <= encounter.observedEndEventIndex
    ) {
      return encounter.encounterIndex;
    }
  }
  return null;
}

function parseBossDamageDetails(events, firstTimestamp, encounters) {
  const details = [];
  const pattern = /^boss-damaged-phase-(one|two|last-stand)-state-(telegraph|execute|recovery|defeated)-source-(player-bomb|self-destruct)-definition-(.+)-health-(\d+)$/;
  for (let eventIndex = 0; eventIndex < events.length; eventIndex += 1) {
    const event = events[eventIndex];
    const match = pattern.exec(event.name);
    if (!match) continue;
    details.push({
      eventIndex,
      encounterIndex: encounterIndexForEvent(eventIndex, encounters),
      timestamp: event.timestamp,
      elapsedMilliseconds: event.timestamp - firstTimestamp,
      phase: match[1],
      state: match[2],
      source: match[3],
      definitionId: match[4],
      currentHealth: Number(match[5]),
    });
  }
  return details;
}

function parseBossPlayerDamageDetails(events, firstTimestamp, encounters) {
  const details = [];
  const pattern = /^boss-player-damaged-phase-(one|two|last-stand)-pattern-(.+)-health-(\d+)$/;
  for (let eventIndex = 0; eventIndex < events.length; eventIndex += 1) {
    const event = events[eventIndex];
    const match = pattern.exec(event.name);
    if (!match) continue;
    details.push({
      eventIndex,
      encounterIndex: encounterIndexForEvent(eventIndex, encounters),
      timestamp: event.timestamp,
      elapsedMilliseconds: event.timestamp - firstTimestamp,
      phase: match[1],
      pattern: match[2],
      currentHealth: Number(match[3]),
    });
  }
  return details;
}

function countDuringBossEncounters(events, encounters, eventName) {
  let count = 0;
  for (let eventIndex = 0; eventIndex < events.length; eventIndex += 1) {
    if (
      events[eventIndex].name === eventName &&
      encounterIndexForEvent(eventIndex, encounters) !== null
    ) {
      count += 1;
    }
  }
  return count;
}

function countPlayerBombHitAlternations(damageDetails) {
  let alternations = 0;
  let hasPrevious = false;
  let previousEncounter = null;
  let previousDefinition = null;
  for (const detail of damageDetails) {
    if (detail.source !== "player-bomb" || detail.definitionId === "unknown") {
      continue;
    }
    if (!hasPrevious || detail.encounterIndex !== previousEncounter) {
      hasPrevious = true;
      previousEncounter = detail.encounterIndex;
      previousDefinition = detail.definitionId;
      continue;
    }
    if (detail.definitionId !== previousDefinition) alternations += 1;
    previousDefinition = detail.definitionId;
  }
  return alternations;
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
  const bossEncounters = buildBossEncounters(events, firstTimestamp);
  const bossDamageDetails = parseBossDamageDetails(
    events,
    firstTimestamp,
    bossEncounters,
  );
  const bossPlayerDamageDetails = parseBossPlayerDamageDetails(
    events,
    firstTimestamp,
    bossEncounters,
  );
  const totalBossDamageEvents = countExact(events, "boss-damaged");
  const playerBombDamageDetails = bossDamageDetails.filter(
    (detail) => detail.source === "player-bomb",
  );

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
      detectedRuns: Math.max(
        1,
        countExact(events, "lobby-start-requested"),
      ) + countExact(events, "dungeon-run-restarted"),
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
      lastStand: countExact(events, "boss-phase-last-stand"),
      defeated: countExact(events, "boss-defeated"),
      overheats: countExact(events, "boss-pattern-overheat-recovery"),
      encounters: bossEncounters,
      damage: {
        totalEvents: totalBossDamageEvents,
        classifiedEvents: bossDamageDetails.length,
        unclassifiedEvents: Math.max(
          0,
          totalBossDamageEvents - bossDamageDetails.length,
        ),
        byPhase: countValues(
          bossDamageDetails,
          (detail) => detail.phase,
          ["one", "two", "last-stand"],
        ),
        byState: countValues(
          bossDamageDetails,
          (detail) => detail.state,
          ["telegraph", "execute", "recovery", "defeated"],
        ),
        bySource: countValues(
          bossDamageDetails,
          (detail) => detail.source,
          ["player-bomb", "self-destruct"],
        ),
        byDefinition: countValues(
          bossDamageDetails,
          (detail) => detail.definitionId,
        ),
        playerBombDefinitionSequence: playerBombDamageDetails.map(
          (detail) => detail.definitionId,
        ),
        playerBombDefinitionAlternations:
          countPlayerBombHitAlternations(bossDamageDetails),
        details: bossDamageDetails,
      },
      playerPatternDamage: {
        classifiedEvents: bossPlayerDamageDetails.length,
        byPhase: countValues(
          bossPlayerDamageDetails,
          (detail) => detail.phase,
          ["one", "two", "last-stand"],
        ),
        byPattern: countValues(
          bossPlayerDamageDetails,
          (detail) => detail.pattern,
        ),
        details: bossPlayerDamageDetails,
      },
      selfDestruct: {
        spawned: countExact(events, "boss-self-destruct-spawned"),
        detonatedDuringEncounter: countDuringBossEncounters(
          events,
          bossEncounters,
          "self-destruct-detonated",
        ),
        bossHits: bossDamageDetails.filter(
          (detail) => detail.source === "self-destruct",
        ).length,
      },
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
      "Bomb-definition alternations count successful boss hits, not swap intent or failed placement attempts.",
      "Parity safe-cell reuse, self-destruct lure intent, readability, and fairness still require observation notes.",
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

function countMapValue(counts) {
  const entries = Object.entries(counts);
  return entries.length > 0
    ? entries.map(([key, value]) => `${key} ${value}`).join(" / ")
    : "없음";
}

function bossEncounterDurationValue(encounters) {
  if (encounters.length === 0) return "없음";
  return encounters
    .map((encounter, index) => {
      const milliseconds =
        encounter.durationMilliseconds ?? encounter.observedDurationMilliseconds;
      const prefix = encounter.durationMilliseconds === null ? ">=" : "";
      return `#${index + 1} ${prefix}${(milliseconds / 1000).toFixed(3)}초 (${encounter.outcome})`;
    })
    .join(" / ");
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
    `- 보스 2페이즈: ${yesNo(summary.boss.phaseTwo)} / LastStand: ${yesNo(summary.boss.lastStand)} / 격파: ${yesNo(summary.boss.defeated)}`,
    "",
    "## 보스 프로토콜 자동 계측",
    "",
    `- 보스전 시간: ${bossEncounterDurationValue(summary.boss.encounters)}`,
    `- 과열 완료: ${summary.boss.overheats}회`,
    `- 보스 피해: ${summary.boss.damage.totalEvents}회 / 상세 분류 ${summary.boss.damage.classifiedEvents}회 / 미분류 ${summary.boss.damage.unclassifiedEvents}회`,
    `- 페이즈별 보스 피해: ${countMapValue(summary.boss.damage.byPhase)}`,
    `- 상태별 보스 피해: ${countMapValue(summary.boss.damage.byState)}`,
    `- 원천별 보스 피해: ${countMapValue(summary.boss.damage.bySource)}`,
    `- 폭탄 정의별 보스 피해: ${countMapValue(summary.boss.damage.byDefinition)}`,
    `- 플레이어 폭탄 적중 정의 순서: ${markdownValue(summary.boss.damage.playerBombDefinitionSequence)}`,
    `- 플레이어 폭탄 정의 교대 적중: ${summary.boss.damage.playerBombDefinitionAlternations}회`,
    `- 페이즈별 보스 패턴 피격: ${countMapValue(summary.boss.playerPatternDamage.byPhase)}`,
    `- 패턴별 플레이어 피격: ${countMapValue(summary.boss.playerPatternDamage.byPattern)}`,
    `- 보스 자폭병: 소환 ${summary.boss.selfDestruct.spawned}회 / 전투 중 폭발 ${summary.boss.selfDestruct.detonatedDuringEncounter}회 / 보스 적중 ${summary.boss.selfDestruct.bossHits}회`,
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
