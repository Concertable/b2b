// eslint-disable-next-line @typescript-eslint/no-require-imports
const { getDefaultConfig } = require("expo/metro-config");
// eslint-disable-next-line @typescript-eslint/no-require-imports
const { withNativeWind } = require("nativewind/metro");
// eslint-disable-next-line @typescript-eslint/no-require-imports
const withPackageResolution = require("@concertable/build-config/metro");

const config = withPackageResolution(getDefaultConfig(__dirname), __dirname, [
  "@concertable/mobile",
  "@concertable/shared",
]);

module.exports = withNativeWind(config, {
  input: require.resolve("@concertable/mobile/global.css"),
  inlineRem: 16,
});
