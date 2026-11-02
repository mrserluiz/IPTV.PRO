@echo off
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
 echo Compilador .NET Framework nao encontrado. Instale o .NET Framework 4.8.
 pause
 exit /b 1
)
"%CSC%" /nologo /target:winexe /win32icon:assets/app.ico /resource:assets/logo.png,Logo /resource:assets/background.jpg,Background /resource:assets/app.ico,AppIcon /out:XCIPTV-PC.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Core.dll /reference:System.Security.dll XCIPTV-PC.cs EmbeddedVlc.cs NavigationUi.cs Cinema.cs
if errorlevel 1 (
 echo Falha na compilacao. Envie uma captura desta janela.
 pause
 exit /b 1
)
echo Compilacao concluida. Abrindo XCIPTV-PC.exe...
start "" XCIPTV-PC.exe




