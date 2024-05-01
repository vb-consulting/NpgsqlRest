#!/usr/bin/env node

const { spawn } = require("child_process");
const path = require("path");

// Path to the binary file
const binaryPath = path.join(__dirname, "./.bin/npgsqlrest");

// Arguments passed to the script
const args = process.argv.slice(2);

// Spawn a child process to run the binary file
const child = spawn(binaryPath, args, { stdio: "inherit" });

child.on("error", (error) => {
    console.error(`Failed to start subprocess.\n${error}`);
});

child.on("exit", (code) => {
    process.exit(code);
});