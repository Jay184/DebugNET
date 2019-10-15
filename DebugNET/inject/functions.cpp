#include "stdafx.h"
#include "functions.h"

// Simple function to demonstrate the syntax.
INJECT_API(unsigned int) echo(unsigned int parameter) {
	return parameter;
}

// Demonstrating a slighter longer function that uses external libraries.
INJECT_API(unsigned int) fibonacci(unsigned int n) {
	unsigned int a = 0;
	unsigned int b = 1;

	if (n == 0) return 0;

	for (unsigned int bit = (int)pow(2, (int)log2(n)); bit != 0; bit >>= 1) {
		unsigned int d = a * ((b << 1) - a);
		unsigned int e = a * a + b * b;

		a = d;
		b = e;

		if ((n & bit) != 0) {
			unsigned int c = a + b;
			a = b;
			b = c;
		}
	}

	return a;
}

// Function with two parameters.
INJECT_API(unsigned int) add(int* parameter) {
	int sum = parameter[0] + parameter[1];
	return sum;
}