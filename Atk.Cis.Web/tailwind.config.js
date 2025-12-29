module.exports = {
  content: ["./Pages/**/*.cshtml", "./Views/**/*.cshtml"],
  theme: {
    extend: {
      fontFamily: {
        terminal: [
          '"Share Tech Mono"',
          "ui-monospace",
          "SFMono-Regular",
          "Menlo",
          "Monaco",
          "Consolas",
          '"Liberation Mono"',
          '"Courier New"',
          "monospace",
        ],
      },
      colors: {
        terminal: {
          glow: "#00ff9c",
        },
      },
    },
  },
};
