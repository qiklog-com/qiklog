# QikLog landing live-tell demo. Render with: vhs landing-live.tape
# Output: ../www/public/demos/landing-live.gif
# Records the real ingest path that feeds /embed/tail/demo (same LiveTailPanel as /tail/demo).

Output ../www/public/demos/landing-live.gif

Set Shell "bash"
Set FontSize 14
Set Width 980
Set Height 520
Set Theme "Catppuccin Mocha"
Set Padding 24

Type "# Scene 3 uses the REAL LiveTailPanel via /embed/tail/demo"
Sleep 600ms
Enter
Type "curl -sS -X POST http://localhost:5080/v1/logs \\"
Sleep 350ms
Enter
Type "  -H 'Content-Type: application/json' \\"
Sleep 250ms
Enter
Type "  -d '{\"source\":\"demo\",\"level\":\"info\",\"message\":\"JWT expired 401\"}'"
Sleep 450ms
Enter
Sleep 900ms
Type "# → 202 Accepted; open www scroll scene or /embed/tail/demo"
Sleep 2s
