#!/bin/sh

GPP="x86_64-w64-mingw32-g++-posix"
CPPFLAGS="-c -Wall -O2 -IKlakSpout"

$GPP $CPPFLAGS KlakSpout/KlakSpout.cpp
$GPP $CPPFLAGS KlakSpout/Spout/SpoutDirectX.cpp
$GPP $CPPFLAGS KlakSpout/Spout/SpoutSenderNames.cpp
$GPP $CPPFLAGS KlakSpout/Spout/SpoutSharedMemory.cpp

$GPP -shared -o KlakSpout.dll *.o -Wl,--subsystem,windows -static -ldxgi -ld3d9 -ld3d11
