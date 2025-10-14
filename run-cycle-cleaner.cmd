@echo off
echo Starting FaCT Cycle Cleaner...
echo Current time: %date% %time%

REM Change to the application directory
cd /d "E:\source\FaCT-CycleClean"

REM Run the application with --no-wait to prevent hanging
dotnet run --no-wait

echo.
echo FaCT Cycle Cleaner completed at: %date% %time%
echo Exit code: %ERRORLEVEL%

REM Optional: Add a brief pause to see the completion message
timeout /t 3 /nobreak > nul

REM
REM  This command runs the CycleClean C# console command.  This .exe runs a series of CFW posted transactions
REM  to remove old, abandoned policies from cycle processing.  This is done by deleting the RB module, and setting the
REM  underlying policy to RNT; Renewal Not Taken.
REM

echo Running cycleClean > E:\moe\cycleClean\logs\logFile.txt
echo %DATE% >> E:\moe\cycleClean\logs\logFile.txt
echo %TIME% >> E:\moe\cycleClean\logs\logFile.txt

E:
cd E:\moe\cycleClean\bin
Fact-CycleClean.exe --no-wait
if errorlevel 1 exit/b
CLS
EXIT