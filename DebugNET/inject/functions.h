#pragma once

/*
<PROJECT>_EXPORTS defined by Visual Studio upon DLL project creation.
extern "C" stops name mangling due to __stdcall.
This on the other hand removes the ability to use polymorphism.
__stdcall ( = WINAPI ) is the calling convention CreateRemoteThread() of kernel32.dll uses.
It tells the compiler to assemble so that the EAX register contains the return value, which is the thread exit code when it exits.
*/
#ifdef INJECT_EXPORTS
#define INJECT_API(ReturnType) extern "C" __declspec(dllexport) ReturnType WINAPI
#else
#define INJECT_API(ReturnType) extern "C" __declspec(dllexport) ReturnType WINAPI
#endif

// This allows the following declaration of functions.
INJECT_API(unsigned int) echo(unsigned int parameter);
INJECT_API(unsigned int) fibonacci(unsigned int n);
INJECT_API(unsigned int) add(int* parameter);


struct vec {
    public:
        int x, y;
};

INJECT_API(vec*) plus2(vec* parameter) {
    parameter->x += 2;
    parameter->y += 2;

    return parameter;
}