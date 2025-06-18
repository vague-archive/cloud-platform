document.addEventListener("void:game:mounted", (e) => {
  const game = (e as CustomEvent).detail.game
  console.log("void:game:mounted", game)
  checkPasswordEvery(30 * 60 * 1000) // 30 minutes
})

function checkPasswordEvery(frequency: number) {
  setInterval(async () => {
    const result = await fetch(`password-check`)
    if (result.status !== 200) {
      document.location.reload()
    }
  }, frequency)
}
