import fs from "node:fs";
import http from "node:http";
import path from "node:path";

export function getContentType(filePath) {
  const uncompressedPath = filePath.replace(/\.(br|gz)$/i, "");
  if (uncompressedPath.toLowerCase().endsWith(".symbols.json")) return "application/json";
  const extension = path.extname(uncompressedPath).toLowerCase();
  return {
    ".html": "text/html; charset=utf-8",
    ".js": "text/javascript; charset=utf-8",
    ".mjs": "text/javascript; charset=utf-8",
    ".css": "text/css; charset=utf-8",
    ".json": "application/json",
    ".wasm": "application/wasm",
    ".data": "application/octet-stream",
    ".png": "image/png",
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".ico": "image/x-icon",
  }[extension] ?? "application/octet-stream";
}

export function validateWebGLBuildRoot(rootDirectory) {
  const normalizedRoot = path.resolve(rootDirectory);
  const indexPath = path.join(normalizedRoot, "index.html");
  if (!fs.existsSync(indexPath) || !fs.statSync(indexPath).isFile()) {
    throw new Error(`WebGL index.html was not found at ${indexPath}.`);
  }
  return normalizedRoot;
}

export function startStaticServer(rootDirectory, { port = 0 } = {}) {
  if (!Number.isInteger(port) || port < 0 || port > 65_535) {
    throw new Error(`Port must be an integer from 0 through 65535; received ${port}.`);
  }

  const normalizedRoot = validateWebGLBuildRoot(rootDirectory);
  const server = http.createServer((request, response) => {
    try {
      if (request.method !== "GET" && request.method !== "HEAD") {
        response.writeHead(405, { Allow: "GET, HEAD" }).end("Method Not Allowed");
        return;
      }

      const requestUrl = new URL(request.url ?? "/", "http://127.0.0.1");
      const relativePath = decodeURIComponent(requestUrl.pathname).replace(/^\/+/, "") || "index.html";
      const resolvedPath = path.resolve(normalizedRoot, relativePath);
      if (!resolvedPath.startsWith(`${normalizedRoot}${path.sep}`) && resolvedPath !== normalizedRoot) {
        response.writeHead(403).end("Forbidden");
        return;
      }

      let filePath = resolvedPath;
      if (fs.existsSync(filePath) && fs.statSync(filePath).isDirectory()) {
        filePath = path.join(filePath, "index.html");
      }
      if (!fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
        if (relativePath === "favicon.ico") {
          response.writeHead(204).end();
          return;
        }
        response.writeHead(404).end("Not found");
        return;
      }

      const headers = {
        "Content-Type": getContentType(filePath),
        "Cache-Control": "no-store",
        "X-Content-Type-Options": "nosniff",
      };
      if (/\.br$/i.test(filePath)) headers["Content-Encoding"] = "br";
      if (/\.gz$/i.test(filePath)) headers["Content-Encoding"] = "gzip";
      response.writeHead(200, headers);
      if (request.method === "HEAD") {
        response.end();
        return;
      }
      const stream = fs.createReadStream(filePath);
      stream.once("error", (error) => response.destroy(error));
      stream.pipe(response);
    } catch (error) {
      response.writeHead(500).end(String(error));
    }
  });

  return new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, "127.0.0.1", () => {
      const address = server.address();
      if (!address || typeof address === "string") {
        server.close();
        reject(new Error("Static server did not expose a TCP port."));
        return;
      }
      resolve({
        server,
        url: `http://127.0.0.1:${address.port}/`,
        rootDirectory: normalizedRoot,
      });
    });
  });
}
