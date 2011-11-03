#!/bin/sh
for d in foo bar baz _-37.817713_144.966580_1 ; do
  echo $d
  mkdir -p $d
  rm -f $d/load.fake
  dd if=/dev/random of=$d/load.fake count=10000 bs=4096
done
