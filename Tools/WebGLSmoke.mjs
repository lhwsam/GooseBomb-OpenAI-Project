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
      await page.keyboard.up(key);
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
    checks.push({
      name: "dungeon-start-ready",
      status: "passed",
      detail: "The safe Start placeholder initialized with the reusable loop shell.",
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
    checks.push({
      name: "graph-scene-transition",
      status: "passed",
      detail: "The seed-0 Start exit loaded and committed the assigned pillars combat scene.",
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
      detail: "Six alternating west/north presses each changed motion before release.",
    });

    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      timeout: 5_000,
    });
    await waitForEvent(page, "bomb-exploded", { timeout: 15_000 });
    await moveToCell(page, 3, 3);
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      count: 2,
      timeout: 5_000,
    });
    await moveToCell(page, 3, 0);
    await waitForEvent(page, "bomb-exploded", { count: 2, timeout: 15_000 });
    await waitForEvent(page, "room-cleared", { timeout: 5_000 });
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

    await page.keyboard.press("KeyX");
    await waitForEvent(page, "active-bomb-slot-1", { timeout: 5_000 });
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "place-bomb-definition-prototype-area", {
      timeout: 5_000,
    });
    checks.push({
      name: "bomb-reward-loadout-persistence",
      status: "passed",
      detail: "The selected area bomb remained in slot 2 and placed successfully in room 4.",
    });

    await page.keyboard.press("Escape");
    await page.keyboard.press("Escape");
    await waitForEvent(page, "pause-resume", { timeout: 5_000 });
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.waitForTimeout(250);
    checks.push({ name: "pause-resume-and-resize", status: "passed" });

    fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
    await page.screenshot({ path: screenshotPath });
    checks.push({ name: "screenshot", status: "passed", detail: screenshotPath });

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
      "dungeon-room-ready-3-bomb-reward-safe",
      "bomb-reward-selected-prototype-area",
      "dungeon-room-ready-2-combat-cleared",
      "dungeon-room-ready-4-combat-active",
      "swap-bomb",
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
