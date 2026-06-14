@echo off
echo Starting WMSPro.Api...
start "WMSPro API" cmd /k "cd src\WMSPro.Api && dotnet run --urls=https://localhost:7000"

echo Starting WMSPro.Mvc...
start "WMSPro MVC" cmd /k "cd src\WMSPro.Mvc && dotnet run --urls=https://localhost:7001"

echo Both applications are starting...
echo API Swagger UI: https://localhost:7000/swagger
echo MVC Web App: https://localhost:7001
pause
