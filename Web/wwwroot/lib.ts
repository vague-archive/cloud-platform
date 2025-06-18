import { is } from "./lib/is"
import { to } from "./lib/to"

function slugify(name: string) { // WARNING: keep in sync with server side version in Lib/Format.cs
  return name.toLowerCase().trim()
    .replace(/'s/g, "s")
    .replace(/[^A-Za-zÀ-ÖØ-öø-ÿ0-9-]/g, "-")
    .replace(/-+/g, "-")
    .replace(/-$/, "")
}

export const lib = {
  slugify,
  timeZone: () => Intl.DateTimeFormat().resolvedOptions().timeZone,
  locale: () => navigator.language,
}

declare global {
  let lib: typeof lib
  let is: typeof is
  let to: typeof to
  interface Window {
    lib: typeof lib
    is: typeof is
    to: typeof to
  }
}

window.lib = lib
window.is = is
window.to = to
