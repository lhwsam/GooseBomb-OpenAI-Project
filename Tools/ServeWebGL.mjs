import { startStaticServer } from "./WebGLStaticServer.mjs";

function parseArguments(argv) {
  const values = {};
  for (let index = 0; index < argv.length; index += 2) {
    const key = argv[index];
    const value = argv[index + 1];
    if (!key?.startsWith("--") || value === undefined) {
      throw new Error(`Invalid argument pair near ${key ?? "<end>"}.`);
    }
    const name = key.slice(2);
    if (Object.hasOwn(values, name)) throw new Error(`Argument --${name} was provided more than once.`);
    values[name] = value;
  }
  return values;
}

function parsePort(value) {
  if (value === undefined) return 8000;
  if (!/^\d+$/.test(value)) throw new Error(`--port must be an integer; received ${value}.`);
  const port = Number(value);
  if (!Number.isSafeInteger(port) || port < 0 || port > 65_535) {
    throw new Error(`--port must be from 0 through 65535; received ${value}.`);
  }
  return port;
}

async function main() {
  const args = parseArguments(process.argv.slice(2));
  if (!args.buildPath) throw new Error("--buildPath is required.");
  const unknownArguments = Object.keys(args).filter((name) => name !== "buildPath" && name !== "port");
  if (unknownArguments.length > 0) {
    throw new Error(`Unknown argument(s): ${unknownArguments.map((name) => `--${name}`).join(", ")}.`);
  }

  const { server, url, rootDirectory } = await startStaticServer(args.buildPath, {
    port: parsePort(args.port),
  });
  process.stdout.write(`BOMBSWAP_WEBGL_SERVER|ready|url=${url}|root=${rootDirectory}\n`);
  process.stdout.write("Press Ctrl+C to stop the local playtest server.\n");

  const closeServer = () => server.close();
  process.once("SIGINT", closeServer);
  process.once("SIGTERM", closeServer);
  await new Promise((resolve, reject) => {
    server.once("close", resolve);
    server.once("error", reject);
  });
}

main().catch((error) => {
  process.stderr.write(`${error.stack ?? error}\n`);
  process.exitCode = 1;
});
