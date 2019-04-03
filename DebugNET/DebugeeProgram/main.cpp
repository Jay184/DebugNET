#include <iostream>
#include <thread>
#include <time.h>

int main() {
	int seed = (int)time(0);
	srand(seed);

	int value = rand() % 255;
	int* addr = &value;

	do {

		// copy value
		int v = value;

		// output
		std::cout << v << " (";
		printf("0x%p", addr);
		std::cout << ")" << std::endl;

		// wait 1 second
		std::this_thread::sleep_for(std::chrono::seconds(1));

	} while (true);
}