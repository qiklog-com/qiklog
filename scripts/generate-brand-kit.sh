#!/usr/bin/env bash
# Generate favicon / PWA / OG rasters from docs/assets/qiklog-mark.svg
# Requires: rsvg-convert, magick (ImageMagick 7)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ASSETS="$ROOT/docs/assets"
WWW="$ROOT/www/public"
WEB="$ROOT/src/QikLog.Web/wwwroot"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

MARK="$ASSETS/qiklog-mark.svg"
ON_RUST="$ASSETS/qiklog-mark-on-rust.svg"

rsvg-convert -w 16 -h 16 "$MARK" -o "$TMP/16.png"
rsvg-convert -w 32 -h 32 "$MARK" -o "$TMP/32.png"
rsvg-convert -w 48 -h 48 "$MARK" -o "$TMP/48.png"
magick "$TMP/16.png" "$TMP/32.png" "$TMP/48.png" "$TMP/favicon.ico"

rsvg-convert -w 180 -h 180 "$ON_RUST" -o "$TMP/apple-touch-icon.png"
rsvg-convert -w 192 -h 192 "$ON_RUST" -o "$TMP/icon-192.png"
rsvg-convert -w 512 -h 512 "$ON_RUST" -o "$TMP/icon-512.png"

FONT=""
for candidate in \
  /Library/Fonts/BricolageGrotesque-ExtraBold.ttf \
  "$TMP/BricolageGrotesque-ExtraBold.ttf"
do
  if [[ -f "$candidate" ]]; then FONT="$candidate"; break; fi
done
if [[ -z "$FONT" ]]; then
  curl -fsSL "https://github.com/google/fonts/raw/main/ofl/bricolagegrotesque/BricolageGrotesque%5Bopsz%2Cwdth%2Cwght%5D.ttf" \
    -o "$TMP/BricolageGrotesque.ttf" || true
  if [[ -f "$TMP/BricolageGrotesque.ttf" ]]; then FONT="$TMP/BricolageGrotesque.ttf"; fi
fi

rsvg-convert -w 84 -h 84 "$MARK" -o "$TMP/og-mark.png"
magick -size 1200x630 "xc:#FCFAF5" \
  -fill none -stroke "#B94700" -strokewidth 4 \
  -draw "rectangle 20,20 1180,610" \
  "$TMP/og-base.png"

if [[ -n "$FONT" ]]; then
  magick "$TMP/og-base.png" \
    \( "$TMP/og-mark.png" \) -gravity west -geometry +390+0 -composite \
    -font "$FONT" -fill "#2E2A26" -pointsize 72 -gravity west -annotate +500-8 "Qik" \
    -font "$FONT" -fill "#B94700" -pointsize 72 -gravity west -annotate +618-8 "Log" \
    -depth 8 \
    "$TMP/og-image.png"
else
    magick "$TMP/og-base.png" \
    \( "$TMP/og-mark.png" \) -gravity center -geometry -120+0 -composite \
    -depth 8 \
    "$TMP/og-image.png"
fi

copy_kit() {
  local dest="$1"
  mkdir -p "$dest"
  cp "$TMP/favicon.ico" "$dest/favicon.ico"
  cp "$TMP/apple-touch-icon.png" "$dest/apple-touch-icon.png"
  cp "$TMP/icon-192.png" "$dest/icon-192.png"
  cp "$TMP/icon-512.png" "$dest/icon-512.png"
  cp "$TMP/og-image.png" "$dest/og-image.png"
}

copy_kit "$WWW"
copy_kit "$WEB"
cp "$TMP/favicon.ico" "$ASSETS/favicon.ico"
cp "$TMP/apple-touch-icon.png" "$ASSETS/apple-touch-icon.png"
cp "$TMP/icon-192.png" "$ASSETS/icon-192.png"
cp "$TMP/icon-512.png" "$ASSETS/icon-512.png"
cp "$TMP/og-image.png" "$ASSETS/og-image.png"

echo "Wrote brand kit to $WWW, $WEB, and $ASSETS"
identify "$WWW/favicon.ico" "$WWW/apple-touch-icon.png" "$WWW/icon-192.png" "$WWW/icon-512.png" "$WWW/og-image.png"
