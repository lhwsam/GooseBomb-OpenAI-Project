import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { startStaticServer } from "./WebGLStaticServer.mjs";

const StandardButton = Object.freeze({
  South: 0,
  West: 2,
  Select: 8,
  Start: 9,
  DpadUp: 12,
});

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
    : ["/usr/bin/microsoft-edge", "/usr/bin/google-chrome", "/usr/bin/chromium"];
  return candidates.find((candidate) => candidate && fs.existsSync(candidate));
}

async function installVirtualGamepad(page) {
  await page.addInitScript(() => {
    const makeButton = () => ({ pressed: false, touched: false, value: 0 });
    const gamepad = {
      axes: [0, 0, 0, 0],
      buttons: Array.from({ length: 16 }, makeButton),
      connected: false,
      id: "BombSwap Virtual Standard Gamepad",
      index: 0,
      mapping: "standard",
      timestamp: 0,
    };

    const touch = () => {
      gamepad.timestamp = performance.now();
    };
    const api = {
      connect() {
        if (gamepad.connected) return;
        gamepad.connected = true;
        touch();
        const event = new Event("gamepadconnected");
        Object.defineProperty(event, "gamepad", { value: gamepad });
        globalThis.dispatchEvent(event);
      },
      reset() {
        gamepad.axes.fill(0);
        for (const button of gamepad.buttons) {
          button.pressed = false;
          button.touched = false;
          button.value = 0;
        }
        touch();
      },
      setAxis(index, value) {
        if (!Number.isInteger(index) || index < 0 || index >= gamepad.axes.length) {
          throw new RangeError(`Invalid virtual gamepad axis ${index}.`);
        }
        gamepad.axes[index] = Number(value);
        touch();
      },
      setButton(index, value) {
        if (!Number.isInteger(index) || index < 0 || index >= gamepad.buttons.length) {
          throw new RangeError(`Invalid virtual gamepad button ${index}.`);
        }
        const numericValue = Number(value);
        const button = gamepad.buttons[index];
        button.value = numericValue;
        button.pressed = numericValue >= 0.5;
        button.touched = numericValue !== 0;
        touch();
      },
      snapshot() {
        return {
          axes: [...gamepad.axes],
          buttons: gamepad.buttons.map((button) => ({ ...button })),
          connected: gamepad.connected,
          id: gamepad.id,
          index: gamepad.index,
          mapping: gamepad.mapping,
          timestamp: gamepad.timestamp,
        };
      },
    };

    Object.defineProperty(navigator, "getGamepads", {
      configurable: true,
      value: () => gamepad.connected ? [gamepad] : [],
    });
    globalThis.__BOMBSWAP_VIRTUAL_GAMEPAD__ = api;
  });
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

async function setAxis(page, index, value) {
  await page.evaluate(({ axisIndex, axisValue }) => {
    globalThis.__BOMBSWAP_VIRTUAL_GAMEPAD__.setAxis(axisIndex, axisValue);
  }, { axisIndex: index, axisValue: value });
}

async function setButton(page, index, value) {
  await page.evaluate(({ buttonIndex, buttonValue }) => {
    globalThis.__BOMBSWAP_VIRTUAL_GAMEPAD__.setButton(buttonIndex, buttonValue);
  }, { buttonIndex: index, buttonValue: value });
}

async function verifyMoveInput(page, applyInput, releaseInput, direction) {
  const directionEvent = `move-direction-${direction}`;
  const directionBefore = await eventCount(page, directionEvent);
  const noneBefore = await eventCount(page, "move-direction-none");
  await applyInput();
  await waitForEvent(page, directionEvent, {
    count: directionBefore + 1,
    timeout: 5_000,
  });
  await releaseInput();
  await waitForEvent(page, "move-direction-none", {
    count: noneBefore + 1,
    timeout: 5_000,
  });
}

async function main() {
  const args = parseArguments(process.argv.slice(2));
  if (!args.buildPath || !args.reportPath) {
    throw new Error("--buildPath and --reportPath are required.");
  }
  const buildPath = path.resolve(args.buildPath);
  const reportPath = path.resolve(args.reportPath);
  const screenshotPath = path.resolve(
    args.screenshotPath ?? path.join(path.dirname(reportPath), "gamepad-paused.png"),
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
    await installVirtualGamepad(page);
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
    await waitForEvent(page, "probe-ready", { timeout: 120_000 });
    await waitForEvent(page, "dungeon-room-ready-1-start-safe", { timeout: 120_000 });
    checks.push({ name: "load-and-focus", status: "passed" });

    await page.evaluate(() => globalThis.__BOMBSWAP_VIRTUAL_GAMEPAD__.connect());
    await page.waitForTimeout(250);

    const virtualGamepad = await page.evaluate(() => {
      const pads = navigator.getGamepads();
      return {
        count: pads.length,
        first: globalThis.__BOMBSWAP_VIRTUAL_GAMEPAD__.snapshot(),
      };
    });
    if (virtualGamepad.count !== 1 ||
        virtualGamepad.first.mapping !== "standard" ||
        virtualGamepad.first.axes.length !== 4 ||
        virtualGamepad.first.buttons.length !== 16) {
      throw new Error(`Unexpected virtual gamepad shape: ${JSON.stringify(virtualGamepad)}.`);
    }
    checks.push({
      name: "virtual-standard-gamepad",
      status: "passed",
      detail: virtualGamepad,
    });

    await verifyMoveInput(
      page,
      () => setAxis(page, 0, 1),
      () => setAxis(page, 0, 0),
      "east",
    );
    checks.push({
      name: "left-stick-move-release",
      status: "passed",
      detail: "Standard axis 0 emitted Move(East), then neutral emitted Move(None).",
    });

    await verifyMoveInput(
      page,
      () => setButton(page, StandardButton.DpadUp, 1),
      () => setButton(page, StandardButton.DpadUp, 0),
      "north",
    );
    checks.push({
      name: "dpad-move-release",
      status: "passed",
      detail: "Standard button 12 emitted Move(North), then release emitted Move(None).",
    });

    const swapsBefore = await eventCount(page, "swap-bomb");
    await setButton(page, StandardButton.West, 1);
    await waitForEvent(page, "swap-bomb", { count: swapsBefore + 1, timeout: 5_000 });
    await setButton(page, StandardButton.West, 0);
    await page.waitForTimeout(100);
    checks.push({
      name: "west-button-swap-command",
      status: "passed",
      detail: "Standard button 2 reached the SwapBomb command probe.",
    });

    const pauseEnteredBefore = await eventCount(page, "pause-entered");
    await setButton(page, StandardButton.Start, 1);
    await waitForEvent(page, "pause-entered", {
      count: pauseEnteredBefore + 1,
      timeout: 5_000,
    });
    await setButton(page, StandardButton.Start, 0);
    await page.waitForTimeout(100);
    fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
    await page.screenshot({ path: screenshotPath });

    const pauseResumedBefore = await eventCount(page, "pause-resumed");
    await setButton(page, StandardButton.Start, 1);
    await waitForEvent(page, "pause-resumed", {
      count: pauseResumedBefore + 1,
      timeout: 5_000,
    });
    await setButton(page, StandardButton.Start, 0);
    await page.waitForTimeout(100);
    checks.push({
      name: "start-button-pause-resume",
      status: "passed",
      detail: "Standard button 9 entered and resumed the authoritative session pause state.",
    });

    for (let hit = 0; hit < 5; hit++) {
      const placementsBefore = await eventCount(
        page,
        "place-bomb-definition-prototype-cross",
      );
      const explosionsBefore = await eventCount(page, "bomb-exploded");
      await setButton(page, StandardButton.South, 1);
      await waitForEvent(page, "place-bomb-definition-prototype-cross", {
        count: placementsBefore + 1,
        timeout: 5_000,
      });
      await setButton(page, StandardButton.South, 0);
      await page.waitForTimeout(100);
      await waitForEvent(page, "bomb-exploded", {
        count: explosionsBefore + 1,
        timeout: 5_000,
      });
    }
    checks.push({
      name: "south-button-place-bomb",
      status: "passed",
      detail: "Standard button 0 placed and exploded five prototype-cross bombs through Core.",
    });

    await waitForEvent(page, "player-died", { timeout: 5_000 });
    await waitForEvent(page, "run-failed", { timeout: 5_000 });
    await waitForEvent(page, "run-failed-cause-bomb-explosion", { timeout: 5_000 });
    checks.push({
      name: "gamepad-self-damage-run-failure",
      status: "passed",
      detail: "Five South-button self explosions produced the authoritative bomb-explosion run failure.",
    });

    const restartRequestsBefore = await eventCount(page, "run-restart-requested");
    const restartsBefore = await eventCount(page, "dungeon-run-restarted");
    const startReadyBefore = await eventCount(page, "dungeon-room-ready-1-start-safe");
    await setButton(page, StandardButton.Select, 1);
    await waitForEvent(page, "run-restart-requested", {
      count: restartRequestsBefore + 1,
      timeout: 5_000,
    });
    await setButton(page, StandardButton.Select, 0);
    await waitForEvent(page, "dungeon-run-restarted", {
      count: restartsBefore + 1,
      timeout: 5_000,
    });
    await waitForEvent(page, "dungeon-room-ready-1-start-safe", {
      count: startReadyBefore + 1,
      timeout: 20_000,
    });
    checks.push({
      name: "select-button-failed-run-restart",
      status: "passed",
      detail: "Standard button 8 restarted the failed run into a fresh safe start room without reloading the page.",
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
      browserVersion: browser.version(),
      deviceSource: "Playwright init-script override of navigator.getGamepads(); not a physical controller",
      checks,
      consoleErrors,
      pageErrors,
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
