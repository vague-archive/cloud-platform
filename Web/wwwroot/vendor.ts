import htmx from "htmx.org"
import alpine from "alpinejs"
import alpineFocus from "@alpinejs/focus"

alpine.data("script", () => {
  return {
    init() {
      var script = this.$el.querySelector('script:first-child[type="application/json"]')
      var data = script ? JSON.parse(script.textContent) : {}
      Object.keys(data).forEach(key => this[key] = data[key])
    },
  };
});

window.alpine = alpine
window.htmx = htmx

htmx.config.defaultSwapStyle = "outerHTML"

alpine.plugin(alpineFocus);
alpine.start()
