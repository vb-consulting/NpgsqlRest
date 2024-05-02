#!/usr/bin/env node

const fs = require("fs");
const os = require("os");

const downloadDir = "../.bin/";
const osType = os.type();

var downloadTo;

if (osType === "Windows_NT") {
    downloadTo = `${downloadDir}npgsqlrest.exe`;
} else {
    downloadTo = `${downloadDir}npgsqlrest`;
}

if (fs.existsSync(downloadTo)) {
    fs.unlinkSync(downloadTo);
}


