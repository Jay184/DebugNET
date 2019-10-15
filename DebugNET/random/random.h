#pragma once

#ifdef RANDOM_EXPORTS
#define RANDOM_API __declspec(dllexport)
#else
#define RANDOM_API __declspec(dllimport)
#endif

extern "C" RANDOM_API void __stdcall seed(void* parameter);
extern "C" RANDOM_API int __stdcall seed_random(void* parameter);
extern "C" RANDOM_API int __stdcall random(void* parameter);
extern "C" RANDOM_API void __stdcall increment(void* parameter);
