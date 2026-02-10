#include <stdio.h>
#include <stdlib.h>

int main(int argc, char** argv) {

    if(argc != 2) {
        printf("Invalid amount of arguments: expected 1, got %d\n", argc-1);
        exit(EXIT_FAILURE);
    }

    FILE* f = fopen(argv[1], "r");

    if(f == NULL) {
        printf("%s: no such file in this directory\n", argv[1]);
        exit(EXIT_FAILURE);
    }

    char buf[16];
    fscanf(f, "%15[^\n]", buf);
    printf("READ: %s\n", buf);

    fclose(f);

    return 0;
}