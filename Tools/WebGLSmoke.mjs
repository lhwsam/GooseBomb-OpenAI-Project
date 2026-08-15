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
  const pauseScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-paused.png`,
  );
  const runFailureScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-run-failed.png`,
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
    page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
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

    const canvas = page.locator("canvas").first();
    await canvas.click({ position: { x: 20, y: 20 } });
    const focusedTag = await page.evaluate(() => document.activeElement?.tagName ?? "");
    checks.push({
      name: "canvas-focus",
      status: focusedTag === "CANVAS" ? "passed" : "failed",
      detail: focusedTag,
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
    checks.push({
      name: "dungeon-start-ready",
      status: "passed",
      detail: "The safe Start placeholder initialized with the reusable loop shell.",
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

    await triggerBoundaryTransition(
      page,
      "ArrowLeft",
      "dungeon-room-ready-2-combat-active",
    );
    await waitForEvent(page, "room-ready-prototype-combat-pillars", {
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
      detail: "The seed-0 Start exit loaded and committed the assigned pillars combat scene.",
    });
    checks.push({
      name: "run-health-room-persistence",
      status: "passed",
      detail: "The next room initialized at 4 health instead of healing on scene transition.",
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
      "room-ready-prototype-combat-pillars",
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
    await waitForEvent(page, "room-ready-prototype-combat-pillars", {
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

    let combatPlacementsBefore = await eventCount(
      page,
      "place-bomb-definition-prototype-cross",
    );
    let combatExplosionsBefore = await eventCount(page, "bomb-exploded");
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      count: combatPlacementsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "bomb-exploded", {
      count: combatExplosionsBefore + 1,
      timeout: 15_000,
    });
    await moveToCell(page, 3, 3);
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
    await moveToCell(page, 3, 0);
    await waitForEvent(page, "bomb-exploded", {
      count: combatExplosionsBefore + 1,
      timeout: 15_000,
    });
    await waitForEvent(page, "room-cleared", { timeout: 5_000 });
    await waitForEvent(page, "combat-reward-tokens-1", { timeout: 5_000 });
    checks.push({
      name: "bomb-input",
      status: "passed",
      detail: "Cleared the first combat room with the single starting cross-bomb slot.",
    });

    const combatCell = await getLastPlayerCell(page);
    await moveToCell(page, 3, combatCell.z);
    await moveToCell(page, 3, 5);
    await moveToCell(page, 0, 5);
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
    await triggerBoundaryTransition(
      page,
      "ArrowUp",
      "dungeon-room-ready-4-combat-active",
    );
    checks.push({
      name: "reward-to-next-combat",
      status: "passed",
      detail: "The reward room north exit committed the next uncleared combat room 4.",
    });

    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-area", {
      timeout: 5_000,
    });
    checks.push({
      name: "bomb-reward-loadout-persistence",
      status: "passed",
      detail: "The area bomb selected before backtracking remained active and placed in room 4 without another swap input.",
    });

    const room4ExplosionsBefore = await eventCount(page, "bomb-exploded");
    const room4ClearsBefore = await eventCount(page, "room-cleared");
    await moveToCell(page, -3, -4);
    await waitForEvent(page, "bomb-exploded", {
      count: room4ExplosionsBefore + 1,
      timeout: 15_000,
    });
    if (await eventCount(page, "room-cleared") === room4ClearsBefore) {
      await moveToCell(page, 0, -5, "zx");
      await waitForChaserAdjacent(page);
      await page.waitForTimeout(100);
      const room4AreaPlacementsBefore = await eventCount(
        page,
        "place-bomb-definition-prototype-area",
      );
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, "place-bomb-definition-prototype-area", {
        count: room4AreaPlacementsBefore + 1,
        timeout: 5_000,
      });
      const secondRoom4ExplosionBefore = await eventCount(page, "bomb-exploded");
      await moveToCell(page, -2, -5);
      await waitForEvent(page, "bomb-exploded", {
        count: secondRoom4ExplosionBefore + 1,
        timeout: 5_000,
      });
    }
    await waitForEvent(page, "room-cleared", {
      count: room4ClearsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "combat-reward-tokens-2", { timeout: 5_000 });
    checks.push({
      name: "second-main-path-combat-clear",
      status: "passed",
      detail: "The selected area bomb cleared the rotated loop combat room 4.",
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
    fs.mkdirSync(path.dirname(gatesRoomScreenshotPath), { recursive: true });
    await page.screenshot({ path: gatesRoomScreenshotPath });
    checks.push({
      name: "gates-room-visible",
      status: "passed",
      detail: gatesRoomScreenshotPath,
    });

    const room5AreaPlacementsBefore = await eventCount(
      page,
      "place-bomb-definition-prototype-area",
    );
    await moveToCell(page, -3, 2);
    await waitForChaserAtDistance(page, 2);
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-area", {
      count: room5AreaPlacementsBefore + 1,
      timeout: 5_000,
    });
    const room5ExplosionsBefore = await eventCount(page, "bomb-exploded");
    const room5ClearsBefore = await eventCount(page, "room-cleared");
    await moveToCell(page, -5, 2);
    await waitForEvent(page, "bomb-exploded", {
      count: room5ExplosionsBefore + 1,
      timeout: 15_000,
    });
    if (await eventCount(page, "room-cleared") === room5ClearsBefore) {
      await moveToCell(page, -3, 1);
      await waitForChaserAtDistance(page, 2);
      await page.waitForTimeout(200);
      const secondRoom5AreaPlacementBefore = await eventCount(
        page,
        "place-bomb-definition-prototype-area",
      );
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, "place-bomb-definition-prototype-area", {
        count: secondRoom5AreaPlacementBefore + 1,
        timeout: 5_000,
      });
      const secondRoom5ExplosionBefore = await eventCount(page, "bomb-exploded");
      await moveToCell(page, -3, -1);
      await waitForEvent(page, "bomb-exploded", {
        count: secondRoom5ExplosionBefore + 1,
        timeout: 5_000,
      });
    }
    await waitForEvent(page, "room-cleared", {
      count: room5ClearsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "combat-reward-tokens-3", { timeout: 5_000 });
    checks.push({
      name: "third-main-path-combat-clear",
      status: "passed",
      detail: "The persisted reward loadout cleared the gates combat room 5 after using its west-side detour.",
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
    await moveToCell(page, 1, -4);
    await moveToCell(page, 1, 0);
    await moveToCell(page, 0, 0);
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
    await waitForEvent(page, "boss-pattern-telegraph", { timeout: 5_000 });
    fs.mkdirSync(path.dirname(bossTelegraphScreenshotPath), { recursive: true });
    await page.screenshot({ path: bossTelegraphScreenshotPath });
    checks.push({
      name: "boss-telegraph-visible",
      status: "passed",
      detail: bossTelegraphScreenshotPath,
    });

    const bossRecoveriesBefore = await eventCount(page, "boss-pattern-recovery");
    const bossDamageBefore = await eventCount(page, "boss-damaged");
    const bossClearBefore = await eventCount(page, "room-cleared");
    const bossBombTargets = [
      { x: 1, z: 1, escapeX: 3, escapeZ: 1 },
      { x: 1, z: 0, escapeX: 1, escapeZ: 2 },
      { x: 1, z: 0, escapeX: 1, escapeZ: 2 },
      { x: 1, z: 1, escapeX: 3, escapeZ: 1 },
    ];
    for (let index = 0; index < bossBombTargets.length; index++) {
      const target = bossBombTargets[index];
      await moveToCell(page, target.x, target.z);
      await waitForEvent(page, "boss-pattern-recovery", {
        count: bossRecoveriesBefore + index + 1,
        timeout: 10_000,
      });
      const placementsBefore = await eventCount(
        page,
        "place-bomb-definition-prototype-area",
      );
      await page.keyboard.press("KeyZ");
      await waitForEvent(page, "place-bomb-definition-prototype-area", {
        count: placementsBefore + 1,
        timeout: 5_000,
      });
      await moveToCell(page, target.escapeX, target.escapeZ);
      await waitForEvent(page, "boss-damaged", {
        count: bossDamageBefore + index + 1,
        timeout: 8_000,
      });
    }
    await waitForEvent(page, "boss-phase-two", { timeout: 10_000 });
    await waitForEvent(page, "boss-defeated", { timeout: 5_000 });
    await waitForEvent(page, "room-cleared", {
      count: bossClearBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "run-completed", { timeout: 5_000 });
    checks.push({
      name: "boss-battle-cleared",
      status: "passed",
      detail: "Room 7 telegraphed deterministic grid attacks, accepted four area-bomb counterattacks only during Recovery, and presented the floor-clear result once.",
    });

    await page.setViewportSize({ width: 1024, height: 768 });
    await page.waitForTimeout(250);
    checks.push({ name: "resize", status: "passed" });

    fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
    await page.screenshot({ path: screenshotPath });
    checks.push({ name: "screenshot", status: "passed", detail: screenshotPath });

    const restartedStartReadyBefore = await eventCount(
      page,
      "dungeon-room-ready-1-start-safe",
    );
    const zeroTokenEventsBeforeCompletedRestart = await eventCount(
      page,
      "combat-reward-tokens-0",
    );
    const fullHealthEventsBeforeCompletedRestart = await eventCount(
      page,
      "player-health-current-5",
    );
    await page.keyboard.press("KeyR");
    await waitForEvent(page, "run-restart-requested", { timeout: 5_000 });
    await waitForEvent(page, "dungeon-run-restarted", { timeout: 5_000 });
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
    checks.push({
      name: "completed-run-restart",
      status: "passed",
      detail: "R restarted the completed seed-0 run at full health in a fresh start room without reloading the browser page.",
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
    checks.push({
      name: "failed-run-restart",
      status: "passed",
      detail: "R restarted the failed run from a fresh same-seed start session without reloading the browser page.",
    });

    const requiredEvents = [
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
      "room-ready-prototype-combat-pillars",
      "move-motion-direction-west",
      "move-motion-direction-north",
      "place-bomb-definition-prototype-cross",
      "bomb-exploded",
      "active-bomb-slot-1",
      "place-bomb-definition-prototype-area",
      "room-cleared",
      "combat-reward-tokens-0",
      "combat-reward-tokens-1",
      "combat-reward-tokens-2",
      "combat-reward-tokens-3",
      "dungeon-room-ready-3-bomb-reward-safe",
      "bomb-reward-selected-prototype-area",
      "dungeon-room-ready-2-combat-cleared",
      "dungeon-room-ready-4-combat-active",
      "dungeon-room-ready-5-combat-active",
      "dungeon-room-ready-5-combat-cleared",
      "room-ready-prototype-combat-gates",
      "dungeon-room-ready-8-recovery-safe",
      `player-health-recovered-${expectedRestoredHealth}`,
      "recovery-consumed-room-8",
      "dungeon-room-ready-6-boss-antechamber-safe",
      "dungeon-room-ready-7-boss-active",
      "boss-pattern-telegraph",
      "boss-pattern-execute",
      "boss-pattern-recovery",
      "boss-damaged",
      "boss-phase-two",
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
      gatesRoomScreenshotPath,
      recoveryRoomScreenshotPath,
      pauseScreenshotPath,
      runFailureScreenshotPath,
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
      gatesRoomScreenshotPath,
      pauseScreenshotPath,
      runFailureScreenshotPath,
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
