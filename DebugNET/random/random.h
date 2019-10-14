#pragma once

#ifdef RANDOM_EXPORTS
#define RANDOM_API __declspec(dllexport)
#else
#define RANDOM_API __declspec(dllimport)
#endif

extern "C" RANDOM_API void seed(int seed);
extern "C" RANDOM_API int seed_random();
extern "C" RANDOM_API int random(int max);
