/**
 * The supplied Alloya lockup, used as-is rather than reproduced.
 *
 * An earlier version traced the mark as inline SVG and set the wordmark in
 * Century Gothic, which drifted from the real letterforms and rendered
 * differently depending on the viewer's installed fonts. The source artwork is
 * 400x171, so it has more than enough resolution for a 56px header even on a
 * 2x display - there is nothing to gain from redrawing it.
 */
export function Logo() {
  return (
    <img
      className="logo"
      src="/alloya-fcu-logo.jpg"
      alt="Alloya Corporate Federal Credit Union"
      width={400}
      height={171}
    />
  )
}
