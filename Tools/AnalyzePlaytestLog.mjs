import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  analyzePlaytestLog,
  renderPlaytestLogMarkdown,
} from "./PlaytestLogAnalyzer.mjs";

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

export function analyzePlaytestLogFile(inputPath) {
  const absoluteInputPath = path.resolve(inputPath);
  if (!fs.existsSync(absoluteInputPath)) {
    throw new Error(`Playtest log was not found: ${absoluteInputPath}`);
  }
  const payload = JSON.parse(fs.readFileSync(absoluteInputPath, "utf8"));
  return analyzePlaytestLog(payload);
}

export function writePlaytestLogSummary(summary, outputDirectory) {
  const absoluteOutputDirectory = path.resolve(outputDirectory);
  const jsonPath = path.join(
    absoluteOutputDirectory,
    "playtest-log-summary.json",
  );
  const markdownPath = path.join(
    absoluteOutputDirectory,
    "playtest-log-summary.md",
  );
  fs.mkdirSync(absoluteOutputDirectory, { recursive: true });
  fs.writeFileSync(jsonPath, `${JSON.stringify(summary, null, 2)}\n`, "utf8");
  fs.writeFileSync(markdownPath, renderPlaytestLogMarkdown(summary), "utf8");
  return { jsonPath, markdownPath };
}

export function runCli(argv) {
  const args = parseArguments(argv);
  if (!args.input) throw new Error("--input is required.");

  const summary = analyzePlaytestLogFile(args.input);
  if (!args.outputDirectory) {
    process.stdout.write(renderPlaytestLogMarkdown(summary));
    return;
  }

  const outputs = writePlaytestLogSummary(summary, args.outputDirectory);
  process.stdout.write(
    `BOMBSWAP_PLAYTEST_LOG_ANALYSIS|passed|events=${summary.source.eventCount}` +
    `|json=${outputs.jsonPath}|markdown=${outputs.markdownPath}\n`,
  );
}

const isMain = process.argv[1] &&
  path.resolve(process.argv[1]) === path.resolve(fileURLToPath(import.meta.url));
if (isMain) {
  try {
    runCli(process.argv.slice(2));
  } catch (error) {
    process.stderr.write(`${error.stack ?? error}\n`);
    process.exitCode = 1;
  }
}
