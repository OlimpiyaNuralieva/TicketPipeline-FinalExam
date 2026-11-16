#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
mkdir -p out
gcc -std=c99 -O2 -Wall -Wextra -fPIC -fvisibility=hidden -shared -o out/libticketcost.so ticketcost.c
echo "built out/libticketcost.so with $(gcc --version | head -n 1)"
