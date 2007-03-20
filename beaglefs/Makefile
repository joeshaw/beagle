# beaglefs/Makefile - a Makefile for, shockingly perhaps, beaglefs
#
# rlove

CC = gcc

CFLAGS = -O2 -std=gnu99 -Wall -Wextra -Wshadow -Waggregate-return -Wcast-qual \
	 -Wmissing-prototypes -Wcast-align -Wmissing-declarations -Wformat=2 \
	 -Wbad-function-cast -Wdisabled-optimization -Wpointer-arith -Wundef \
	 -Wwrite-strings -Wredundant-decls -Wunused -Wfloat-equal \
	 -Wstrict-prototypes -Wmissing-noreturn -Wmissing-format-attribute

FUSE_CFLAGS ?= `pkg-config --cflags fuse`
FUSE_LIBS ?= `pkg-config --libs fuse`

BEAGLE_CFLAGS ?= `pkg-config --cflags libbeagle-0.0`
BEAGLE_LIBS ?= `pkg-config --libs libbeagle-0.0`

GTHREAD_CFLAGS ?= `pkg-config --cflags gthread-2.0`
GTHREAD_LIBS ?= `pkg-config --libs gthread-2.0`

beaglefs: beaglefs.c dir.c dir.h file.c file.h hit.c hit.h inode.c inode.h
	@$(CC)			\
	$(CFLAGS) 		\
	$(GTHREAD_CFLAGS)	\
	$(GTHREAD_LIBS)		\
	$(FUSE_CFLAGS)		\
	$(FUSE_LIBS)		\
	$(BEAGLE_CFLAGS)	\
	$(BEAGLE_LIBS)		\
	-o beaglefs beaglefs.c dir.c file.c hit.c inode.c

clean:
	@rm -f beaglefs
