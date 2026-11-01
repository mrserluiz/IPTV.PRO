@echo off
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
 echo Compilador .NET Framework nao encontrado. Instale o .NET Framework 4.8.
 pause
 exit /b 1
)
"%CSC%" /nologo /target:winexe /out:EtherPlayer.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Core.dll EtherPlayer.cs
if errorlevel 1 (
 echo Falha na compilacao. Envie uma captura desta janela.
 pause
 exit /b 1
)
echo Compilacao concluida. Abrindo EtherPlayer.exe...
start "" EtherPlayer.exe
