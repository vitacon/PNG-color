rem 10:17 15.12.2021

set GD=g:\M…j disk

if exist "\visual-work" goto WORK

rem Domácí nastavení
set PLACE=home
set mydate=%date:~9,4%%date:~6,2%%date:~3,2%
goto ALLSET

:WORK
rem Pracovní nastavení
set PLACE=atc
set mydate=%date:~6,4%%date:~3,2%%date:~0,2%

:ALLSET
set suf=
echo %mydate%

set prefix=png-color

:SUF
set FULLNAME=..\_archive\%prefix%\%prefix%-%mydate%-%place%%suf%
if not exist %FULLNAME%.rar goto READY
set SUF=%SUF%x
goto SUF

:READY
"c:\Program Files\WinRAR\WinRAR.exe" a %FULLNAME% . %prefix% -m5 -s

copy %FULLNAME%.rar "%GD%\video\png-tools\%prefix%\"

pause