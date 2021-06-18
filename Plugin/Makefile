#
# File listings
#

TARGET = KlakSpout.dll

SRCS = KlakSpout/KlakSpout.cpp \
       Spout/SpoutDirectX.cpp \
       Spout/SpoutSenderNames.cpp \
       Spout/SpoutSharedMemory.cpp

OBJS = $(SRCS:.cpp=.o)

LIBS = -Wl,--subsystem,windows -static -ldxgi -ld3d9 -ld3d11

#
# Compiler/linker options
#

TOOLCHAIN = x86_64-w64-mingw32
CC = $(TOOLCHAIN)-g++-posix
STRIP = $(TOOLCHAIN)-strip

CC_FLAGS = -O2 -IKlakSpout -I. -std=c++14
CC_FLAGS += -Wall -Wno-unknown-pragmas -Wno-conversion-null
CC_FLAGS += -Wno-format -Wno-sign-compare

LD_FLAGS = -shared

#
# Building rules
#

all: $(TARGET)

clean:
	rm -f $(TARGET) $(OBJS)

$(TARGET): $(OBJS)
	$(CC) $(LD_FLAGS) -o $@ $^ $(LIBS)
	$(STRIP) $@

%.o: %.cpp
	$(CC) $(CC_FLAGS) -c -o $@ $<
