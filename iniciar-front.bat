@echo off
echo ========================================================
echo   Iniciando Frontend de TouringGO (Explorador Lugares)
echo ========================================================
echo Abriendo en tu navegador: http://localhost:3000
start http://localhost:3000
python -m http.server 3000 --directory frontend
pause
