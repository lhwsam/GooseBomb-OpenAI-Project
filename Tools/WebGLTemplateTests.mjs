import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const toolsDirectory = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.resolve(toolsDirectory, "..");
const templatePath = path.join(
  projectRoot,
  "Assets",
  "WebGLTemplates",
  "BombSwap",
  "index.html",
);
const templateScopePath = path.join(
  projectRoot,
  "Assets",
  "Game",
  "Editor",
  "BuildAutomation",
  "ResponsiveWebGLTemplateScope.cs",
);
const harnessInteropPath = path.join(
  projectRoot,
  "Assets",
  "Game",
  "Runtime",
  "WebGL",
  "BombSwapHarness.jslib",
);
const mobilePipelinePath = path.join(
  projectRoot,
  "Assets",
  "Settings",
  "Mobile_RPAsset.asset",
);
const qualitySettingsPath = path.join(
  projectRoot,
  "ProjectSettings",
  "QualitySettings.asset",
);

const html = fs.readFileSync(templatePath, "utf8");
const templateScope = fs.readFileSync(templateScopePath, "utf8");
const harnessInterop = fs.readFileSync(harnessInteropPath, "utf8");
const mobilePipeline = fs.readFileSync(mobilePipelinePath, "utf8");
const qualitySettings = fs.readFileSync(qualitySettingsPath, "utf8");

for (const requiredText of [
  'name="viewport"',
  'id="unity-canvas-frame"',
  'id="unity-canvas"',
  "{{{ WIDTH }}}",
  "{{{ HEIGHT }}}",
  "{{{ LOADER_FILENAME }}}",
  "{{{ DATA_FILENAME }}}",
  "{{{ FRAMEWORK_FILENAME }}}",
  "{{{ CODE_FILENAME }}}",
  "#if USE_WASM",
  "#if SYMBOLS_FILENAME",
  "createUnityInstance(canvas, config",
  "function fitCanvasToViewport()",
  "availableWidth / referenceWidth",
  "availableHeight / referenceHeight",
  'window.addEventListener("resize", fitCanvasToViewport)',
  "new ResizeObserver(fitCanvasToViewport).observe(canvasFrame)",
  'id="unity-playtest-log-button"',
  "function exportPlaytestLog()",
  'schemaVersion: "bombswap/playtest-log@1"',
  "events: events.slice()",
  "URL.createObjectURL(blob)",
  "link.download = createPlaytestLogFilename(generatedAt)",
  "globalThis.BombSwapHarnessNotifyEventsAvailable",
]) {
  assert.ok(
    html.includes(requiredText),
    `Responsive WebGL template is missing: ${requiredText}`,
  );
}

assert.ok(
  html.includes("Math.min(\n          1,"),
  "Canvas scaling must never enlarge the native 960x600 render surface.",
);
assert.ok(
  html.includes('overflow: hidden;'),
  "The hosting page must not expose scrollbars around the game canvas.",
);
assert.ok(
  !html.includes('canvas.style.width = "{{{ WIDTH }}}px"'),
  "The template must not restore Unity's fixed desktop canvas width.",
);
assert.ok(
  !html.includes('canvas.style.height = "{{{ HEIGHT }}}px"'),
  "The template must not restore Unity's fixed desktop canvas height.",
);
assert.match(
  qualitySettings,
  /^\s+WebGL:\s+0\s*$/m,
  "WebGL must keep using the Mobile quality profile.",
);
assert.match(
  mobilePipeline,
  /^\s+m_RenderScale:\s+1(?:\.0+)?\s*$/m,
  "The WebGL Mobile pipeline must preserve the native 480x300 pixel-camera target instead of downscaling it again.",
);
assert.match(
  templateScope,
  /PlayerSettings\.WebGL\.template = _previousTemplate;\s+AssetDatabase\.SaveAssets\(\);/,
  "The build scope must persist the caller's original WebGL template setting after the build.",
);
assert.match(
  html,
  /id="unity-playtest-log-button"[\s\S]*?hidden>SAVE TEST LOG<\/button>/,
  "The local playtest export must stay hidden until the development harness reports an event.",
);
assert.ok(
  !html.includes("navigator.sendBeacon"),
  "Playtest evidence must be saved locally instead of transmitted by the template.",
);
for (const requiredInteropText of [
  "__BOMBSWAP_HARNESS_EXPORT_READY__",
  "globalThis.BombSwapHarnessNotifyEventsAvailable",
  'typeof notifyEventsAvailable === "function"',
]) {
  assert.ok(
    harnessInterop.includes(requiredInteropText),
    `WebGL harness interop is missing: ${requiredInteropText}`,
  );
}

process.stdout.write("BOMBSWAP_WEBGL_TEMPLATE_TEST|passed\n");
