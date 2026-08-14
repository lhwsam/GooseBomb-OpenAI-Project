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
  const candidates = ["playwright", "playwright-core"];
  for (const candidate of candidates) {
    try {
      return await import(candidate);
    } catch {
      // Try the next package name.
    }
  }

  const runtimeModules = process.env.CODEX_NODE_MODULES;
  if (runtimeModules) {
    for (const candidate of candidates) {
      const modulePath = path.join(runtimeModules, candidate, "index.mjs");
      if (fs.existsSync(modulePath)) return await import(pathToFileURL(modulePath).href);
    }
  }

  throw new Error("Playwright is unavailable. Install playwright or set CODEX_NODE_MODULES to a runtime containing it.");
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

async function main() {
  const args = parseArguments(process.argv.slice(2));
  if (!args.buildPath || !args.reportPath) {
    throw new Error("--buildPath and --reportPath are required.");
  }

  const buildPath = path.resolve(args.buildPath);
  const indexPath = path.join(buildPath, "index.html");
  if (!fs.existsSync(indexPath)) throw new Error(`WebGL index.html was not found at ${indexPath}.`);

  const { chromium } = await loadPlaywright();
  const browserExecutable = resolveBrowserExecutable();
  const { server, url } = await startStaticServer(buildPath);
  const consoleErrors = [];
  const pageErrors = [];
  const checks = [];
  const screenshotPath = path.resolve(
    args.screenshotPath ??
      path.join(path.dirname(path.resolve(args.reportPath)), "webgl-gameplay.png"),
  );
  const wallScreenshotPath = path.resolve(
    args.wallScreenshotPath ??
      path.join(path.dirname(path.resolve(args.reportPath)), "webgl-destructible-walls.png"),
  );
  let browser;

  try {
    browser = await chromium.launch({
      headless: true,
      ...(browserExecutable ? { executablePath: browserExecutable } : {}),
    });
    const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
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
    checks.push({ name: "canvas-focus", status: focusedTag === "CANVAS" ? "passed" : "failed", detail: focusedTag });

    try {
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) === "probe-ready");
      }, undefined, { timeout: 120_000 });
      checks.push({ name: "gameplay-probe-ready", status: "passed" });
    } catch (error) {
      checks.push({
        name: "gameplay-probe-ready",
        status: "failed",
        detail: `Timed out waiting for Unity runtime readiness: ${error}`,
      });
    }

    let moveObserved = false;
    let moveWaitError = null;
    await page.keyboard.down("KeyW");
    try {
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) === "move");
      }, undefined, { timeout: 30_000 });
      moveObserved = true;
    } catch (error) {
      moveWaitError = String(error);
    } finally {
      await page.keyboard.up("KeyW");
    }
    await page.keyboard.press("KeyZ");

    let contactObserved = false;
    let contactWaitError = null;
    try {
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) === "player-contact-damaged");
      }, undefined, { timeout: 30_000 });
      contactObserved = true;
    } catch (error) {
      contactWaitError = String(error);
    }

    let contactEscapeObserved = false;
    let contactEscapeWaitError = null;
    await page.keyboard.down("KeyA");
    try {
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) === "contact-escape-moved");
      }, undefined, { timeout: 30_000 });
      contactEscapeObserved = true;
    } catch (error) {
      contactEscapeWaitError = String(error);
    } finally {
      await page.keyboard.up("KeyA");
    }

    let firstBombExplosionObserved = false;
    let firstBombExplosionWaitError = null;
    try {
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) === "bomb-exploded");
      }, undefined, { timeout: 30_000 });
      firstBombExplosionObserved = true;
    } catch (error) {
      firstBombExplosionWaitError = String(error);
    }

    await page.keyboard.press("KeyX");
    let secondSlotObserved = false;
    let secondSlotWaitError = null;
    try {
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) === "active-bomb-slot-1");
      }, undefined, { timeout: 5_000 });
      await page.keyboard.press("KeyZ");
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) ===
            "place-bomb-definition-prototype-area");
      }, undefined, { timeout: 5_000 });
      secondSlotObserved = true;
    } catch (error) {
      secondSlotWaitError = String(error);
    }
    for (const key of ["Escape", "Escape"]) {
      await page.keyboard.press(key);
    }
    checks.push({
      name: "keyboard-input",
      status: moveObserved && contactObserved && contactEscapeObserved &&
        firstBombExplosionObserved && secondSlotObserved ? "passed" : "failed",
      detail: moveObserved && contactObserved && contactEscapeObserved &&
        firstBombExplosionObserved && secondSlotObserved
        ? "W moved, Z placed the cross definition, A escaped contact, X changed the Core slot, and Z placed the area definition before pause/resume"
        : {
            moveWaitError,
            contactWaitError,
            contactEscapeWaitError,
            firstBombExplosionWaitError,
            secondSlotWaitError,
          },
    });

    try {
      fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
      await page.waitForTimeout(100);
      await page.screenshot({ path: screenshotPath });
      checks.push({
        name: "gameplay-screenshot",
        status: "passed",
        detail: screenshotPath,
      });
    } catch (error) {
      checks.push({
        name: "gameplay-screenshot",
        status: "failed",
        detail: String(error),
      });
    }

    let roomSequenceObserved = false;
    let roomSequenceWaitError = null;
    try {
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        if (!Array.isArray(events)) return false;
        const names = events.map((event) => typeof event === "string" ? event : event?.name);
        return names.filter((name) => name === "room-transition-started").length >= 1 &&
          names.includes("room-ready-prototype-combat-lanes");
      }, undefined, { timeout: 60_000 });

      try {
        fs.mkdirSync(path.dirname(wallScreenshotPath), { recursive: true });
        await page.waitForTimeout(100);
        await page.screenshot({ path: wallScreenshotPath });
        checks.push({
          name: "destructible-wall-screenshot",
          status: "passed",
          detail: wallScreenshotPath,
        });
      } catch (error) {
        checks.push({
          name: "destructible-wall-screenshot",
          status: "failed",
          detail: String(error),
        });
      }

      await canvas.click({ position: { x: 20, y: 20 } });
      await page.keyboard.press("KeyX");
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        if (!Array.isArray(events)) return false;
        const names = events.map((event) => typeof event === "string" ? event : event?.name);
        return names.filter((name) => name === "active-bomb-slot-1").length >= 2;
      }, undefined, { timeout: 5_000 });
      await page.keyboard.press("KeyZ");
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.some((event) =>
          (typeof event === "string" ? event : event?.name) ===
            "destructible-wall-destroyed");
      }, undefined, { timeout: 30_000 });
      await page.waitForFunction(() => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        if (!Array.isArray(events)) return false;
        const names = events.map((event) => typeof event === "string" ? event : event?.name);
        return names.filter((name) => name === "room-transition-started").length >= 2 &&
          names.includes("room-ready-prototype-combat-pillars");
      }, undefined, { timeout: 60_000 });
      roomSequenceObserved = true;
    } catch (error) {
      roomSequenceWaitError = String(error);
    }
    checks.push({
      name: "three-room-sequence",
      status: roomSequenceObserved ? "passed" : "failed",
      detail: roomSequenceObserved
        ? "Cleared the loop and lanes rooms, then loaded the final pillars room in one browser session"
        : roomSequenceWaitError,
    });

    let frameResponsiveTurnsObserved = false;
    let frameResponsiveTurnsWaitError = null;
    const turnEventStartIndex = await page.evaluate(() =>
      Array.isArray(globalThis.__BOMBSWAP_HARNESS_EVENTS__)
        ? globalThis.__BOMBSWAP_HARNESS_EVENTS__.length
        : 0);
    try {
      const rapidDirections = [
        ["ArrowUp", "move-motion-direction-north"],
        ["ArrowRight", "move-motion-direction-east"],
        ["ArrowUp", "move-motion-direction-north"],
        ["ArrowRight", "move-motion-direction-east"],
        ["ArrowUp", "move-motion-direction-north"],
        ["ArrowRight", "move-motion-direction-east"],
      ];
      const expectedMotionEvents = [];
      for (const [key, motionEvent] of rapidDirections) {
        expectedMotionEvents.push(motionEvent);
        await page.keyboard.down(key);
        await page.waitForFunction(({ startIndex, expected }) => {
          const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
          if (!Array.isArray(events)) return false;
          const names = events
            .slice(startIndex)
            .map((event) => typeof event === "string" ? event : event?.name);
          let expectedIndex = 0;
          for (const name of names) {
            if (name === expected[expectedIndex]) expectedIndex++;
            if (expectedIndex === expected.length) return true;
          }
          return false;
        }, { startIndex: turnEventStartIndex, expected: expectedMotionEvents }, { timeout: 2_000 });
        await page.keyboard.up(key);
      }
      frameResponsiveTurnsObserved = true;
    } catch (error) {
      frameResponsiveTurnsWaitError = String(error);
    } finally {
      await page.keyboard.up("ArrowRight");
      await page.keyboard.up("ArrowUp");
    }
    checks.push({
      name: "frame-responsive-cardinal-turns",
      status: frameResponsiveTurnsObserved ? "passed" : "failed",
      detail: frameResponsiveTurnsObserved
        ? "Six alternating north/east taps each produced motion before release"
        : frameResponsiveTurnsWaitError,
    });

    let finalRoomExplosionDamageObserved = false;
    let finalRoomExplosionDamageWaitError = null;
    const finalExplosionEventStartIndex = await page.evaluate(() =>
      Array.isArray(globalThis.__BOMBSWAP_HARNESS_EVENTS__)
        ? globalThis.__BOMBSWAP_HARNESS_EVENTS__.length
        : 0);
    await page.keyboard.press("KeyZ");
    try {
      await page.waitForFunction((startIndex) => {
        const events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
        return Array.isArray(events) && events.slice(startIndex).some((event) =>
          (typeof event === "string" ? event : event?.name) === "player-explosion-damaged");
      }, finalExplosionEventStartIndex, { timeout: 10_000 });
      finalRoomExplosionDamageObserved = true;
    } catch (error) {
      finalRoomExplosionDamageWaitError = String(error);
    }
    checks.push({
      name: "final-room-self-explosion",
      status: finalRoomExplosionDamageObserved ? "passed" : "failed",
      detail: finalRoomExplosionDamageObserved
        ? "A final-room bomb damaged the stationary player after contact invulnerability was no longer timing-coupled to the first-room fuse"
        : finalRoomExplosionDamageWaitError,
    });

    await page.setViewportSize({ width: 1024, height: 768 });
    await page.waitForTimeout(250);
    checks.push({ name: "resize", status: "passed" });

    const requiredGameplayEvents = ["probe-ready", "room-ready-prototype-combat-loop", "move", "move-direction-north", "move-direction-east", "move-motion-direction-north", "move-motion-direction-east", "move-step-direction-north", "chaser-moved", "place-bomb", "place-bomb-definition-prototype-cross", "active-bomb-slot-1", "place-bomb-definition-prototype-area", "destructible-wall-destroyed", "player-contact-damaged", "contact-escape-moved", "bomb-exploded", "player-damaged", "player-explosion-damaged", "enemy-died", "room-cleared", "room-transition-started", "room-ready-prototype-combat-lanes", "room-ready-prototype-combat-pillars", "swap-bomb", "pause-resume", "audio-unlocked"];
    let harnessEvents = null;
    let missingEvents = requiredGameplayEvents;
    const probeDeadline = Date.now() + 10_000;
    do {
      harnessEvents = await page.evaluate(() => globalThis.__BOMBSWAP_HARNESS_EVENTS__ ?? null);
      const observedEventNames = new Set(
        Array.isArray(harnessEvents)
          ? harnessEvents
            .map((event) => typeof event === "string" ? event : event?.name)
            .filter((name) => typeof name === "string")
          : [],
      );
      missingEvents = requiredGameplayEvents.filter((eventName) => !observedEventNames.has(eventName));
      if (missingEvents.length === 0) break;
      await page.waitForTimeout(250);
    } while (Date.now() < probeDeadline);

    if (Array.isArray(harnessEvents)) {
      const observedEventNames = new Set(
        harnessEvents
          .map((event) => typeof event === "string" ? event : event?.name)
          .filter((name) => typeof name === "string"),
      );
      checks.push({
        name: "gameplay-probe",
        status: missingEvents.length === 0 ? "passed" : "failed",
        detail: missingEvents.length === 0
          ? { required: requiredGameplayEvents, observed: [...observedEventNames] }
          : { missing: missingEvents, observed: [...observedEventNames] },
      });
    } else {
      checks.push({ name: "gameplay-probe", status: "failed", detail: "No __BOMBSWAP_HARNESS_EVENTS__ bridge. Add it with the first playable vertical slice." });
    }

    if (consoleErrors.length > 0 || pageErrors.length > 0) {
      checks.push({ name: "browser-console", status: "failed", detail: { consoleErrors, pageErrors } });
    } else {
      checks.push({ name: "browser-console", status: "passed" });
    }

    const failedChecks = checks.filter((check) => check.status !== "passed");
    const report = {
      schemaVersion: 1,
      status: failedChecks.length === 0 ? "passed" : "failed",
      url,
      checks,
      consoleErrors,
      pageErrors,
      screenshotPath,
      wallScreenshotPath,
      generatedAt: new Date().toISOString(),
    };
    fs.mkdirSync(path.dirname(path.resolve(args.reportPath)), { recursive: true });
    fs.writeFileSync(path.resolve(args.reportPath), `${JSON.stringify(report, null, 2)}\n`, "utf8");
    if (failedChecks.length > 0) process.exitCode = 1;
  } finally {
    if (browser) await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
}

main().catch((error) => {
  process.stderr.write(`${error.stack ?? error}\n`);
  process.exitCode = 1;
});
