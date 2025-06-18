export default {
  presets: [
    require("@vaguevoid/design-system/preset").default
  ],
  content: [
    "./Web/Page/**/*.{cs,cshtml}",
    "./Web/Pages/**/*.{cs,cshtml}",
  ],
  theme: {
    screens: { // https://www.freecodecamp.org/news/the-100-correct-way-to-do-css-breakpoints-88d6a5ba1862/
      'sm':  '450px',
      'md':  '600px',
      'lg':  '850px',
      'xl':  '1200px',
      '2xl': '1800px'
    },
    extend: {},
  },
  plugins: [],
}
