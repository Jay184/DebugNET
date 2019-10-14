// random.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "random.h"
#include <iostream>
#include <time.h>

void seed(int seed) {
	srand(seed);
}
int seed_random() {
	int seed = (int)time(0);
	srand(seed);
	return seed;
}
int random(int max) {
	int value = rand() % max;
	return value;
}