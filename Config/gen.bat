@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"
set "LUBAN=%SCRIPT_DIR%LubanDll\Luban.dll"
set "CONF=%SCRIPT_DIR%DataTables\luban.conf"
set "CLIENT_ASSETS=%REPO_ROOT%\Client\GameTopRouge\Assets\AAAGame"
set "SERVER_CONFIG=%REPO_ROOT%\Server\Hotfix\Config"
set "DOTNET_ROLL_FORWARD=Major"

pushd "%SCRIPT_DIR%"
if errorlevel 1 exit /b 1

echo Generating client Luban tables...
dotnet "%LUBAN%" -t client -c cs-bin -d bin --conf "%CONF%" ^
    -x "outputCodeDir=%CLIENT_ASSETS%\ScriptsHotfix\DataTable" ^
    -x "outputDataDir=%CLIENT_ASSETS%\DataTable"
if errorlevel 1 goto :failed

echo Generating server Luban tables...
dotnet "%LUBAN%" -t server -c cs-bin -d bin --conf "%CONF%" ^
    -x "outputCodeDir=%SERVER_CONFIG%\Code" ^
    -x "outputDataDir=%SERVER_CONFIG%\Data"
if errorlevel 1 goto :failed

python "%SCRIPT_DIR%validate_dialogue.py"
if errorlevel 1 goto :failed
python "%SCRIPT_DIR%validate_quest.py"
if errorlevel 1 goto :failed

popd
echo Luban client and server tables generated successfully.
exit /b 0

:failed
set "EXIT_CODE=%ERRORLEVEL%"
popd
echo Luban generation failed with exit code %EXIT_CODE%.
exit /b %EXIT_CODE%
