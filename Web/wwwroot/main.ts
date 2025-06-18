import "@vaguevoid/design-system/components"
import { lib } from "./lib"

const csrfToken = document.querySelector("meta[name='csrf-token']")?.getAttribute("content")

document.body.addEventListener("htmx:configRequest", function (event) {
  event.detail.headers["X-CSRF-Token"] = csrfToken
})

console.log("Welcome to void...")

declare global {
  let lib: typeof lib
  interface Window {
    lib: typeof lib
  }
}

window.lib = lib
