#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

// Get the destination directory from the command line arguments, or use the current directory
const destDir = process.argv[2] || process.cwd();

// Path to the source file
const srcFile = path.join(__dirname, "appsettings.json");

// Path to the destination file
const destFile = path.join(destDir, "appsettings.json");

// Copy the file
fs.copyFileSync(srcFile, destFile);

console.log(`Copied appsettings.json to ${destFile}`);