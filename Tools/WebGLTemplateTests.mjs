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

const html = fs.readFileSync(templatePath, "utf8");
const templateScope = fs.readFileSync(templateScopePath, "utf8");

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
  templateScope,
  /PlayerSettings\.WebGL\.template = _previousTemplate;\s+AssetDatabase\.SaveAssets\(\);/,
  "The build scope must persist the caller's original WebGL template setting after the build.",
);

process.stdout.write("BOMBSWAP_WEBGL_TEMPLATE_TEST|passed\n");
