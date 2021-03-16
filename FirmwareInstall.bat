@ECHO OFF
::This batch file take in VID and PID and Firmware Location, then installs the firmware

sdphost -u %1,%2 -V write-file 0x400 %3
sdphost -u %1,%2 -V jump-address 0x400
pause