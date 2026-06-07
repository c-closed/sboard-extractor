@echo
powershell -Command "Start-Process cmd -ArgumentList '/k cd /d %cd% && SboardExtractor.exe --discover > log.txt' -Verb RunAs"
pause