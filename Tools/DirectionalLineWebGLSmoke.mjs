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
  if (explicitPath) return explicitPath;
  const candidates = process.platform === "win32"
    ? [
        path.join(process.env["PROGRAMFILES(X86)"] ?? "", "Microsoft/Edge/Application/msedge.exe"),
        path.join(process.env.PROGRAMFILES ?? "", "Microsoft/Edge/Application/msedge.exe"),
        path.join(process.env.PROGRAMFILES ?? "", "Google/Chrome/Application/chrome.exe"),
        path.join(process.env.LOCALAPPDATA ?? "", "Google/Chrome/Application/chrome.exe"),
      ]
    : [];
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

async function eventNames(page) {
  return page.evaluate(() => {
    const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    return Array.isArray(events)
      ? events.map((event) => typeof event === "string" ? event : event?.name)
      : [];
  });
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

async function triggerBoundaryTransition(page, key, expectedRoomEvent) {
  const transitionsBefore = await eventCount(page, "dungeon-transition-started");
  const commitsBefore = await eventCount(page, "dungeon-room-committed");
  const probesBefore = await eventCount(page, "probe-ready");
  await page.keyboard.down(key);
  try {
    await waitForEvent(page, "dungeon-transition-started", {
      count: transitionsBefore + 1,
      timeout: 30_000,
    });
    await waitForEvent(page, expectedRoomEvent, { timeout: 60_000 });
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

function assertOrdered(names, orderedNames) {
  let cursor = -1;
  for (const name of orderedNames) {
    cursor = names.indexOf(name, cursor + 1);
    if (cursor < 0) {
      throw new Error(
        `Expected ordered marker '${name}' after ${orderedNames.slice(0, orderedNames.indexOf(name)).join(", ")}.`,
      );
    }
  }
}

async function main() {
  const args = parseArguments(process.argv.slice(2));
  if (!args.buildPath || !args.reportPath) {
    throw new Error("--buildPath and --reportPath are required.");
  }
  const buildPath = path.resolve(args.buildPath);
  const reportPath = path.resolve(args.reportPath);
  const screenshotPath = path.resolve(
    args.screenshotPath ?? path.join(path.dirname(reportPath), "directional-line-bomb.png"),
  );
  if (!fs.existsSync(path.join(buildPath, "index.html"))) {
    throw new Error(`WebGL index.html was not found under ${buildPath}.`);
  }

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
    const canvas = page.locator("canvas").first();
    await canvas.click({ position: { x: 20, y: 20 } });
    await waitForEvent(page, "dungeon-room-ready-1-start-safe", { timeout: 120_000 });
    checks.push({ name: "load-and-focus", status: "passed" });

    await moveSteps(page, "ArrowUp", "north", 1);
    await moveSteps(page, "ArrowLeft", "west", 2);
    await moveSteps(page, "ArrowUp", "north", 1);
    await moveSteps(page, "ArrowLeft", "west", 3);
    await moveSteps(page, "ArrowDown", "south", 2);
    await triggerBoundaryTransition(
      page,
      "ArrowLeft",
      "dungeon-room-ready-2-combat-active",
    );

    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "bomb-exploded", { timeout: 15_000 });
    await moveToCell(page, 3, 3);
    await page.keyboard.press("KeyZ");
    await moveToCell(page, 3, 0);
    await waitForEvent(page, "bomb-exploded", { count: 2, timeout: 15_000 });
    await waitForEvent(page, "room-cleared", { timeout: 5_000 });
    checks.push({ name: "first-combat-clear", status: "passed" });

    const combatCell = await getLastPlayerCell(page);
    await moveToCell(page, 3, combatCell.z);
    await moveToCell(page, 3, 5);
    await moveToCell(page, 0, 5);
    await triggerBoundaryTransition(
      page,
      "ArrowUp",
      "dungeon-room-ready-3-bomb-reward-safe",
    );
    await moveToCell(page, 1, -4);
    await moveToCell(page, 1, 0);
    await waitForEvent(page, "bomb-reward-selected-prototype-line", { timeout: 5_000 });
    checks.push({ name: "line-reward-selection", status: "passed" });

    await page.keyboard.press("KeyX");
    await waitForEvent(page, "active-bomb-slot-1", { timeout: 5_000 });
    await page.keyboard.press("ArrowRight");
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "line-bomb-placed-east", { timeout: 5_000 });
    await page.keyboard.press("ArrowUp");
    await waitForEvent(page, "move-direction-north", { timeout: 5_000 });
    fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
    await page.screenshot({ path: screenshotPath });
    await waitForEvent(page, "line-bomb-exploded-east", { timeout: 15_000 });

    const names = await eventNames(page);
    assertOrdered(names, [
      "bomb-reward-selected-prototype-line",
      "active-bomb-slot-1",
      "line-bomb-placed-east",
      "move-direction-north",
      "line-bomb-exploded-east",
    ]);
    checks.push({
      name: "placement-direction-fixed",
      status: "passed",
      detail: "Selected prototype-line, placed east, changed input north, then exploded east.",
    });
    checks.push({
      name: "browser-console",
      status: consoleErrors.length === 0 && pageErrors.length === 0 ? "passed" : "failed",
      detail: { consoleErrors, pageErrors },
    });

    const report = {
      schemaVersion: 1,
      status: checks.every((check) => check.status === "passed") ? "passed" : "failed",
      url,
      checks,
      consoleErrors,
      pageErrors,
      requiredOrderedEvents: [
        "bomb-reward-selected-prototype-line",
        "active-bomb-slot-1",
        "line-bomb-placed-east",
        "move-direction-north",
        "line-bomb-exploded-east",
      ],
      screenshotPath,
      generatedAt: new Date().toISOString(),
    };
    fs.mkdirSync(path.dirname(reportPath), { recursive: true });
    fs.writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");
    if (report.status !== "passed") process.exitCode = 1;
  } catch (error) {
    fs.mkdirSync(path.dirname(reportPath), { recursive: true });
    fs.writeFileSync(reportPath, `${JSON.stringify({
      schemaVersion: 1,
      status: "failed",
      checks,
      consoleErrors,
      pageErrors,
      error: String(error?.stack ?? error),
      generatedAt: new Date().toISOString(),
    }, null, 2)}\n`, "utf8");
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
