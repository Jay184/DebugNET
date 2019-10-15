// random.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "random.h"
#include <iostream>
#include <time.h>

RANDOM_API void __stdcall seed(void * parameter) {
	srand((unsigned int)parameter);
}
RANDOM_API int __stdcall seed_random(void * parameter) {
	unsigned int seed = (unsigned int)time(0);
	srand(seed);
	return seed;
}
RANDOM_API int __stdcall random(void * parameter) {
	int value = rand() % (unsigned int)parameter;
	return value;
}
RANDOM_API void __stdcall increment(void * parameter) {
	__asm {
		inc ebx
		mov eax, ebx
	}
}
