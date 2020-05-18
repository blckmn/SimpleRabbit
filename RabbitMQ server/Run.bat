@echo off
CALL docker build -t myrabbitmq .
CALL docker run -d --rm --name local-rabbit -p 15672:15672 -p 5672:5672 myrabbitmq
Echo Hit any key to stop database
PAUSE >nul
CALL docker stop local-rabbit