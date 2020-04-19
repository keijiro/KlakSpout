#!/bin/sh

set -e

GXX="x86_64-w64-mingw32-g++-posix"

compile()
{
    SRC_FILE="$1"
    OBJ_FILE="build/$(basename -s .cpp $SRC_FILE).o"
    $GXX -c -Wall -O2 -I. -IKlakSpout $SRC_FILE -o $OBJ_FILE
}

[ -d "build" ] || mkdir build

compile KlakSpout/KlakSpout.cpp
compile Spout/SpoutDirectX.cpp
compile Spout/SpoutSenderNames.cpp
compile Spout/SpoutSharedMemory.cpp

$GXX -shared -o build/KlakSpout.dll build/*.o -s \
     -Wl,--subsystem,windows -static -ldxgi -ld3d9 -ld3d11
