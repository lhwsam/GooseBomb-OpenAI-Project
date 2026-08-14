import assert from "node:assert/strict";
import fs from "node:fs";
import http from "node:http";
import os from "node:os";
import path from "node:path";
import { startStaticServer, validateWebGLBuildRoot } from "./WebGLStaticServer.mjs";

function request(port, requestPath, method = "GET") {
  return new Promise((resolve, reject) => {
    const operation = http.request({
      host: "127.0.0.1",
      port,
      path: requestPath,
      method,
    }, (response) => {
      const chunks = [];
      response.on("data", (chunk) => chunks.push(chunk));
      response.on("end", () => resolve({
        body: Buffer.concat(chunks),
        headers: response.headers,
        statusCode: response.statusCode,
      }));
    });
    operation.once("error", reject);
    operation.end();
  });
}

async function main() {
  const fixtureRoot = fs.mkdtempSync(path.join(os.tmpdir(), "bombswap-webgl-server-"));
  const buildDirectory = path.join(fixtureRoot, "Build");
  fs.mkdirSync(buildDirectory);
  fs.writeFileSync(path.join(fixtureRoot, "index.html"), "<!doctype html><title>BombSwap</title>");
  fs.writeFileSync(path.join(buildDirectory, "game.framework.js"), "export default true;");
  fs.writeFileSync(path.join(buildDirectory, "game.data"), "data");
  fs.writeFileSync(path.join(buildDirectory, "game.wasm"), "wasm");
  fs.writeFileSync(path.join(buildDirectory, "game.wasm.br"), "compressed-wasm");
  fs.writeFileSync(path.join(buildDirectory, "game.data.gz"), "compressed-data");
  fs.writeFileSync(path.join(buildDirectory, "game.symbols.json"), "{}");

  let server;
  try {
    assert.equal(validateWebGLBuildRoot(fixtureRoot), path.resolve(fixtureRoot));
    assert.throws(
      () => validateWebGLBuildRoot(path.join(fixtureRoot, "missing")),
      /index\.html was not found/,
    );

    const started = await startStaticServer(fixtureRoot, { port: 0 });
    server = started.server;
    const port = new URL(started.url).port;

    const index = await request(port, "/");
    assert.equal(index.statusCode, 200);
    assert.equal(index.headers["content-type"], "text/html; charset=utf-8");
    assert.equal(index.headers["cache-control"], "no-store");
    assert.equal(index.headers["x-content-type-options"], "nosniff");
    assert.match(index.body.toString("utf8"), /BombSwap/);

    const wasm = await request(port, "/Build/game.wasm", "HEAD");
    assert.equal(wasm.statusCode, 200);
    assert.equal(wasm.headers["content-type"], "application/wasm");
    assert.equal(wasm.body.length, 0);

    const compressedWasm = await request(port, "/Build/game.wasm.br", "HEAD");
    assert.equal(compressedWasm.statusCode, 200);
    assert.equal(compressedWasm.headers["content-type"], "application/wasm");
    assert.equal(compressedWasm.headers["content-encoding"], "br");

    const data = await request(port, "/Build/game.data", "HEAD");
    assert.equal(data.headers["content-type"], "application/octet-stream");

    const compressedData = await request(port, "/Build/game.data.gz", "HEAD");
    assert.equal(compressedData.headers["content-type"], "application/octet-stream");
    assert.equal(compressedData.headers["content-encoding"], "gzip");

    const symbols = await request(port, "/Build/game.symbols.json", "HEAD");
    assert.equal(symbols.headers["content-type"], "application/json");

    assert.equal((await request(port, "/..%2foutside.txt")).statusCode, 403);
    assert.equal((await request(port, "/missing.txt")).statusCode, 404);
    assert.equal((await request(port, "/", "POST")).statusCode, 405);

    process.stdout.write("BOMBSWAP_WEBGL_STATIC_SERVER_TEST|passed\n");
  } finally {
    if (server) {
      await new Promise((resolve) => server.close(resolve));
    }
    fs.rmSync(fixtureRoot, { recursive: true, force: true });
  }
}

main().catch((error) => {
  process.stderr.write(`${error.stack ?? error}\n`);
  process.exitCode = 1;
});
