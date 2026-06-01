# QikLog quickstart terminal demo — render with: vhs quickstart.tape
# Output: ../www/public/demos/quickstart.gif

Output ../www/public/demos/quickstart.gif

Set Shell "bash"
Set FontSize 14
Set Width 900
Set Height 480
Set Theme "Catppuccin Mocha"
Set Padding 24

Type "curl -X POST http://localhost:5080/v1/logs \\"
Sleep 400ms
Enter
Type "  -H 'Content-Type: application/json' \\"
Sleep 300ms
Enter
Type "  -d '{\"source\":\"demo\",\"level\":\"info\",\"message\":\"shipped\"}'"
Sleep 500ms
Enter
Sleep 800ms
Type "# → 202 Accepted — open http://localhost:5081/tail/demo"
Sleep 2s
