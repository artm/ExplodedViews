#!/usr/bin/env make -f

DIRS := $(shell ls -d */)
ZIPS := $(patsubst %/,%.zip,$(DIRS))

all: $(ZIPS)

%.zip: %
	zip -r $@ $^ && rm -rf $^
