import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { startStaticServer } from "./WebGLStaticServer.mjs";

function parseArguments(argv) {
  const values = {};
  for (let index = 0; index < argv.length; index += 2) {
    const key = argv[index];
    const value = argv[index + 1];
    if (!key?.startsWith("--") || value === undefined) {
      throw new Error(`Invalid argument pair near ${key ?? "<end>"}.`);
    }
    values[key.slice(2)] = value;
  }
  return values;
}

async function loadPlaywright() {
  for (const packageName of ["playwright", "playwright-core"]) {
    try {
      return await import(packageName);
    } catch {
      // Try the bundled Codex runtime next.
    }
  }

  const runtimeModules = process.env.CODEX_NODE_MODULES;
  if (runtimeModules) {
    for (const packageName of ["playwright", "playwright-core"]) {
      const modulePath = path.join(runtimeModules, packageName, "index.mjs");
      if (fs.existsSync(modulePath)) {
        return await import(pathToFileURL(modulePath).href);
      }
    }
  }
  throw new Error(
    "Playwright is unavailable. Install it or set CODEX_NODE_MODULES to a runtime containing it.",
  );
}

function resolveBrowserExecutable() {
  const explicitPath = process.env.BOMBSWAP_BROWSER_PATH;
  if (explicitPath) {
    if (!fs.existsSync(explicitPath)) {
      throw new Error(`BOMBSWAP_BROWSER_PATH does not exist: ${explicitPath}`);
    }
    return explicitPath;
  }

  const candidates = process.platform === "win32"
    ? [
        path.join(process.env["PROGRAMFILES(X86)"] ?? "", "Microsoft/Edge/Application/msedge.exe"),
        path.join(process.env.PROGRAMFILES ?? "", "Microsoft/Edge/Application/msedge.exe"),
        path.join(process.env.PROGRAMFILES ?? "", "Google/Chrome/Application/chrome.exe"),
        path.join(process.env.LOCALAPPDATA ?? "", "Google/Chrome/Application/chrome.exe"),
      ]
    : process.platform === "darwin"
      ? [
          "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
          "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
        ]
      : [
          "/usr/bin/microsoft-edge",
          "/usr/bin/microsoft-edge-stable",
          "/usr/bin/google-chrome",
          "/usr/bin/google-chrome-stable",
          "/usr/bin/chromium",
          "/usr/bin/chromium-browser",
        ];
  return candidates.find((candidate) => candidate && fs.existsSync(candidate));
}

async function eventCount(page, name) {
  return page.evaluate((expectedName) => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return 0;
    return events.filter((event) =>
      (typeof event === "string" ? event : event?.name) === expectedName).length;
  }, name);
}

async function waitForEvent(page, name, options = {}) {
  const { count = 1, timeout = 30_000 } = options;
  await page.waitForFunction(({ expectedName, expectedCount }) => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return false;
    return events.filter((event) =>
      (typeof event === "string" ? event : event?.name) === expectedName).length >= expectedCount;
  }, { expectedName: name, expectedCount: count }, { timeout });
}

async function tapUiNavigationKey(page, key) {
  await page.keyboard.down(key);
  await page.waitForTimeout(100);
  await page.keyboard.up(key);
  await page.waitForTimeout(100);
}

async function assertCanvasFitsViewport(page, label) {
  const layout = await page.evaluate(() => {
    const canvas = document.querySelector("#unity-canvas");
    if (!(canvas instanceof HTMLCanvasElement)) return null;
    const rect = canvas.getBoundingClientRect();
    return {
      viewportWidth: window.innerWidth,
      viewportHeight: window.innerHeight,
      documentWidth: document.documentElement.scrollWidth,
      documentHeight: document.documentElement.scrollHeight,
      referenceWidth: Number(canvas.getAttribute("width")),
      referenceHeight: Number(canvas.getAttribute("height")),
      left: rect.left,
      top: rect.top,
      right: rect.right,
      bottom: rect.bottom,
      width: rect.width,
      height: rect.height,
    };
  });
  if (!layout) {
    throw new Error(`${label}: Unity canvas was not found.`);
  }

  const epsilon = 1.5;
  if (layout.left < -epsilon ||
      layout.top < -epsilon ||
      layout.right > layout.viewportWidth + epsilon ||
      layout.bottom > layout.viewportHeight + epsilon) {
    throw new Error(
      `${label}: canvas ${layout.width.toFixed(1)}x${layout.height.toFixed(1)} ` +
      `at (${layout.left.toFixed(1)}, ${layout.top.toFixed(1)}) exceeds ` +
      `${layout.viewportWidth}x${layout.viewportHeight}.`,
    );
  }
  if (layout.documentWidth > layout.viewportWidth + epsilon ||
      layout.documentHeight > layout.viewportHeight + epsilon) {
    throw new Error(
      `${label}: document overflowed viewport ` +
      `${layout.documentWidth}x${layout.documentHeight} > ` +
      `${layout.viewportWidth}x${layout.viewportHeight}.`,
    );
  }
  if (layout.width > layout.referenceWidth + epsilon ||
      layout.height > layout.referenceHeight + epsilon) {
    throw new Error(
      `${label}: canvas enlarged beyond native ` +
      `${layout.referenceWidth}x${layout.referenceHeight}.`,
    );
  }

  const expectedAspect = layout.referenceWidth / layout.referenceHeight;
  const actualAspect = layout.width / layout.height;
  if (Math.abs(actualAspect - expectedAspect) > 0.01) {
    throw new Error(
      `${label}: canvas aspect ${actualAspect.toFixed(3)} did not preserve ` +
      `${expectedAspect.toFixed(3)}.`,
    );
  }

  return `${layout.viewportWidth}x${layout.viewportHeight} viewport, ` +
    `${layout.width.toFixed(0)}x${layout.height.toFixed(0)} canvas, no overflow`;
}

async function verifyPlaytestLogExport(page, outputPath, expectedEvents) {
  if (!Array.isArray(expectedEvents) || expectedEvents.length === 0) {
    throw new Error("Playtest log export requires a non-empty harness event snapshot.");
  }

  const exportButton = page.locator("#unity-playtest-log-button");
  await exportButton.waitFor({ state: "visible", timeout: 5_000 });
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  const [download] = await Promise.all([
    page.waitForEvent("download", { timeout: 10_000 }),
    exportButton.click(),
  ]);
  await download.saveAs(outputPath);
  const downloadFailure = await download.failure();
  if (downloadFailure) {
    throw new Error(`Playtest log download failed: ${downloadFailure}`);
  }

  const payload = JSON.parse(fs.readFileSync(outputPath, "utf8"));
  if (payload.schemaVersion !== "bombswap/playtest-log@1") {
    throw new Error(
      `Unexpected playtest log schema: ${String(payload.schemaVersion)}`,
    );
  }
  if (!Number.isFinite(Date.parse(payload.generatedAt))) {
    throw new Error("Playtest log generatedAt is not a valid ISO timestamp.");
  }
  if (typeof payload.build?.productName !== "string" ||
      typeof payload.build?.productVersion !== "string") {
    throw new Error("Playtest log is missing its product build identity.");
  }
  if (payload.eventCount !== expectedEvents.length ||
      !Array.isArray(payload.events) ||
      JSON.stringify(payload.events) !== JSON.stringify(expectedEvents)) {
    throw new Error(
      `Downloaded playtest events did not match the live snapshot ` +
      `(${payload.eventCount ?? "missing"}/${expectedEvents.length}).`,
    );
  }

  return {
    schemaVersion: payload.schemaVersion,
    eventCount: payload.eventCount,
    suggestedFilename: download.suggestedFilename(),
    outputPath,
  };
}

async function moveSteps(page, key, direction, count) {
  const eventName = `move-step-direction-${direction}`;
  const initialCount = await eventCount(page, eventName);
  await page.keyboard.down(key);
  try {
    await waitForEvent(page, eventName, {
      count: initialCount + count,
      timeout: Math.max(5_000, count * 2_000),
    });
  } finally {
    await page.keyboard.up(key);
  }
}

async function getLastPlayerCell(page) {
  return page.evaluate(() => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return null;
    for (let index = events.length - 1; index >= 0; index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      const match = /^player-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
      if (match) return { x: Number(match[1]), z: Number(match[2]) };
    }
    return null;
  });
}

async function waitForThrowerRoomEntryCells(page, startIndex, timeout = 5_000) {
  await page.waitForFunction((minimumIndex) => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return false;
    let roomReadyIndex = -1;
    for (let index = minimumIndex; index < events.length; index++) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (name === "room-ready-prototype-combat-thrower") {
        roomReadyIndex = index;
        break;
      }
    }
    if (roomReadyIndex < 0) return false;

    let hasPlayer = false;
    let hasThrower = false;
    for (let index = roomReadyIndex + 1; index < events.length; index++) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      hasPlayer ||= /^player-cell-x-(-?\d+)-z-(-?\d+)$/.test(name ?? "");
      hasThrower ||= /^thrower-cell-x-(-?\d+)-z-(-?\d+)$/.test(name ?? "");
      if (hasPlayer && hasThrower) return true;
    }
    return false;
  }, startIndex, { timeout });

  return page.evaluate((minimumIndex) => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    let roomReadyIndex = -1;
    for (let index = minimumIndex; index < events.length; index++) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (name === "room-ready-prototype-combat-thrower") {
        roomReadyIndex = index;
        break;
      }
    }

    let player = null;
    let thrower = null;
    for (let index = roomReadyIndex + 1;
      index < events.length && (!player || !thrower);
      index++) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (!player) {
        const match = /^player-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
        if (match) player = { x: Number(match[1]), z: Number(match[2]) };
      }
      if (!thrower) {
        const match = /^thrower-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
        if (match) thrower = { x: Number(match[1]), z: Number(match[2]) };
      }
    }
    return { player, thrower };
  }, startIndex);
}

async function getLastChaserCell(page) {
  return page.evaluate(() => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return null;
    for (let index = events.length - 1; index >= 0; index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      const match = /^chaser-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
      if (match) return { x: Number(match[1]), z: Number(match[2]) };
    }
    return null;
  });
}

async function getLastSelfDestructCell(page) {
  return page.evaluate(() => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return null;
    for (let index = events.length - 1; index >= 0; index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      const match = /^self-destruct-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
      if (match) return { x: Number(match[1]), z: Number(match[2]) };
    }
    return null;
  });
}

async function waitForSelfDestructAtCell(page, expectedX, expectedZ, timeout = 15_000) {
  await page.waitForFunction(({ x, z }) => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return false;
    for (let index = events.length - 1; index >= 0; index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (name === `self-destruct-cell-x-${x}-z-${z}`) return true;
    }
    return false;
  }, { x: expectedX, z: expectedZ }, { timeout });

  const observed = await getLastSelfDestructCell(page);
  if (!observed || observed.x !== expectedX || observed.z !== expectedZ) {
    throw new Error(
      `Expected self-destruct cell (${expectedX}, ${expectedZ}), observed ${JSON.stringify(observed)}.`,
    );
  }
}

async function getLastPlayerHealth(page) {
  return page.evaluate(() => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return null;
    for (let index = events.length - 1; index >= 0; index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      const match = /^player-health-current-(\d+)$/.exec(name ?? "");
      if (match) return Number(match[1]);
    }
    return null;
  });
}

async function verifyFocusLossClearsHeldInput(page) {
  const eastCommandsBefore = await eventCount(page, "move-direction-east");
  const eastMotionBefore = await eventCount(page, "move-motion-direction-east");
  const noneCommandsBefore = await eventCount(page, "move-direction-none");
  let focusRestored = false;
  await page.keyboard.down("ArrowRight");
  try {
    await waitForEvent(page, "move-direction-east", {
      count: eastCommandsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "move-motion-direction-east", {
      count: eastMotionBefore + 1,
      timeout: 5_000,
    });

    await page.evaluate(() => globalThis.dispatchEvent(new Event("blur")));
    await waitForEvent(page, "move-direction-none", {
      count: noneCommandsBefore + 1,
      timeout: 5_000,
    });
    await page.waitForTimeout(100);
    const settledMotion = await eventCount(page, "move-motion-direction-east");
    const settledCell = await getLastPlayerCell(page);
    await page.waitForTimeout(300);
    const blurredMotion = await eventCount(page, "move-motion-direction-east");
    const blurredCell = await getLastPlayerCell(page);
    if (blurredMotion !== settledMotion ||
        JSON.stringify(blurredCell) !== JSON.stringify(settledCell)) {
      throw new Error(
        `Browser blur left movement active: cell ${JSON.stringify(settledCell)} -> ` +
        `${JSON.stringify(blurredCell)}, east motion ${settledMotion} -> ${blurredMotion}.`,
      );
    }

    await page.evaluate(() => globalThis.dispatchEvent(new Event("focus")));
    focusRestored = true;
    await page.waitForTimeout(300);
    const restoredMotion = await eventCount(page, "move-motion-direction-east");
    const restoredCell = await getLastPlayerCell(page);
    if (restoredMotion !== settledMotion ||
        JSON.stringify(restoredCell) !== JSON.stringify(settledCell)) {
      throw new Error(
        `Browser focus restored a lost key-up: cell ${JSON.stringify(settledCell)} -> ` +
        `${JSON.stringify(restoredCell)}, east motion ${settledMotion} -> ${restoredMotion}.`,
      );
    }

    return {
      cell: settledCell,
      eastMotionCount: settledMotion,
      releasedCommand: "move-direction-none",
      lifecycleEvents: ["blur", "focus"],
    };
  } finally {
    if (!focusRestored) {
      await page.evaluate(() => globalThis.dispatchEvent(new Event("focus")));
    }
    await page.keyboard.up("ArrowRight");
    await page.locator("canvas").first().click({ position: { x: 20, y: 20 } });
  }
}

async function waitForChaserAtDistance(page, expectedDistance, timeout = 15_000) {
  await page.waitForFunction((distance) => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) return false;
    let roomStart = 0;
    for (let index = events.length - 1; index >= 0; index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (/^dungeon-room-ready-\d+-combat-active$/.test(name ?? "")) {
        roomStart = index;
        break;
      }
    }
    let player = null;
    let chaser = null;
    for (let index = events.length - 1; index >= roomStart && (!player || !chaser); index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (!player) {
        const match = /^player-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
        if (match) player = { x: Number(match[1]), z: Number(match[2]) };
      }
      if (!chaser) {
        const match = /^chaser-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
        if (match) chaser = { x: Number(match[1]), z: Number(match[2]) };
      }
    }
    return player && chaser &&
      Math.abs(player.x - chaser.x) + Math.abs(player.z - chaser.z) === distance;
  }, expectedDistance, { timeout });

  return page.evaluate(() => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    let roomStart = 0;
    for (let index = events.length - 1; index >= 0; index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (/^dungeon-room-ready-\d+-combat-active$/.test(name ?? "")) {
        roomStart = index;
        break;
      }
    }
    let player = null;
    let chaser = null;
    for (let index = events.length - 1; index >= roomStart && (!player || !chaser); index--) {
      const name = typeof events[index] === "string" ? events[index] : events[index]?.name;
      if (!player) {
        const match = /^player-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
        if (match) player = { x: Number(match[1]), z: Number(match[2]) };
      }
      if (!chaser) {
        const match = /^chaser-cell-x-(-?\d+)-z-(-?\d+)$/.exec(name ?? "");
        if (match) chaser = { x: Number(match[1]), z: Number(match[2]) };
      }
    }
    return { player, chaser };
  });
}

async function waitForChaserAdjacent(page, timeout = 15_000) {
  return waitForChaserAtDistance(page, 1, timeout);
}

async function moveToCell(page, targetX, targetZ, order = "xz") {
  const axes = order === "zx" ? ["z", "x"] : ["x", "z"];
  for (const axis of axes) {
    const current = await getLastPlayerCell(page);
    if (!current) throw new Error("The gameplay probe did not report a player cell.");
    const target = axis === "x" ? targetX : targetZ;
    const delta = target - current[axis];
    if (delta === 0) continue;
    const positive = delta > 0;
    const key = axis === "x"
      ? positive ? "ArrowRight" : "ArrowLeft"
      : positive ? "ArrowUp" : "ArrowDown";
    const direction = axis === "x"
      ? positive ? "east" : "west"
      : positive ? "north" : "south";
    await moveSteps(page, key, direction, Math.abs(delta));
  }

  const finalCell = await getLastPlayerCell(page);
  if (!finalCell || finalCell.x !== targetX || finalCell.z !== targetZ) {
    throw new Error(
      `Expected player cell (${targetX}, ${targetZ}), observed ${JSON.stringify(finalCell)}.`,
    );
  }
}

async function triggerBoundaryTransition(page, key, expectedRoomEvent, expectedCount = 1) {
  const transitionsBefore = await eventCount(page, "dungeon-transition-started");
  const commitsBefore = await eventCount(page, "dungeon-room-committed");
  const probesBefore = await eventCount(page, "probe-ready");
  await page.keyboard.down(key);
  try {
    await waitForEvent(page, "dungeon-transition-started", {
      count: transitionsBefore + 1,
      timeout: 30_000,
    });
    await waitForEvent(page, expectedRoomEvent, {
      count: expectedCount,
      timeout: 60_000,
    });
    await waitForEvent(page, "dungeon-room-committed", {
      count: commitsBefore + 1,
      timeout: 60_000,
    });
    await waitForEvent(page, "probe-ready", {
      count: probesBefore + 1,
      timeout: 60_000,
    });
  } finally {
    await page.keyboard.up(key);
  }
}

async function verifyRapidCardinalTurns(page) {
  const startIndex = await page.evaluate(() =>
    Array.isArray(globalThis.__BOMBSWAP_HARNESS_EVENTS__)
      ? globalThis.__BOMBSWAP_HARNESS_EVENTS__.length
      : 0);
  const rapidDirections = [
    ["ArrowLeft", "move-motion-direction-west"],
    ["ArrowUp", "move-motion-direction-north"],
    ["ArrowLeft", "move-motion-direction-west"],
    ["ArrowUp", "move-motion-direction-north"],
    ["ArrowLeft", "move-motion-direction-west"],
    ["ArrowUp", "move-motion-direction-north"],
  ];
  const expectedMotionEvents = [];
  try {
    for (const [key, motionEvent] of rapidDirections) {
      expectedMotionEvents.push(motionEvent);
      await page.keyboard.down(key);
      await page.keyboard.up(key);
      await page.waitForFunction(({ eventStartIndex, expected }) => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        if (!Array.isArray(events)) return false;
        const names = events
          .slice(eventStartIndex)
          .map((event) => typeof event === "string" ? event : event?.name);
        let expectedIndex = 0;
        for (const name of names) {
          if (name === expected[expectedIndex]) expectedIndex++;
          if (expectedIndex === expected.length) return true;
        }
        return false;
      }, { eventStartIndex: startIndex, expected: expectedMotionEvents }, { timeout: 2_000 });
      const motionCountAfterTap = await eventCount(page, motionEvent);
      await page.waitForTimeout(50);
      const motionCountAfterStop = await eventCount(page, motionEvent);
      if (motionCountAfterStop !== motionCountAfterTap) {
        throw new Error(
          `Released ${key} kept moving: ${motionCountAfterTap} -> ${motionCountAfterStop}.`,
        );
      }
    }
  } finally {
    await page.keyboard.up("ArrowLeft");
    await page.keyboard.up("ArrowUp");
  }
}

async function verifyHeldDiagonalLatestAxis(page) {
  const northEvent = "move-motion-direction-north";
  const westEvent = "move-motion-direction-west";
  const initialNorthCount = await eventCount(page, northEvent);
  const initialWestCount = await eventCount(page, westEvent);
  await page.keyboard.down("ArrowUp");
  try {
    await waitForEvent(page, northEvent, {
      count: initialNorthCount + 1,
      timeout: 2_000,
    });
    await page.keyboard.down("ArrowLeft");
    await waitForEvent(page, westEvent, {
      count: initialWestCount + 1,
      timeout: 2_000,
    });
    const stableStartIndex = await page.evaluate(() =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__.length);
    await page.waitForTimeout(150);
    const heldMotion = await page.evaluate((eventStartIndex) =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__
        .slice(eventStartIndex)
        .map((event) => typeof event === "string" ? event : event?.name)
        .filter((name) => name?.startsWith("move-motion-direction-")), stableStartIndex);
    if (heldMotion.some((name) => name !== westEvent)) {
      throw new Error(
        `Held diagonal changed away from the latest west axis: ${heldMotion.join(", ")}`,
      );
    }

    const northBeforeRelease = await eventCount(page, northEvent);
    await page.keyboard.up("ArrowLeft");
    await waitForEvent(page, northEvent, {
      count: northBeforeRelease + 1,
      timeout: 2_000,
    });
  } finally {
    await page.keyboard.up("ArrowLeft");
    await page.keyboard.up("ArrowUp");
  }
}

async function main() {
  const args = parseArguments(process.argv.slice(2));
  if (!args.buildPath || !args.reportPath) {
    throw new Error("--buildPath and --reportPath are required.");
  }

  const buildPath = path.resolve(args.buildPath);
  if (!fs.existsSync(path.join(buildPath, "index.html"))) {
    throw new Error(`WebGL index.html was not found under ${buildPath}.`);
  }
  const reportPath = path.resolve(args.reportPath);
  const screenshotPath = path.resolve(
    args.screenshotPath ?? path.join(path.dirname(reportPath), "webgl-dungeon.png"),
  );
  const lobbyScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-lobby.png`,
  );
  const settingsScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-settings.png`,
  );
  const bossTelegraphScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-boss-telegraph.png`,
  );
  const gatesRoomScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-gates-room.png`,
  );
  const recoveryRoomScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-recovery-room.png`,
  );
  const secretWallScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-secret-wall.png`,
  );
  const secretRoomScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-secret-room.png`,
  );
  const pauseScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-paused.png`,
  );
  const runFailureScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-run-failed.png`,
  );
  const playtestLogPath = path.resolve(
    args.playtestLogPath ?? path.join(path.dirname(reportPath), "playtest-events.json"),
  );
  const { chromium } = await loadPlaywright();
  const { server, url } = await startStaticServer(buildPath);
  const consoleErrors = [];
  const pageErrors = [];
  const checks = [];
  let browser;
  let page;

  try {
    const executablePath = resolveBrowserExecutable();
    browser = await chromium.launch({
      headless: true,
      ...(executablePath ? { executablePath } : {}),
    });
    page = await browser.newPage({
      viewport: { width: 1280, height: 720 },
      acceptDownloads: true,
    });
    page.on("console", (message) => {
      if (message.type() === "error") consoleErrors.push(message.text());
    });
    page.on("pageerror", (error) => pageErrors.push(String(error)));

    await page.goto(url, { waitUntil: "domcontentloaded", timeout: 120_000 });
    await page.waitForFunction(() => {
      const canvas = document.querySelector("canvas");
      return canvas && canvas.width > 0 && canvas.height > 0;
    }, undefined, { timeout: 120_000 });
    checks.push({ name: "load", status: "passed" });

    const initialDesktopFit = await assertCanvasFitsViewport(
      page,
      "Initial desktop viewport",
    );
    await page.setViewportSize({ width: 640, height: 720 });
    await page.waitForTimeout(250);
    const initialNarrowFit = await assertCanvasFitsViewport(
      page,
      "Initial narrow viewport",
    );
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.waitForTimeout(250);
    checks.push({
      name: "responsive-canvas-layout",
      status: "passed",
      detail: `${initialDesktopFit}; ${initialNarrowFit}`,
    });

    const canvas = page.locator("canvas").first();
    await canvas.click({ position: { x: 20, y: 20 } });
    const focusedTag = await page.evaluate(() => document.activeElement?.tagName ?? "");
    checks.push({
      name: "canvas-focus",
      status: focusedTag === "CANVAS" ? "passed" : "failed",
      detail: focusedTag,
    });

    await waitForEvent(page, "lobby-ready", { timeout: 120_000 });
    fs.mkdirSync(path.dirname(lobbyScreenshotPath), { recursive: true });
    await page.screenshot({ path: lobbyScreenshotPath });
    await tapUiNavigationKey(page, "ArrowDown");
    await page.keyboard.press("Enter");
    await waitForEvent(page, "lobby-settings-opened", { timeout: 5_000 });
    await waitForEvent(page, "settings-opened", { timeout: 5_000 });
    await page.screenshot({ path: settingsScreenshotPath });
    await tapUiNavigationKey(page, "ArrowRight");
    await page.keyboard.press("Enter");
    await waitForEvent(page, "settings-audio-page-opened", { timeout: 5_000 });
    await tapUiNavigationKey(page, "ArrowDown");
    await tapUiNavigationKey(page, "ArrowLeft");
    await waitForEvent(page, "settings-bgm-volume-changed", {
      timeout: 5_000,
    });
    const settingsCanvasBox = await canvas.boundingBox();
    if (!settingsCanvasBox) {
      throw new Error("Settings smoke could not resolve the Unity canvas bounds.");
    }
    await canvas.click({
      position: {
        x: settingsCanvasBox.width * 0.5,
        y: settingsCanvasBox.height * 0.77,
      },
    });
    await waitForEvent(page, "settings-closed", { timeout: 5_000 });
    checks.push({
      name: "lobby-settings",
      status: "passed",
      detail: "The keyboard-only settings panel opened, switched to audio/screen controls, changed BGM volume, and closed without displaying gamepad bindings.",
    });
    await tapUiNavigationKey(page, "ArrowUp");
    await page.keyboard.press("Enter");
    await waitForEvent(page, "lobby-start-requested", { timeout: 5_000 });
    checks.push({
      name: "lobby-start",
      status: "passed",
      detail: "The DungGeunMo lobby loaded first and started a clean run through keyboard UI submit.",
    });

    await waitForEvent(page, "probe-ready", { timeout: 120_000 });
    await waitForEvent(page, "room-ready-prototype-combat-loop", {
      timeout: 120_000,
    });
    await waitForEvent(page, "dungeon-room-ready-1-start-safe", {
      timeout: 120_000,
    });
    await waitForEvent(page, "player-health-current-5", {
      timeout: 120_000,
    });
    await waitForEvent(page, "minimap-current-room-1", { timeout: 120_000 });
    await waitForEvent(page, "minimap-visible-rooms-2", { timeout: 120_000 });
    await waitForEvent(page, "minimap-visible-connections-1", {
      timeout: 120_000,
    });
    checks.push({
      name: "dungeon-start-ready",
      status: "passed",
      detail: "The safe Start placeholder initialized with the reusable loop shell.",
    });
    checks.push({
      name: "minimap-initial-knowledge",
      status: "passed",
      detail: "The start-room minimap exposed only the current room, its discovered neighbor, and their confirmed connection.",
    });

    const focusRecovery = await verifyFocusLossClearsHeldInput(page);
    checks.push({
      name: "focus-loss-clears-held-input",
      status: "passed",
      detail: focusRecovery,
    });

    const pauseCellBefore = await getLastPlayerCell(page);
    const pauseMotionBefore = await eventCount(
      page,
      "move-motion-direction-north",
    );
    const pauseBombPlacementsBefore = await eventCount(
      page,
      "place-bomb-definition-prototype-cross",
    );
    await page.keyboard.press("Escape");
    await waitForEvent(page, "pause-entered", { timeout: 5_000 });
    fs.mkdirSync(path.dirname(pauseScreenshotPath), { recursive: true });
    await page.screenshot({ path: pauseScreenshotPath });
    const pauseSettingsOpenedBefore = await eventCount(page, "settings-opened");
    const pauseSettingsClosedBefore = await eventCount(page, "settings-closed");
    const pauseResumedBeforeSettings = await eventCount(page, "pause-resumed");
    const pauseCanvasBox = await canvas.boundingBox();
    if (!pauseCanvasBox) {
      throw new Error("Pause settings smoke could not resolve the Unity canvas bounds.");
    }
    await canvas.click({
      position: {
        x: pauseCanvasBox.width * 0.5,
        y: pauseCanvasBox.height * 0.62,
      },
    });
    await waitForEvent(page, "settings-opened", {
      count: pauseSettingsOpenedBefore + 1,
      timeout: 5_000,
    });
    await page.keyboard.press("Escape");
    await waitForEvent(page, "settings-closed", {
      count: pauseSettingsClosedBefore + 1,
      timeout: 5_000,
    });
    if (await eventCount(page, "pause-resumed") !== pauseResumedBeforeSettings) {
      throw new Error("Closing pause settings also resumed gameplay.");
    }
    checks.push({
      name: "pause-settings",
      status: "passed",
      detail: "The shared settings panel opened from PAUSED and Escape returned to the pause menu without resuming gameplay.",
    });
    await page.keyboard.down("ArrowUp");
    try {
      await page.keyboard.press("KeyZ");
      await page.waitForTimeout(400);
    } finally {
      await page.keyboard.up("ArrowUp");
    }
    const pauseCellAfter = await getLastPlayerCell(page);
    const pauseMotionAfter = await eventCount(
      page,
      "move-motion-direction-north",
    );
    const pauseBombPlacementsAfter = await eventCount(
      page,
      "place-bomb-definition-prototype-cross",
    );
    if (JSON.stringify(pauseCellAfter) !== JSON.stringify(pauseCellBefore) ||
        pauseMotionAfter !== pauseMotionBefore ||
        pauseBombPlacementsAfter !== pauseBombPlacementsBefore) {
      throw new Error(
        `Pause did not block gameplay: cell ${JSON.stringify(pauseCellBefore)} -> ` +
        `${JSON.stringify(pauseCellAfter)}, motion ${pauseMotionBefore} -> ` +
        `${pauseMotionAfter}, bombs ${pauseBombPlacementsBefore} -> ` +
        `${pauseBombPlacementsAfter}.`,
      );
    }
    await page.keyboard.press("Escape");
    await waitForEvent(page, "pause-resumed", { timeout: 5_000 });
    await waitForEvent(page, "pause-resume", { timeout: 5_000 });
    checks.push({
      name: "pause-freezes-gameplay",
      status: "passed",
      detail: "The PAUSED state blocked movement and bomb placement, then resumed from Escape without advancing the player cell.",
    });

    await verifyHeldDiagonalLatestAxis(page);
    checks.push({
      name: "held-diagonal-latest-axis",
      status: "passed",
      detail: "North changed to west immediately, stayed west while both keys were held, then resumed north on west release.",
    });

    await verifyRapidCardinalTurns(page);
    checks.push({
      name: "frame-responsive-cardinal-turns",
      status: "passed",
      detail: "Six alternating west/north press-release taps each produced motion for one frame and then stopped.",
    });
    await moveToCell(page, 0, 0);

    const startHealthFourBefore = await eventCount(
      page,
      "player-health-current-4",
    );
    const startBombPlacementsBefore = await eventCount(
      page,
      "place-bomb-definition-prototype-cross",
    );
    const startExplosionsBefore = await eventCount(page, "bomb-exploded");
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      count: startBombPlacementsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "bomb-exploded", {
      count: startExplosionsBefore + 1,
      timeout: 15_000,
    });
    await waitForEvent(page, "player-health-current-4", {
      count: startHealthFourBefore + 1,
      timeout: 5_000,
    });
    checks.push({
      name: "run-health-damage-baseline",
      status: "passed",
      detail: "One self explosion reduced the first run from 5 health to 4 before leaving the Start room.",
    });

    await moveSteps(page, "ArrowUp", "north", 1);
    await moveSteps(page, "ArrowLeft", "west", 2);
    await moveSteps(page, "ArrowUp", "north", 1);
    await moveSteps(page, "ArrowLeft", "west", 3);
    await moveSteps(page, "ArrowDown", "south", 2);
    checks.push({
      name: "safe-room-route",
      status: "passed",
      detail: "Moved around the authored blockers to the deterministic west Start exit.",
    });

    const healthProbeThrowerTelegraphBefore = await eventCount(
      page,
      "thrower-telegraph",
    );
    const healthProbeThrowerLaunchBefore = await eventCount(
      page,
      "thrower-bomb-launched",
    );
    const healthProbeEntryEventStart = await page.evaluate(() =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__.length);
    await triggerBoundaryTransition(
      page,
      "ArrowLeft",
      "dungeon-room-ready-2-combat-active",
    );
    await waitForEvent(page, "room-ready-prototype-combat-thrower", {
      timeout: 60_000,
    });
    await waitForEvent(page, "probe-ready", { count: 2, timeout: 60_000 });
    await waitForEvent(page, "player-health-current-4", {
      count: startHealthFourBefore + 2,
      timeout: 60_000,
    });
    checks.push({
      name: "graph-scene-transition",
      status: "passed",
      detail: "The seed-0 Start exit loaded and committed the assigned thrower combat scene.",
    });
    checks.push({
      name: "run-health-room-persistence",
      status: "passed",
      detail: "The next room initialized at 4 health instead of healing on scene transition.",
    });

    const throwerEntryCells = await waitForThrowerRoomEntryCells(
      page,
      healthProbeEntryEventStart,
    );
    const throwerEntryDistance = throwerEntryCells.player && throwerEntryCells.thrower
      ? Math.abs(throwerEntryCells.player.x - throwerEntryCells.thrower.x) +
        Math.abs(throwerEntryCells.player.z - throwerEntryCells.thrower.z)
      : -1;
    if (throwerEntryCells.player?.x !== 4 ||
        throwerEntryCells.player?.z !== 0 ||
        throwerEntryCells.thrower?.x !== 2 ||
        throwerEntryCells.thrower?.z !== -3 ||
        throwerEntryDistance < 4) {
      throw new Error(
        `Expected the seed-0 thrower entry at player (4, 0), thrower (2, -3) ` +
        `with at least four cells of clearance, got ` +
        `${JSON.stringify(throwerEntryCells)} at distance ${throwerEntryDistance}.`,
      );
    }
    checks.push({
      name: "thrower-entry-clearance",
      status: "passed",
      detail: `The rotated thrower starts ${throwerEntryDistance} Manhattan cells from the east entry instead of adjacent to it.`,
    });

    const healthProbeRestartRequestsBefore = await eventCount(
      page,
      "run-restart-requested",
    );
    const healthProbeRestartsBefore = await eventCount(
      page,
      "dungeon-run-restarted",
    );
    const healthProbeStartReadyBefore = await eventCount(
      page,
      "dungeon-room-ready-1-start-safe",
    );
    const healthProbeFullHealthBefore = await eventCount(
      page,
      "player-health-current-5",
    );
    await waitForEvent(page, "thrower-telegraph", {
      count: healthProbeThrowerTelegraphBefore + 1,
      timeout: 10_000,
    });
    await waitForEvent(page, "thrower-bomb-launched", {
      count: healthProbeThrowerLaunchBefore + 3,
      timeout: 10_000,
    });
    const firstEntryThrowerEvents = await page.evaluate((startIndex) =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__
        .slice(startIndex)
        .map((event) => typeof event === "string" ? event : event?.name)
        .filter((name) => /^thrower-cell-x-(-?\d+)-z-(-?\d+)$/.test(name ?? "") ||
          name === "thrower-track-moved" ||
          name === "thrower-telegraph" ||
          name === "thrower-bomb-launched"), healthProbeEntryEventStart);
    const firstEntryTelegraphIndex = firstEntryThrowerEvents.indexOf(
      "thrower-telegraph",
    );
    const entryTrackCellsBeforeTelegraph = firstEntryThrowerEvents
      .slice(0, firstEntryTelegraphIndex)
      .filter((name) => /^thrower-cell-x-(-?\d+)-z-(-?\d+)$/.test(name ?? ""));
    const uniqueEntryTrackCells = new Set(entryTrackCellsBeforeTelegraph);
    const entryStagingIndex = firstEntryThrowerEvents.indexOf(
      "thrower-cell-x-2-z--3",
    );
    const firstFiringAnchorIndex = firstEntryThrowerEvents.indexOf(
      "thrower-cell-x-3-z-0",
    );
    const firstTrackMarkerIndex = firstEntryThrowerEvents.indexOf(
      "thrower-track-moved",
    );
    if (entryStagingIndex < 0 ||
        firstTrackMarkerIndex <= entryStagingIndex ||
        firstFiringAnchorIndex <= firstTrackMarkerIndex ||
        firstFiringAnchorIndex >= firstEntryTelegraphIndex ||
        firstEntryTelegraphIndex < 0 ||
        uniqueEntryTrackCells.size < 5 ||
        firstEntryThrowerEvents.filter((name) => name === "thrower-bomb-launched").length < 3) {
      throw new Error(
        `Expected four staging Track cell transitions before the integrated thrower room ` +
        `Telegraph and three launches, got ${firstEntryThrowerEvents.join(", ")}.`,
      );
    }
    checks.push({
      name: "thrower-main-dungeon-entry",
      status: "passed",
      detail: `The main dungeon thrower crossed ${uniqueEntryTrackCells.size - 1} staging-to-anchor cells before its first Telegraph and authored three-bomb volley.`,
    });
    await waitForEvent(page, "player-died", { timeout: 15_000 });
    await waitForEvent(page, "run-failed", { timeout: 5_000 });
    await page.keyboard.press("KeyR");
    await waitForEvent(page, "run-restart-requested", {
      count: healthProbeRestartRequestsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "dungeon-run-restarted", {
      count: healthProbeRestartsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "dungeon-room-ready-1-start-safe", {
      count: healthProbeStartReadyBefore + 1,
      timeout: 20_000,
    });
    await waitForEvent(page, "player-health-current-5", {
      count: healthProbeFullHealthBefore + 1,
      timeout: 5_000,
    });
    checks.push({
      name: "health-probe-new-run-reset",
      status: "passed",
      detail: "After the persisted-health probe run failed, R created a fresh run at full health without reloading the page.",
    });

    await moveSteps(page, "ArrowUp", "north", 1);
    await moveSteps(page, "ArrowLeft", "west", 2);
    await moveSteps(page, "ArrowUp", "north", 1);
    await moveSteps(page, "ArrowLeft", "west", 3);
    await moveSteps(page, "ArrowDown", "south", 2);

    const fullRunCombatReadyBefore = await eventCount(
      page,
      "dungeon-room-ready-2-combat-active",
    );
    const fullRunRoomReadyBefore = await eventCount(
      page,
      "room-ready-prototype-combat-thrower",
    );
    const fullRunProbeReadyBefore = await eventCount(page, "probe-ready");
    const fullRunHealthReadyBefore = await eventCount(
      page,
      "player-health-current-5",
    );
    await triggerBoundaryTransition(
      page,
      "ArrowLeft",
      "dungeon-room-ready-2-combat-active",
      fullRunCombatReadyBefore + 1,
    );
    await waitForEvent(page, "room-ready-prototype-combat-thrower", {
      count: fullRunRoomReadyBefore + 1,
      timeout: 60_000,
    });
    await waitForEvent(page, "probe-ready", {
      count: fullRunProbeReadyBefore + 1,
      timeout: 60_000,
    });
    await waitForEvent(page, "player-health-current-5", {
      count: fullRunHealthReadyBefore + 1,
      timeout: 60_000,
    });

    let combatPlacementsBefore = await eventCount(
      page,
      "place-bomb-definition-prototype-cross",
    );
    let combatExplosionsBefore = await eventCount(page, "bomb-exploded");
    const combatRoomClearsBefore = await eventCount(page, "room-cleared");
    const combatEnemyDeathsBefore = await eventCount(page, "enemy-died");
    const combatThrowerDeathsBefore = await eventCount(page, "thrower-died");
    const combatThrowerTelegraphsBefore = await eventCount(
      page,
      "thrower-telegraph",
    );
    await waitForChaserAdjacent(page);
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      count: combatPlacementsBefore + 1,
      timeout: 5_000,
    });
    await moveToCell(page, 4, -2);
    await waitForEvent(page, "bomb-exploded", {
      count: combatExplosionsBefore + 1,
      timeout: 15_000,
    });
    await waitForEvent(page, "enemy-died", {
      count: combatEnemyDeathsBefore + 1,
      timeout: 5_000,
    });
    const throwerDiedWithLure = await eventCount(page, "thrower-died") >
      combatThrowerDeathsBefore;
    const roomClearedWithLure = await eventCount(page, "room-cleared") >
      combatRoomClearsBefore;
    if (throwerDiedWithLure && !roomClearedWithLure) {
      await waitForChaserAdjacent(page);
      combatPlacementsBefore = await eventCount(
        page,
        "place-bomb-definition-prototype-cross",
      );
      combatExplosionsBefore = await eventCount(page, "bomb-exploded");
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, "place-bomb-definition-prototype-cross", {
        count: combatPlacementsBefore + 1,
        timeout: 5_000,
      });
      await moveToCell(page, 3, -1);
      await waitForEvent(page, "bomb-exploded", {
        count: combatExplosionsBefore + 1,
        timeout: 15_000,
      });
    } else if (!throwerDiedWithLure) {
      await moveToCell(page, 4, 0);
      await waitForEvent(page, "thrower-telegraph", {
        count: combatThrowerTelegraphsBefore + 1,
        timeout: 10_000,
      });
      combatPlacementsBefore = await eventCount(
        page,
        "place-bomb-definition-prototype-cross",
      );
      combatExplosionsBefore = await eventCount(page, "bomb-exploded");
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, "place-bomb-definition-prototype-cross", {
        count: combatPlacementsBefore + 1,
        timeout: 5_000,
      });
      await moveToCell(page, 3, -2, "zx");
      await waitForEvent(page, "bomb-exploded", {
        count: combatExplosionsBefore + 1,
        timeout: 15_000,
      });
    }
    await waitForEvent(page, "thrower-died", {
      count: combatThrowerDeathsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "room-cleared", {
      count: combatRoomClearsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "combat-reward-tokens-1", { timeout: 5_000 });
    checks.push({
      name: "bomb-input",
      status: "passed",
      detail: throwerDiedWithLure
        ? "Cleared the first combat room by intercepting the staged thrower with the first lure bomb and trapping the surviving chaser with a follow-up when required."
        : "Cleared the first combat room with a deliberate chaser lure followed by a post-Telegraph interception of the staged thrower.",
    });

    await moveToCell(page, -3, 0, "zx");
    fs.mkdirSync(path.dirname(secretWallScreenshotPath), { recursive: true });
    await page.screenshot({ path: secretWallScreenshotPath });
    const secretRevealBefore = await eventCount(
      page,
      "secret-wall-revealed-room-2-direction-west",
    );
    const secretBombPlacementsBefore = await eventCount(
      page,
      "place-bomb-definition-prototype-cross",
    );
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      count: secretBombPlacementsBefore + 1,
      timeout: 5_000,
    });
    await moveToCell(page, 0, -1);
    await waitForEvent(page, "secret-wall-revealed-room-2-direction-west", {
      count: secretRevealBefore + 1,
      timeout: 15_000,
    });
    await waitForEvent(page, "minimap-current-room-2", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-rooms-4", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-connections-3", { timeout: 5_000 });
    checks.push({
      name: "secret-wall-explosion-reveal",
      status: "passed",
      detail: "A real cross-bomb explosion destroyed room 2's cracked west exit and revealed the hidden room on the minimap.",
    });

    await moveToCell(page, -3, 0, "zx");
    const secretReadyBefore = await eventCount(
      page,
      "dungeon-room-ready-10-secret-safe",
    );
    await triggerBoundaryTransition(
      page,
      "ArrowLeft",
      "dungeon-room-ready-10-secret-safe",
      secretReadyBefore + 1,
    );
    await waitForEvent(page, "minimap-current-room-10", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-rooms-4", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-connections-3", { timeout: 5_000 });
    fs.mkdirSync(path.dirname(secretRoomScreenshotPath), { recursive: true });
    await page.screenshot({ path: secretRoomScreenshotPath });

    const secretRewardsBefore = await eventCount(
      page,
      "secret-reward-collected-3",
    );
    const secretInteractionsBefore = await eventCount(page, "interact");
    await moveToCell(page, 3, 1);
    await moveToCell(page, 0, 1);
    await page.keyboard.press("KeyE");
    await waitForEvent(page, "interact", {
      count: secretInteractionsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "secret-reward-collected-3", {
      count: secretRewardsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "room-reward-tokens-4", { timeout: 5_000 });
    checks.push({
      name: "secret-room-cache",
      status: "passed",
      detail: "The hidden safe room loaded through the revealed entrance and its central cache awarded three room tokens.",
    });

    await moveToCell(page, 0, 1);
    await moveToCell(page, 3, 1);
    await moveToCell(page, 3, 0);
    await moveToCell(page, 4, 0);
    const clearedRoomReadyBeforeSecretExit = await eventCount(
      page,
      "dungeon-room-ready-2-combat-cleared",
    );
    await triggerBoundaryTransition(
      page,
      "ArrowRight",
      "dungeon-room-ready-2-combat-cleared",
      clearedRoomReadyBeforeSecretExit + 1,
    );
    checks.push({
      name: "secret-room-return",
      status: "passed",
      detail: "The revealed entrance remained traversable in both directions and returned to the already-cleared combat room.",
    });

    await moveToCell(page, -3, 0);
    await moveToCell(page, 3, 0);
    await moveToCell(page, 3, 4);
    await moveToCell(page, 0, 4);
    const rewardReadyBefore = await eventCount(
      page,
      "dungeon-room-ready-3-bomb-reward-safe",
    );
    await triggerBoundaryTransition(
      page,
      "ArrowUp",
      "dungeon-room-ready-3-bomb-reward-safe",
      rewardReadyBefore + 1,
    );
    checks.push({
      name: "combat-clear-to-reward",
      status: "passed",
      detail: "Clearing room 2 opened its north exit and committed the safe BombReward room 3.",
    });

    await moveToCell(page, -1, -4);
    await moveToCell(page, -1, 0);
    await waitForEvent(page, "bomb-reward-selected-prototype-area", {
      timeout: 5_000,
    });
    checks.push({
      name: "bomb-reward-selection",
      status: "passed",
      detail: "Walking onto the left reward equipped prototype-area into the empty second slot.",
    });
    await page.keyboard.press("KeyX");
    await waitForEvent(page, "active-bomb-slot-1", { timeout: 5_000 });
    await moveToCell(page, -1, -4);
    await moveToCell(page, 0, -4);

    await triggerBoundaryTransition(
      page,
      "ArrowDown",
      "dungeon-room-ready-2-combat-cleared",
    );
    const clearedReentryStart = await page.evaluate(() =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__.length);
    await page.waitForTimeout(500);
    const clearedReentryEnemyEvents = await page.evaluate((startIndex) =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__
        .slice(startIndex)
        .map((event) => typeof event === "string" ? event : event?.name)
        .filter((name) => name === "chaser-moved" || name?.startsWith("charger-") ||
          name?.startsWith("armored-")), clearedReentryStart);
    if (clearedReentryEnemyEvents.length > 0) {
      throw new Error(
        `Cleared room reentry produced enemy events: ${clearedReentryEnemyEvents.join(", ")}`,
      );
    }
    await triggerBoundaryTransition(
      page,
      "ArrowUp",
      "dungeon-room-ready-3-bomb-reward-safe",
      rewardReadyBefore + 2,
    );
    checks.push({
      name: "cleared-combat-backtrack",
      status: "passed",
      detail: "Room 2 reentered as cleared, emitted no enemy activity, and allowed immediate travel back to reward room 3.",
    });

    await moveToCell(page, 0, -3);
    await moveToCell(page, -1, -3);
    await moveToCell(page, -1, 4);
    await moveToCell(page, 0, 4);
    const room4EntryEventStart = await page.evaluate(() =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__.length);
    const room4ChargerTrackBefore = await eventCount(page, "charger-track-moved");
    await triggerBoundaryTransition(
      page,
      "ArrowUp",
      "dungeon-room-ready-4-combat-active",
    );
    await waitForEvent(page, "room-ready-prototype-combat-pillars", {
      timeout: 60_000,
    });
    await waitForEvent(page, "charger-track-moved", {
      count: room4ChargerTrackBefore + 1,
      timeout: 5_000,
    });
    const firstRoom4ChargerAction = await page.evaluate((startIndex) =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__
        .slice(startIndex)
        .map((event) => typeof event === "string" ? event : event?.name)
        .find((name) =>
          name === "charger-track-moved" || name === "charger-telegraph"),
    room4EntryEventStart);
    if (firstRoom4ChargerAction !== "charger-track-moved") {
      throw new Error(
        `Expected the first Pillars charger action to be Track movement, got ${firstRoom4ChargerAction ?? "<none>"}.`,
      );
    }
    checks.push({
      name: "reward-to-next-combat",
      status: "passed",
      detail: "The reward room north exit committed the next uncleared combat room 4.",
    });
    checks.push({
      name: "charger-safe-entry-track-first",
      status: "passed",
      detail: "The seed-0 Pillars spawn produced Track movement before any Telegraph instead of attacking immediately on entry.",
    });

    const room4ExplosionsBefore = await eventCount(page, "bomb-exploded");
    const room4ClearsBefore = await eventCount(page, "room-cleared");
    await moveToCell(page, 0, -4);
    await waitForChaserAdjacent(page);
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-area", {
      timeout: 5_000,
    });
    checks.push({
      name: "bomb-reward-loadout-persistence",
      status: "passed",
      detail: "The area bomb selected before backtracking remained active and placed in room 4 without another swap input.",
    });
    await moveToCell(page, -3, -4);
    await waitForEvent(page, "bomb-exploded", {
      count: room4ExplosionsBefore + 1,
      timeout: 15_000,
    });
    if (await eventCount(page, "room-cleared") < room4ClearsBefore + 1) {
      const room4ChargerTelegraphsBefore = await eventCount(
        page,
        "charger-telegraph",
      );
      await waitForEvent(page, "charger-telegraph", {
        count: room4ChargerTelegraphsBefore + 1,
        timeout: 10_000,
      });
      const room4CrossExplosionsBefore = await eventCount(page, "bomb-exploded");
      await page.keyboard.press("KeyX");
      await waitForEvent(page, "active-bomb-slot-0", { timeout: 5_000 });
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, "place-bomb-definition-prototype-cross", {
        timeout: 5_000,
      });
      await moveToCell(page, 0, -4);
      await waitForEvent(page, "bomb-exploded", {
        count: room4CrossExplosionsBefore + 1,
        timeout: 15_000,
      });
    }
    await waitForEvent(page, "room-cleared", {
      count: room4ClearsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "combat-reward-tokens-5", { timeout: 5_000 });
    checks.push({
      name: "second-main-path-combat-clear",
      status: "passed",
      detail: "The room-4 enemies entered the selected area bomb footprint before the player escaped east, clearing the Pillars encounter.",
    });

    await moveToCell(page, -3, -4);
    await moveToCell(page, 4, -4);
    await moveToCell(page, 4, 0);
    await triggerBoundaryTransition(
      page,
      "ArrowRight",
      "dungeon-room-ready-5-combat-active",
    );
    await waitForEvent(page, "room-ready-prototype-combat-gates", {
      timeout: 60_000,
    });
    await moveToCell(page, 3, -3, "zx");
    await waitForSelfDestructAtCell(page, 3, -1, 10_000);
    await moveToCell(page, -1, -2, "xz");
    fs.mkdirSync(path.dirname(gatesRoomScreenshotPath), { recursive: true });
    await page.screenshot({ path: gatesRoomScreenshotPath });
    checks.push({
      name: "gates-room-visible",
      status: "passed",
      detail: gatesRoomScreenshotPath,
    });

    const gatesDestroyedWallsBefore = await eventCount(
      page,
      "destructible-wall-destroyed",
    );
    const room5AreaPlacementsBefore = await eventCount(
      page,
      "place-bomb-definition-prototype-area",
    );
    const room5ClearsBefore = await eventCount(page, "room-cleared");
    await waitForEvent(page, "self-destruct-warning-chase", {
      timeout: 10_000,
    });
    await waitForSelfDestructAtCell(page, 2, -2, 10_000);
    await moveToCell(page, -1, -3);
    await page.waitForTimeout(200);
    await moveToCell(page, -1, -2);
    await waitForSelfDestructAtCell(page, 0, -2, 10_000);
    await waitForEvent(page, "self-destruct-armed", { timeout: 5_000 });
    await waitForEvent(page, "self-destruct-telegraph", { timeout: 5_000 });
    const gatesChaserAtArm = await getLastChaserCell(page);
    if (!gatesChaserAtArm) {
      throw new Error("The Gates cleanup route requires a reported chaser cell.");
    }
    const gatesTrapX = gatesChaserAtArm.x < -1 ? 1 : -1;
    const gatesEscapeX = gatesTrapX > 0 ? 4 : -4;
    await moveToCell(page, gatesTrapX, -3, "zx");
    await waitForEvent(page, "self-destruct-detonated", { timeout: 5_000 });
    await waitForEvent(page, "self-destruct-died", { timeout: 5_000 });
    await waitForEvent(
      page,
      "bomb-exploded-definition-prototype-self-destruct-blast",
      { timeout: 5_000 },
    );
    await waitForEvent(page, "destructible-wall-destroyed", {
      count: gatesDestroyedWallsBefore + 1,
      timeout: 5_000,
    });
    const gatesChaserBeforeCleanup = await getLastChaserCell(page);
    const gatesPlayerBeforeCleanup = await getLastPlayerCell(page);
    if (!gatesChaserBeforeCleanup || !gatesPlayerBeforeCleanup) {
      throw new Error("The Gates cleanup route lost its actor positions.");
    }
    const gatesCleanupDistance =
      Math.abs(gatesChaserBeforeCleanup.x - gatesPlayerBeforeCleanup.x) +
      Math.abs(gatesChaserBeforeCleanup.z - gatesPlayerBeforeCleanup.z);
    if (gatesCleanupDistance > 3) {
      await waitForChaserAtDistance(page, 3);
    }
    const room5CleanupExplosionsBefore = await eventCount(page, "bomb-exploded");
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-area", {
      count: room5AreaPlacementsBefore + 1,
      timeout: 5_000,
    });
    await moveToCell(page, gatesEscapeX, -3);
    await waitForEvent(page, "bomb-exploded", {
      count: room5CleanupExplosionsBefore + 1,
      timeout: 15_000,
    });
    await waitForEvent(page, "room-cleared", {
      count: room5ClearsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "combat-reward-tokens-6", { timeout: 5_000 });
    checks.push({
      name: "self-destruct-gates-interaction",
      status: "passed",
      detail: "The Gates enemy followed the player's two-stage lure, entered warning chase, stopped at the lower lure cell, armed, detonated its dedicated blast, destroyed one authored gate, and died once.",
    });
    checks.push({
      name: "third-main-path-combat-clear",
      status: "passed",
      detail: "After the gate blast, a pre-emptive east-lane area bomb caught the pursuing chaser without another contact hit and cleared combat room 5.",
    });

    const healthBeforeRecoveryDetour = await getLastPlayerHealth(page);
    if (!Number.isInteger(healthBeforeRecoveryDetour) ||
        healthBeforeRecoveryDetour < 1 ||
        healthBeforeRecoveryDetour >= 5) {
      throw new Error(
        `The recovery-room probe requires damaged living health, observed ${healthBeforeRecoveryDetour}.`,
      );
    }
    const expectedRecoveredHealth = Math.min(5, healthBeforeRecoveryDetour + 2);
    const expectedRestoredHealth =
      expectedRecoveredHealth - healthBeforeRecoveryDetour;
    await moveToCell(page, -3, 0);
    await moveToCell(page, -3, 4);
    await moveToCell(page, 0, 4);
    await triggerBoundaryTransition(
      page,
      "ArrowUp",
      "dungeon-room-ready-8-recovery-safe",
    );
    await waitForEvent(page, "minimap-current-room-8", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-rooms-9", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-connections-8", {
      timeout: 5_000,
    });
    fs.mkdirSync(path.dirname(recoveryRoomScreenshotPath), { recursive: true });
    await page.screenshot({ path: recoveryRoomScreenshotPath });

    const healthOnRecoveryEntry = await getLastPlayerHealth(page);
    if (healthOnRecoveryEntry !== healthBeforeRecoveryDetour) {
      throw new Error(
        `Entering the recovery room changed health ${healthBeforeRecoveryDetour}→${healthOnRecoveryEntry}.`,
      );
    }
    const recoveryConsumedBefore = await eventCount(
      page,
      "recovery-consumed-room-8",
    );
    const recoveredHealthBefore = await eventCount(
      page,
      `player-health-recovered-${expectedRestoredHealth}`,
    );
    const recoveryInteractionsBefore = await eventCount(page, "interact");
    await moveToCell(page, 1, -4);
    await moveToCell(page, 1, 0);
    await page.keyboard.press("KeyE");
    await waitForEvent(page, "interact", {
      count: recoveryInteractionsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(
      page,
      `player-health-recovered-${expectedRestoredHealth}`,
      {
        count: recoveredHealthBefore + 1,
        timeout: 5_000,
      },
    );
    await waitForEvent(page, "recovery-consumed-room-8", {
      count: recoveryConsumedBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(
      page,
      `player-health-current-${expectedRecoveredHealth}`,
      { timeout: 5_000 },
    );
    checks.push({
      name: "recovery-pickup-restores-and-consumes",
      status: "passed",
      detail: `The optional recovery leaf preserved entry HP, restored ${healthBeforeRecoveryDetour}→${expectedRecoveredHealth}, and consumed its pickup once.`,
    });

    await moveToCell(page, 1, 0);
    await moveToCell(page, 1, -4);
    await moveToCell(page, 0, -4);
    const room5ClearedReadyBeforeBoss = await eventCount(
      page,
      "dungeon-room-ready-5-combat-cleared",
    );
    await triggerBoundaryTransition(
      page,
      "ArrowDown",
      "dungeon-room-ready-5-combat-cleared",
      room5ClearedReadyBeforeBoss + 1,
    );

    await moveToCell(page, -3, 0);
    await moveToCell(page, 4, 0);
    await triggerBoundaryTransition(
      page,
      "ArrowRight",
      "dungeon-room-ready-6-boss-antechamber-safe",
    );
    await waitForEvent(page, "minimap-current-room-6", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-rooms-10", { timeout: 5_000 });
    await waitForEvent(page, "minimap-visible-connections-9", {
      timeout: 5_000,
    });
    checks.push({
      name: "boss-antechamber-reached",
      status: "passed",
      detail: "The full seed-0 main path reached the safe boss antechamber room 6.",
    });

    await moveToCell(page, -4, -3);
    await moveToCell(page, 0, -3);
    await triggerBoundaryTransition(
      page,
      "ArrowDown",
      "dungeon-room-ready-7-boss-active",
    );
    await waitForEvent(page, "boss-intro-started", { timeout: 5_000 });
    await waitForEvent(page, "boss-intro-completed", { timeout: 10_000 });
    await waitForEvent(page, "boss-pattern-limited-chase-telegraph", {
      timeout: 10_000,
    });
    await waitForEvent(page, "boss-cell-x-0-z-1", { timeout: 5_000 });
    const firstChargeTelegraphBefore = await eventCount(
      page,
      "boss-pattern-fixed-charge-telegraph",
    );
    await waitForEvent(page, "boss-pattern-fixed-charge-telegraph", {
      count: firstChargeTelegraphBefore + 1,
      timeout: 15_000,
    });
    fs.mkdirSync(path.dirname(bossTelegraphScreenshotPath), { recursive: true });
    await page.screenshot({ path: bossTelegraphScreenshotPath });
    checks.push({
      name: "boss-telegraph-visible",
      status: "passed",
      detail: bossTelegraphScreenshotPath,
    });
    const bossClearBefore = await eventCount(page, "room-cleared");
    const bossMoveBlocksBefore = await eventCount(page, "boss-move-blocked");

    let activeBossBombSlot = 1;

    const placeBossCounterBomb = async (definitionId) => {
      const marker = `place-bomb-definition-${definitionId}`;
      const before = await eventCount(page, marker);
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, marker, {
        count: before + 1,
        timeout: 5_000,
      });
    };

    const swapBossCounterSlot = async (targetSlot) => {
      const marker = `active-bomb-slot-${targetSlot}`;
      const before = await eventCount(page, marker);
      await page.keyboard.press("KeyX");
      await waitForEvent(page, marker, {
        count: before + 1,
        timeout: 5_000,
      });
      activeBossBombSlot = targetSlot;
    };

    const dodgeNextBossCharge = async () => {
      const before = await eventCount(
        page,
        "boss-pattern-fixed-charge-telegraph",
      );
      await waitForEvent(page, "boss-pattern-fixed-charge-telegraph", {
        count: before + 1,
        timeout: 20_000,
      });
      await moveToCell(page, 4, -2, "zx");
      await moveToCell(page, 1, -2);
      await moveToCell(page, 1, 0);
    };

    const counterBossOverheat = async (parityRows, label, finalCycle = false) => {
      const parityTelegraphsBefore = await eventCount(
        page,
        "boss-pattern-parity-wave-telegraph",
      );
      const parityRecoveriesBefore = await eventCount(
        page,
        "boss-pattern-parity-wave-recovery",
      );
      const overheatRecoveriesBefore = await eventCount(
        page,
        "boss-pattern-overheat-recovery",
      );
      const damageBefore = await eventCount(page, "boss-damaged");

      await waitForEvent(page, "boss-pattern-parity-wave-telegraph", {
        count: parityTelegraphsBefore + 1,
        timeout: 25_000,
      });
      await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
        count: parityRecoveriesBefore + 1,
        timeout: 5_000,
      });

      if (parityRows === 9) {
        if (activeBossBombSlot === 1) {
          await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
            count: parityRecoveriesBefore + 5,
            timeout: 10_000,
          });
          await moveToCell(page, 0, 0);
          await placeBossCounterBomb("prototype-area");
          await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
            count: parityRecoveriesBefore + 6,
            timeout: 5_000,
          });
          await moveToCell(page, 1, 1);
          await swapBossCounterSlot(0);
          await placeBossCounterBomb("prototype-cross");
        } else {
          await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
            count: parityRecoveriesBefore + 3,
            timeout: 10_000,
          });
          await moveToCell(page, 1, 1);
          await placeBossCounterBomb("prototype-cross");
          await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
            count: parityRecoveriesBefore + 5,
            timeout: 5_000,
          });
          await moveToCell(page, 0, 0, "zx");
          await swapBossCounterSlot(1);
          await placeBossCounterBomb("prototype-area");
        }
      } else if (activeBossBombSlot === 1) {
        await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
          count: parityRecoveriesBefore + 12,
          timeout: 15_000,
        });
        await moveToCell(page, 0, 0);
        await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
          count: parityRecoveriesBefore + 14,
          timeout: 5_000,
        });
        await placeBossCounterBomb("prototype-area");
        await moveToCell(page, 1, 1);
        await swapBossCounterSlot(0);
        await placeBossCounterBomb("prototype-cross");
      } else {
        await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
          count: parityRecoveriesBefore + 3,
          timeout: 15_000,
        });
        await moveToCell(page, 0, 0);
        await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
          count: parityRecoveriesBefore + 12,
          timeout: 15_000,
        });
        await moveToCell(page, 1, 0);
        await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
          count: parityRecoveriesBefore + 13,
          timeout: 5_000,
        });
        await moveToCell(page, 1, 1);
        await placeBossCounterBomb("prototype-cross");
        await waitForEvent(page, "boss-pattern-parity-wave-recovery", {
          count: parityRecoveriesBefore + 14,
          timeout: 5_000,
        });
        await moveToCell(page, 0, 0, "zx");
        await swapBossCounterSlot(1);
        await placeBossCounterBomb("prototype-area");
      }

      await moveToCell(page, 4, 0, "zx");
      await waitForEvent(page, "boss-pattern-overheat-recovery", {
        count: overheatRecoveriesBefore + 1,
        timeout: 10_000,
      });
      if (finalCycle) {
        await waitForEvent(page, "boss-defeated", { timeout: 8_000 });
      } else {
        await waitForEvent(page, "boss-damaged", {
          count: damageBefore + 2,
          timeout: 8_000,
        });
      }
      checks.push({
        name: `boss-overheat-${label}`,
        status: "passed",
        detail: `Two distinct player bombs were preplaced through ${parityRows} sequential parity rows and damaged the always-damageable boss during the scripted counter sequence.`,
      });
    };

    await moveToCell(page, 1, 0, "xz");
    await counterBossOverheat(9, "phase-one-a");
    await dodgeNextBossCharge();
    await counterBossOverheat(9, "phase-one-b");

    await waitForEvent(page, "boss-phase-two", { timeout: 15_000 });
    await waitForEvent(page, "boss-summon-target-x--3-z-3", {
      timeout: 10_000,
    });
    await waitForEvent(page, "boss-self-destruct-spawned", {
      timeout: 10_000,
    });
    await dodgeNextBossCharge();
    await waitForEvent(page, "self-destruct-armed", { timeout: 12_000 });
    await moveToCell(page, 0, -4, "zx");
    await waitForEvent(page, "self-destruct-died", { timeout: 5_000 });
    await moveToCell(page, 1, 0, "xz");
    await counterBossOverheat(18, "phase-two-a");

    await dodgeNextBossCharge();
    await counterBossOverheat(18, "phase-two-b");
    await waitForEvent(page, "boss-phase-last-stand", { timeout: 15_000 });
    await dodgeNextBossCharge();
    await counterBossOverheat(18, "last-stand", true);

    if (await eventCount(page, "boss-move-blocked") !== bossMoveBlocksBefore) {
      throw new Error("The deterministic boss movement was unexpectedly blocked.");
    }
    checks.push({
      name: "boss-phased-counterplay",
      status: "passed",
      detail: "The browser route cleared two phase-one overheats, the one-time self-destruct gate, two phase-two overheats, and the one-time last stand without exact move-target ghosts.",
    });
    await waitForEvent(
      page,
      "boss-bomb-armed-definition-prototype-boss-chain",
      { timeout: 10_000 },
    );
    await waitForEvent(page, "boss-chain-bomb-detonated-by-chain", {
      timeout: 10_000,
    });
    await waitForEvent(page, "room-cleared", {
      count: bossClearBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "run-completed", { timeout: 5_000 });
    checks.push({
      name: "boss-battle-cleared",
      status: "passed",
      detail: "Room 7 accepted player-bomb damage through all three phases and presented the floor-clear result once.",
    });

    await page.setViewportSize({ width: 1024, height: 768 });
    await page.waitForTimeout(250);
    const desktopFit = await assertCanvasFitsViewport(page, "Desktop resize");
    await page.setViewportSize({ width: 640, height: 720 });
    await page.waitForTimeout(250);
    const narrowFit = await assertCanvasFitsViewport(page, "Narrow resize");
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.waitForTimeout(250);
    checks.push({
      name: "resize",
      status: "passed",
      detail: `${desktopFit}; ${narrowFit}`,
    });

    fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
    await page.screenshot({ path: screenshotPath });
    checks.push({ name: "screenshot", status: "passed", detail: screenshotPath });

    const restartedStartReadyBefore = await eventCount(
      page,
      "dungeon-room-ready-1-start-safe",
    );
    const startMinimapCurrentBeforeCompletedRestart = await eventCount(
      page,
      "minimap-current-room-1",
    );
    const startMinimapRoomsBeforeCompletedRestart = await eventCount(
      page,
      "minimap-visible-rooms-2",
    );
    const startMinimapConnectionsBeforeCompletedRestart = await eventCount(
      page,
      "minimap-visible-connections-1",
    );
    const zeroTokenEventsBeforeCompletedRestart = await eventCount(
      page,
      "combat-reward-tokens-0",
    );
    const fullHealthEventsBeforeCompletedRestart = await eventCount(
      page,
      "player-health-current-5",
    );
    const lobbyReadyBeforeCompletedReturn = await eventCount(page, "lobby-ready");
    const lobbyStartRequestsBeforeCompletedReturn = await eventCount(
      page,
      "lobby-start-requested",
    );
    const lobbyRequestsBeforeCompletedReturn = await eventCount(
      page,
      "run-lobby-requested",
    );
    await page.keyboard.down("ArrowRight");
    await page.waitForTimeout(100);
    await page.keyboard.up("ArrowRight");
    await page.waitForTimeout(100);
    await page.keyboard.press("Enter");
    await waitForEvent(page, "run-lobby-requested", {
      count: lobbyRequestsBeforeCompletedReturn + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "lobby-ready", {
      count: lobbyReadyBeforeCompletedReturn + 1,
      timeout: 20_000,
    });
    await page.keyboard.press("Enter");
    await waitForEvent(page, "lobby-start-requested", {
      count: lobbyStartRequestsBeforeCompletedReturn + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "dungeon-room-ready-1-start-safe", {
      count: restartedStartReadyBefore + 1,
      timeout: 20_000,
    });
    await waitForEvent(page, "combat-reward-tokens-0", {
      count: zeroTokenEventsBeforeCompletedRestart + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "player-health-current-5", {
      count: fullHealthEventsBeforeCompletedRestart + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "minimap-current-room-1", {
      count: startMinimapCurrentBeforeCompletedRestart + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "minimap-visible-rooms-2", {
      count: startMinimapRoomsBeforeCompletedRestart + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "minimap-visible-connections-1", {
      count: startMinimapConnectionsBeforeCompletedRestart + 1,
      timeout: 5_000,
    });
    checks.push({
      name: "completed-run-lobby-roundtrip",
      status: "passed",
      detail: "The result UI returned to the lobby, then started a full-health seed-0 run without reloading the browser page.",
    });

    for (let hit = 0; hit < 5; hit++) {
      const placementsBefore = await eventCount(
        page,
        "place-bomb-definition-prototype-cross",
      );
      const explosionsBefore = await eventCount(page, "bomb-exploded");
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, "place-bomb-definition-prototype-cross", {
        count: placementsBefore + 1,
        timeout: 5_000,
      });
      await waitForEvent(page, "bomb-exploded", {
        count: explosionsBefore + 1,
        timeout: 5_000,
      });
    }
    await waitForEvent(page, "player-died", { timeout: 5_000 });
    await waitForEvent(page, "run-failed", { timeout: 5_000 });
    await waitForEvent(page, "run-failed-cause-bomb-explosion", {
      timeout: 5_000,
    });
    fs.mkdirSync(path.dirname(runFailureScreenshotPath), { recursive: true });
    await page.screenshot({ path: runFailureScreenshotPath });
    checks.push({
      name: "failed-run-result",
      status: "passed",
      detail: "Five self explosions in the restarted safe room produced one player death, the run-failed result, and the BOMB EXPLOSION cause.",
    });

    const failureRestartRequestsBefore = await eventCount(
      page,
      "run-restart-requested",
    );
    const failureRestartsBefore = await eventCount(page, "dungeon-run-restarted");
    const failureStartReadyBefore = await eventCount(
      page,
      "dungeon-room-ready-1-start-safe",
    );
    const startMinimapCurrentBeforeFailureRestart = await eventCount(
      page,
      "minimap-current-room-1",
    );
    const startMinimapRoomsBeforeFailureRestart = await eventCount(
      page,
      "minimap-visible-rooms-2",
    );
    const startMinimapConnectionsBeforeFailureRestart = await eventCount(
      page,
      "minimap-visible-connections-1",
    );
    const zeroTokenEventsBeforeFailureRestart = await eventCount(
      page,
      "combat-reward-tokens-0",
    );
    await page.keyboard.press("KeyR");
    await waitForEvent(page, "run-restart-requested", {
      count: failureRestartRequestsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "dungeon-run-restarted", {
      count: failureRestartsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "dungeon-room-ready-1-start-safe", {
      count: failureStartReadyBefore + 1,
      timeout: 20_000,
    });
    await waitForEvent(page, "combat-reward-tokens-0", {
      count: zeroTokenEventsBeforeFailureRestart + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "minimap-current-room-1", {
      count: startMinimapCurrentBeforeFailureRestart + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "minimap-visible-rooms-2", {
      count: startMinimapRoomsBeforeFailureRestart + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "minimap-visible-connections-1", {
      count: startMinimapConnectionsBeforeFailureRestart + 1,
      timeout: 5_000,
    });
    checks.push({
      name: "failed-run-restart",
      status: "passed",
      detail: "R restarted the failed run from a fresh same-seed start session without reloading the browser page.",
    });

    const requiredEvents = [
      "lobby-ready",
      "lobby-settings-opened",
      "settings-opened",
      "settings-audio-page-opened",
      "settings-bgm-volume-changed",
      "settings-closed",
      "lobby-start-requested",
      "run-lobby-requested",
      "probe-ready",
      "room-ready-prototype-combat-loop",
      "dungeon-room-ready-1-start-safe",
      "move",
      "move-step-direction-north",
      "move-step-direction-west",
      "move-step-direction-south",
      "dungeon-transition-started",
      "dungeon-room-committed",
      "dungeon-room-ready-2-combat-active",
      "room-ready-prototype-combat-thrower",
      "thrower-telegraph",
      "thrower-bomb-launched",
      "room-ready-prototype-combat-pillars",
      "charger-track-moved",
      "move-motion-direction-west",
      "move-motion-direction-north",
      "place-bomb-definition-prototype-cross",
      "bomb-exploded",
      "active-bomb-slot-1",
      "place-bomb-definition-prototype-area",
      "room-cleared",
      "combat-reward-tokens-0",
      "combat-reward-tokens-1",
      "combat-reward-tokens-5",
      "combat-reward-tokens-6",
      "secret-wall-revealed-room-2-direction-west",
      "dungeon-room-ready-10-secret-safe",
      "minimap-current-room-10",
      "minimap-visible-rooms-4",
      "minimap-visible-connections-3",
      "interact",
      "secret-reward-collected-3",
      "room-reward-tokens-4",
      "dungeon-room-ready-3-bomb-reward-safe",
      "bomb-reward-selected-prototype-area",
      "dungeon-room-ready-2-combat-cleared",
      "dungeon-room-ready-4-combat-active",
      "dungeon-room-ready-5-combat-active",
      "dungeon-room-ready-5-combat-cleared",
      "room-ready-prototype-combat-gates",
      "dungeon-room-ready-8-recovery-safe",
      "minimap-current-room-8",
      "minimap-visible-rooms-9",
      "minimap-visible-connections-8",
      `player-health-recovered-${expectedRestoredHealth}`,
      "recovery-consumed-room-8",
      "dungeon-room-ready-6-boss-antechamber-safe",
      "minimap-current-room-6",
      "minimap-visible-rooms-10",
      "minimap-visible-connections-9",
      "dungeon-room-ready-7-boss-active",
      "boss-pattern-telegraph",
      "boss-pattern-limited-chase-telegraph",
      "boss-pattern-fixed-charge-telegraph",
      "boss-pattern-return-to-center-telegraph",
      "boss-pattern-bomb-volley-telegraph",
      "boss-pattern-parity-wave-telegraph",
      "boss-pattern-overheat-recovery",
      "boss-pattern-summon-self-destruct-telegraph",
      "boss-pattern-wait-for-self-destruct-telegraph",
      "boss-pattern-last-stand-bomb-chain-telegraph",
      "boss-summon-target-x--3-z-3",
      "boss-self-destruct-spawned",
      "boss-bomb-launched-definition-prototype-boss-throw",
      "boss-bomb-launched-definition-prototype-boss-chain",
      "boss-bomb-armed-definition-prototype-boss-throw",
      "boss-bomb-armed-definition-prototype-boss-chain",
      "bomb-exploded-definition-prototype-boss-throw",
      "bomb-exploded-definition-prototype-boss-chain",
      "boss-chain-bomb-detonated-by-chain",
      "boss-cell-x-0-z-1",
      "boss-moved",
      "boss-pattern-execute",
      "boss-pattern-recovery",
      "boss-damaged",
      "boss-phase-two",
      "boss-phase-last-stand",
      "boss-defeated",
      "run-completed",
      "player-died",
      "run-failed",
      "run-failed-cause-bomb-explosion",
      "run-restart-requested",
      "dungeon-run-restarted",
      "swap-bomb",
      "pause-entered",
      "pause-resumed",
      "pause-resume",
      "audio-unlocked",
      "bgm-audio-started",
    ];
    const harnessEvents = await page.evaluate(() =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__ ?? null);
    const observedNames = new Set(
      Array.isArray(harnessEvents)
        ? harnessEvents
          .map((event) => typeof event === "string" ? event : event?.name)
          .filter((name) => typeof name === "string")
        : [],
    );
    const missingEvents = requiredEvents.filter((name) => !observedNames.has(name));
    checks.push({
      name: "gameplay-probe",
      status: missingEvents.length === 0 ? "passed" : "failed",
      detail: missingEvents.length === 0
        ? { required: requiredEvents, observed: [...observedNames] }
        : { missing: missingEvents, observed: [...observedNames] },
    });
    const playtestLog = await verifyPlaytestLogExport(
      page,
      playtestLogPath,
      harnessEvents,
    );
    checks.push({
      name: "playtest-log-export",
      status: "passed",
      detail: playtestLog,
    });
    checks.push({
      name: "browser-console",
      status: consoleErrors.length === 0 && pageErrors.length === 0
        ? "passed"
        : "failed",
      detail: { consoleErrors, pageErrors },
    });

    const failedChecks = checks.filter((check) => check.status !== "passed");
    const report = {
      schemaVersion: 2,
      status: failedChecks.length === 0 ? "passed" : "failed",
      url,
      checks,
      consoleErrors,
      pageErrors,
      harnessEvents,
      screenshotPath,
      lobbyScreenshotPath,
      settingsScreenshotPath,
      gatesRoomScreenshotPath,
      recoveryRoomScreenshotPath,
      secretWallScreenshotPath,
      secretRoomScreenshotPath,
      pauseScreenshotPath,
      runFailureScreenshotPath,
      playtestLogPath,
      generatedAt: new Date().toISOString(),
    };
    fs.mkdirSync(path.dirname(reportPath), { recursive: true });
    fs.writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");
    if (failedChecks.length > 0) process.exitCode = 1;
  } catch (error) {
    let harnessEvents = null;
    if (page) {
      try {
        harnessEvents = await page.evaluate(() =>
          globalThis.__BOMBSWAP_HARNESS_EVENTS__ ?? null);
      } catch {
        // Preserve the original smoke failure when the page is already unavailable.
      }
    }
    checks.push({
      name: "smoke-execution",
      status: "failed",
      detail: String(error?.stack ?? error),
    });
    const report = {
      schemaVersion: 2,
      status: "failed",
      url,
      checks,
      consoleErrors,
      pageErrors,
      harnessEvents,
      screenshotPath,
      lobbyScreenshotPath,
      settingsScreenshotPath,
      gatesRoomScreenshotPath,
      secretWallScreenshotPath,
      secretRoomScreenshotPath,
      pauseScreenshotPath,
      runFailureScreenshotPath,
      playtestLogPath,
      generatedAt: new Date().toISOString(),
    };
    fs.mkdirSync(path.dirname(reportPath), { recursive: true });
    fs.writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");
    throw error;
  } finally {
    if (browser) await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
}

main().catch((error) => {
  process.stderr.write(`${error.stack ?? error}\n`);
  process.exitCode = 1;
});
