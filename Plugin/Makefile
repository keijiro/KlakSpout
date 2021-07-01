#
# File listings
#

TARGET = KlakSpout.dll

DEST = ../Packages/jp.keijiro.klak.spout/Plugin/

SRCS = Plugin.cpp \
       Spout/SpoutSenderNames.cpp \
       Spout/SpoutSharedMemory.cpp \
       Spout/SpoutUtils.cpp

OBJS = $(SRCS:.cpp=.o)

LIBS = -Wl,--subsystem,windows -static -ldxgi -ld3d12 -ld3d11 -lole32

#
# Compiler/linker options
#

TOOLCHAIN = x86_64-w64-mingw32
CC = $(TOOLCHAIN)-g++-posix
STRIP = $(TOOLCHAIN)-strip

CC_FLAGS = -O2
CC_FLAGS += -I. -std=c++17 -DMINI_SPOUTUTILS
CC_FLAGS += -Wall -Wno-unknown-pragmas -Wno-conversion-null
CC_FLAGS += -Wno-format -Wno-unused-function

LD_FLAGS = -shared

#
# Building rules
#

all: $(TARGET)

clean:
	rm -f $(TARGET) $(OBJS)

copy: all
	cp $(TARGET) $(DEST)

$(TARGET): $(OBJS)
	$(CC) $(LD_FLAGS) -o $@ $^ $(LIBS)
	$(STRIP) $@

%.o: %.cpp
	$(CC) $(CC_FLAGS) -c -o $@ $<
