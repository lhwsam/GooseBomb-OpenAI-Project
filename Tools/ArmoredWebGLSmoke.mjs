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
    await waitForEvent(page, "armored-moved", { timeout: 15_000 });
    checks.push({
      name: "first-explosion-breaks-armor",
      status: "passed",
      detail: "The first cross-bomb explosion changed Armored to Broken and the enemy moved on the logical grid.",
    });

    await page.keyboard.press("KeyZ");
    await waitForEvent(page, "armored-died", { timeout: 15_000 });
    await waitForEvent(page, "place-bomb-definition-prototype-cross", {
      count: 2,
      timeout: 5_000,
    });
    await waitForEvent(page, "room-cleared", { timeout: 5_000 });
    checks.push({
      name: "second-explosion-kills-broken-enemy",
      status: "passed",
      detail: "A second distinct cross-bomb explosion changed Broken to Dead, removed the final enemy, and cleared the room.",
    });

    fs.mkdirSync(path.dirname(screenshotPath), { recursive: true });
    await page.waitForTimeout(150);
    await page.screenshot({ path: screenshotPath });
    checks.push({ name: "screenshot", status: "passed", detail: screenshotPath });

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
    const diedIndex = eventNames.indexOf("armored-died");
    const clearedIndex = eventNames.indexOf("room-cleared");
    const armoredOrderPassed = brokenIndex >= 0 &&
      diedIndex > brokenIndex &&
      clearedIndex > diedIndex;
    checks.push({
      name: "armored-state-and-clear-order",
      status: armoredOrderPassed ? "passed" : "failed",
      detail: "Expected armored-broken before armored-died before room-cleared after two cross-bomb placements.",
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
