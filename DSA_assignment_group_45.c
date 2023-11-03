#include <stdio.h>
#include <math.h>
#include <stdlib.h>
#include <stdbool.h>
#include <time.h>
#include <limits.h>
#include <string.h>

#define THRESHOLD 8
#define T INT_MAX
typedef struct LNode *LNODE;
typedef struct MNode MNode;

FILE *fptr1;

int HEIGHT = 0;
int COUNT = 0;

struct LNode
{
    int value;
    LNODE next;
};

struct MNode
{
    LNODE list;
    bool dirty;
    int count;
};

MNode *tree;

LNODE createLNode(int value)
{
    LNODE node = (LNODE)calloc(1, sizeof(struct LNode));
    node->value = value;
    node->next = NULL;
    return node;
}

void createTree()
{
    tree = (MNode *)calloc(pow(2, HEIGHT + 1) - 1, sizeof(MNode));
    for (int i = 0; i < pow(2, HEIGHT + 1) - 1; i++)
    {
        (tree + i)->list = createLNode(T);
        (tree + i)->dirty = false;
        (tree + i)->count = 0;
    }
}

void resizeTree()
{
    tree = (MNode *)realloc(tree, (pow(2, HEIGHT + 1) - 1) * sizeof(MNode));
    for (int i = pow(2, HEIGHT) - 1; i < pow(2, HEIGHT + 1) - 1; i++)
    {
        (tree + i)->list = createLNode(T);
        (tree + i)->dirty = false;
        (tree + i)->count = 0;
    }
}

void moundify(int index)
{
    if (index >= pow(2, HEIGHT) - 1)
    {
        (tree + index)->dirty = false;
        return;
    }
    int parent_val = (tree + index)->list->value;
    int left_child = (index + 1) * 2 - 1;
    int right_child = left_child + 1;
    int left_child_val = (tree + left_child)->list->value;
    int right_child_val = (tree + right_child)->list->value;
    if ((tree + left_child)->dirty)
    {
        moundify(left_child);
    }

    if ((tree + right_child)->dirty)
    {
        moundify(right_child);
    }

    bool isLeftMin = left_child_val <= right_child_val;
    if (isLeftMin && left_child_val < parent_val)
    {
        MNode temp = tree[index];
        tree[index] = tree[left_child];
        tree[left_child] = temp;
        moundify(left_child);
    }
    else if (!isLeftMin && right_child_val < parent_val)
    {
        MNode temp = tree[index];
        tree[index] = tree[right_child];
        tree[right_child] = temp;
        moundify(right_child);
    }
    else
        (tree + index)->dirty = false;
}

int extractMin()
{
    int min = tree->list->value;
    if (min == T)
    {
        return min;
    }
    LNODE temp = tree->list;
    tree->list = tree->list->next;
    free(temp);
    tree->dirty = true;
    moundify(0);
    return min;
}

void insertAtHead(MNode *head, LNODE node)
{
    node->next = head->list;
    head->list = node;
}

int get_ancestor(int index, int generation)
{
    return index / pow(2, HEIGHT - generation);
}

int binary_search(int key, int leaf_index)
{
    int ans = -1;
    int low = 0;
    int high = HEIGHT;

    while (low <= high)
    {
        int mid = low + (high - low + 1) / 2;
        int mid1 = get_ancestor(leaf_index + 1, mid) - 1;
        int midVal = (tree + mid1)->list->value;

        if (midVal > key)
        {
            ans = mid1;
            high = mid - 1;
        }
        else if (midVal < key)
        {
            low = mid + 1;
        }
        else if (midVal == key)
        {
            ans = mid1;
            break;
        }
    }
    return ans;
}

bool insertion(int leaf_index, LNODE node)
{
    if (node->value > (tree + leaf_index)->list->value)
        return false;

    int insertion_index = binary_search(node->value, leaf_index);

    insertAtHead(tree + insertion_index, node);
    return true;
}

bool insertLNode(int value)
{
    LNODE node = createLNode(value);
    int count = 0;
    int leaf_index = -1;
    for (int i = 1; i <= THRESHOLD; i++)
    {
        int random = rand();
        leaf_index = (int)pow(2, HEIGHT) - 1 + random % (int)pow(2, HEIGHT);

        if (insertion(leaf_index, node) == true)
        {
            COUNT++;
            return true;
        }
    }

    HEIGHT++;
    resizeTree(); 
    insertAtHead((tree + 2 * (leaf_index + 1) - 1), node);
    COUNT++;
    return true;
}


void printTree()
{
    fprintf(fptr1, "\n\n########################################Tree########################################################\n");

    for (int j = 0; j <= HEIGHT; j++)
    {
        int c = 1;
        fprintf(fptr1, "---------------------------------------------------%d-------------------------------------------\n", j);

        for (int i = pow(2, j) - 1; i < pow(2, j + 1) - 1; i++, c++)
        {
            fprintf(fptr1, "%d.%d:\t", j, c);
            LNODE l = (tree + i)->list;
            while (l->value != T)
            {
                fprintf(fptr1, "%d -> ", l->value);
                l = l->next;
            }
            fprintf(fptr1, "T");
            fprintf(fptr1, "\n");
        }
        fprintf(fptr1, "\n");
    }
}

int main()
{
    time_t t1;
    srand(time(&t1));

    createTree();
    FILE *fptr;

    fptr = fopen("data.txt", "r");
    fptr1 = fopen("output.txt", "w");
    if (fptr == NULL || fptr1 == NULL)
    {
        printf("Enter correct input and output files\n");
        return 1;
    }
    char *line = malloc(100);
    while (fgets(line, 100, fptr) != NULL)
    {
        char *token;
        token = strtok(line, "\n");
        int a = atoi(token);
        insertLNode(a);
    }

    printTree();

    int min, i = 0;
    fprintf(fptr1, "Extraction of values in ascending order:\n");
    while (min = extractMin(tree), min != T)
    {
        fprintf(fptr1, "%d.\t%d\n", i + 1, min);
        i++;
    }

    for (int i = 0; i < pow(2, HEIGHT + 1) - 1; i++)
    {
        free((tree + i)->list);
    }
    free(tree);
    fclose(fptr);
    fclose(fptr1);
    return 0;
}
