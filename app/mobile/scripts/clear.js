const fs = require("fs");
const path = require("path");
fs.writeFileSync(path.join(__dirname, "..", ".metro-clear"), "");
console.log("Metro cache clear flagged — restart the mobile resource in Aspire.");
