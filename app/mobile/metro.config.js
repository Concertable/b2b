// eslint-disable-next-line @typescript-eslint/no-require-imports
const { getDefaultConfig } = require("expo/metro-config");
// eslint-disable-next-line @typescript-eslint/no-require-imports
const { withNativeWind } = require("nativewind/metro");
// eslint-disable-next-line @typescript-eslint/no-require-imports
const path = require("path");

const config = getDefaultConfig(__dirname);

const mobilePackage = path.dirname(require.resolve("@concertable/mobile/package.json"));

config.watchFolders = [mobilePackage];

module.exports = withNativeWind(config, {
  input: require.resolve("@concertable/mobile/global.css"),
  inlineRem: 16,
});
