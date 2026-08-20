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
      // Try the bundled runtime next.
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
  if (process.env.BOMBSWAP_BROWSER_PATH) return process.env.BOMBSWAP_BROWSER_PATH;
  const candidates = process.platform === "win32"
    ? [
        path.join(process.env["PROGRAMFILES(X86)"] ?? "", "Microsoft/Edge/Application/msedge.exe"),
        path.join(process.env.PROGRAMFILES ?? "", "Microsoft/Edge/Application/msedge.exe"),
        path.join(process.env.PROGRAMFILES ?? "", "Google/Chrome/Application/chrome.exe"),
      ]
    : ["/usr/bin/microsoft-edge", "/usr/bin/google-chrome", "/usr/bin/chromium"];
  return candidates.find((candidate) => candidate && fs.existsSync(candidate));
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
    args.screenshotPath ?? path.join(path.dirname(reportPath), "armored-webgl.png"),
  );
  const finalScreenshotPath = path.join(
    path.dirname(screenshotPath),
    `${path.basename(screenshotPath, path.extname(screenshotPath))}-final${path.extname(screenshotPath) || ".png"}`,
  );
  const { chromium } = await loadPlaywright();
  const { server, url } = await startStaticServer(buildPath);
  const consoleErrors = [];
  const pageErrors = [];
  const checks = [];
  let browser;

  try {
    const executablePath = resolveBrowserExecutable();
    browser = await chromium.launch({
      headless: true,
      ...(executablePath ? { executablePath } : {}),
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
    const canvas = page.locator("canvas").first();
    await canvas.click({ position: { x: 20, y: 20 } });
    checks.push({ name: "load-and-focus", status: "passed" });

    await waitForEvent(page, "probe-ready", { timeout: 120_000 });
    await waitForEvent(page, "room-ready-prototype-combat-armor", { timeout: 120_000 });
    checks.push({ name: "armored-room-ready", status: "passed" });

    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "armored-broken", { timeout: 15_000 });
    await waitForEvent(page, "armored-panic-telegraph-east-distance-3", {
      timeout: 5_000,
    });
    fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
    await page.screenshot({ path: screenshotPath });
    checks.push({
      name: "first-explosion-locks-panic-branch",
      status: "passed",
      detail: "The first cross-bomb explosion changed Armored to Broken and locked the three-cell east panic branch.",
    });

    await page.keyboard.down("KeyS");
    try {
      await waitForEvent(page, "player-cell-x-0-z--3", { timeout: 5_000 });
    } finally {
      await page.keyboard.up("KeyS");
    }
    await page.keyboard.down("KeyD");
    try {
      await waitForEvent(page, "player-cell-x-3-z--3", { timeout: 5_000 });
    } finally {
      await page.keyboard.up("KeyD");
    }
    await page.keyboard.down("KeyW");
    try {
      await waitForEvent(page, "player-cell-x-3-z--2", { timeout: 5_000 });
    } finally {
      await page.keyboard.up("KeyW");
    }
    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "armored-panic-run-moved", { timeout: 5_000 });
    await waitForEvent(page, "armored-panic-recover", { timeout: 5_000 });
    await waitForEvent(page, "armored-chase", { timeout: 5_000 });
    await waitForEvent(page, "armored-died", { timeout: 15_000 });
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      count: 2,
      timeout: 5_000,
    });
    checks.push({
      name: "second-explosion-kills-broken-enemy",
      status: "passed",
      detail: "After moving to the predicted east branch, a second cross bomb hit the recovered chase position and killed the armored enemy.",
    });

    await page.waitForTimeout(150);
    await page.screenshot({ path: finalScreenshotPath });
    checks.push({
      name: "screenshots",
      status: "passed",
      detail: { panicTelegraph: screenshotPath, final: finalScreenshotPath },
    });

    checks.push({
      name: "browser-console",
      status: consoleErrors.length === 0 && pageErrors.length === 0 ? "passed" : "failed",
      detail: { consoleErrors, pageErrors },
    });

    const harnessEvents = await page.evaluate(() =>
      globalThis.__BOMBSWAP_HARNESS_EVENTS__ ?? null);
    const eventNames = Array.isArray(harnessEvents)
      ? harnessEvents.map((event) => typeof event === "string" ? event : event?.name)
      : [];
    const brokenIndex = eventNames.indexOf("armored-broken");
    const telegraphIndex = eventNames.indexOf("armored-panic-telegraph-east-distance-3");
    const panicRunIndex = eventNames.indexOf("armored-panic-run-moved");
    const panicRecoverIndex = eventNames.indexOf("armored-panic-recover");
    const chaseIndex = eventNames.indexOf("armored-chase");
    const secondPlacementIndex = eventNames.indexOf(
      "place-bomb-definition-prototype-cross",
      eventNames.indexOf("place-bomb-definition-prototype-cross") + 1,
    );
    const diedIndex = eventNames.indexOf("armored-died");
    const armoredOrderPassed = brokenIndex >= 0 &&
      telegraphIndex > brokenIndex &&
      panicRunIndex > telegraphIndex &&
      panicRecoverIndex > panicRunIndex &&
      chaseIndex > panicRecoverIndex &&
      secondPlacementIndex > telegraphIndex &&
      diedIndex > chaseIndex &&
      diedIndex > secondPlacementIndex;
    checks.push({
      name: "armored-state-and-bomb-order",
      status: armoredOrderPassed ? "passed" : "failed",
      detail: "Expected Broken → east Telegraph → PanicRun → Recover → Chase and a repositioned second bomb before armored Dead.",
    });
    const failedChecks = checks.filter((check) => check.status !== "passed");
    const report = {
      schemaVersion: 1,
      status: failedChecks.length === 0 ? "passed" : "failed",
      url,
      checks,
      consoleErrors,
      pageErrors,
      harnessEvents,
      screenshotPath,
      finalScreenshotPath,
      generatedAt: new Date().toISOString(),
    };
    fs.mkdirSync(path.dirname(reportPath), { recursive: true });
    fs.writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");
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
