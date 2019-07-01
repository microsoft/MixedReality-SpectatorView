#!/bin/sh

git config core.symlinks true
git submodule update --init
find ../../src -type l | xargs rm
find ../../samples -type l | xargs rm
git checkout -f -- :/samples
