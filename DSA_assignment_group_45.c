#include <stdio.h>
#include <math.h>
#include <stdlib.h>
#include <stdbool.h>
#include <time.h>
#include <limits.h>
#include <string.h>

#define THRESHOLD 8 // We define the threshold parameter as proposed in the paper as 8 in our implementation
#define T INT_MAX   // We define T as the maximum possible int value(2^31-1)
typedef struct LNode *LNODE;
typedef struct MNode MNode;

FILE *fptr1;

int HEIGHT = 0; // HEIGHT stores the current height(termed as Depth in the paper) of the tree
                // initially it's 0
int COUNT = 0;  // COUNT stores the total number of LNodes(values) in the mound

struct LNode
{
    int value; // Each list node(LNode) stores a value and a pointer to the next Lnode
    LNODE next;
};

struct MNode
{
    LNODE list;
    bool dirty; // Each mound node(MNode) stores a pointer to a list, a dirty field to check mound invariant
    int count;  // Will be used in parallel access by threads, not implemented here
};

MNode *tree; // Global variable as defined in the paper

// the function takes in a value and creates an LNode to initialize it with that value

LNODE createLNode(int value)
{
    LNODE node = (LNODE)calloc(1, sizeof(struct LNode));
    node->value = value;
    node->next = NULL;
    return node;
}

// the function cretes an array "tree" for implementing the mound.
// the mound initially contains a single MNode initialised with the value T i.e. pointing to a list with only element T which is the value given to a null node by the paper
// T is the max posible integer value
// As there is only one MNode , the mound invariant is satisfied so dirty field is set to false

void createTree()
{
    tree = (MNode *)calloc(pow(2, HEIGHT + 1) - 1, sizeof(MNode));
    for (int i = 0; i < pow(2, HEIGHT + 1) - 1; i++)
    {
        // whenever we create a new MNode it's initially pointing to an empty list
        (tree + i)->list = createLNode(T); // according to the paper it has a value =T, which we are implementing by
                                           // explicitly placing an Lnode with value = T  in the list                 // so if first Lnode's value is T it will imply that the list is empty

        (tree + i)->dirty = false;
        (tree + i)->count = 0;
    }
}

// The function resizes the mound to include one more level thereby creating more 2^height MNodes.
// All those new MNodes are initialised to T i.e. all those point to a list with containing only one element T

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

// When we extract a node from the mound, the mound invariant may get unsatisfied,
// so we set that MNode's dirty field to be true and apply moundify

void moundify(int index)
{
    if (index >= pow(2, HEIGHT) - 1)
    {
        (tree + index)->dirty = false;
        return;
    }
    int parent_val = (tree + index)->list->value;
    int left_child = (index + 1) * 2 - 1; // this is similar to the simple array based priority queue implementation, +/-1 offests are used as required to account for the indices starting from 0
    int right_child = left_child + 1;     // child nodes of the node at position i are 2i , 2i +1
    int left_child_val = (tree + left_child)->list->value;
    int right_child_val = (tree + right_child)->list->value;
    if ((tree + left_child)->dirty)
    {
        moundify(left_child);
    }

    // before applying moundify to the currnt node we first check if the child nodes are dirty
    // the child nodes may be dirty because of parallel extraction

    if ((tree + right_child)->dirty)
    {
        moundify(right_child);
    }

    // checking which MNode has the smallest value among
    // the triangle formed by parent node and the children nodes

    bool isLeftMin = left_child_val <= right_child_val;
    if (isLeftMin && left_child_val < parent_val)
    {
        MNode temp = tree[index];
        tree[index] = tree[left_child]; // if the left child node has the smallest value in the triangle
        tree[left_child] = temp;        // swapping the parent node and the left child node
        moundify(left_child);
    }
    else if (!isLeftMin && right_child_val < parent_val)
    {
        MNode temp = tree[index];
        tree[index] = tree[right_child]; // if the right child node has the smallest value in the triangle
        tree[right_child] = temp;        // swapping the parent node and the right child node
        moundify(right_child);
    }
    else
        (tree + index)->dirty = false; // if the parent node actually has the smallest value
                                       // then setting it's dirty field to again to false
}

// this function returns the smallest value(LNode value) in the mound
// the smallest value is typically stored in the list originating from the root
// infact the first LNode's value in the list is the smallest value as the list is sorted
// this value is returned and the corresonding LNode is removed from the list

// after removal the value of the MNode is logically the next element in the list
// this value may not be smallest in the triangle formed at the root MNode so moundify is applied at the root index(0)

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

// this function inserts a new LNode at the beginning of the list originating at the given MNode

void insertAtHead(MNode *head, LNODE node)
{
    node->next = head->list;
    head->list = node;
}

// this function returns the index of the ancestors of a leaf-node(the MNodes at the level HEIGHT) in the tree array
// the parent MNode of a MNode is obtained by floor of i/2 where i is the index in the tree array
// the root MNode is an ancestor for each leaf-node and is of the 0th generation.(i / 2^ HEIGHT)
// genaration can't be greator than the HEIGHT , generation = HEIGHT is the leaf-node itself (i / 2^ 0)
// So value of generation moving down the levels of mound i.e. 1st level = 1st generation and so on

int get_ancestor(int index, int generation)
{
    return index / pow(2, HEIGHT - generation);
}

// after we know that the new element can be inserted in the branch containing that particular leaf-node
// we have to find the actual position of the new element in the branch
// we can apply binary search on that branch because we know that for any MNode in the branch the value of the
// parent MNode is smaller or equal to that MNode , so the values of MNodes in the branch are sorted

int binary_search(int key, int leaf_index)
{
    int ans = -1;
    int low = 0;       // low is inintially the first level(containing root node) of the mound
    int high = HEIGHT; // high is initially the last level(HEIGHT) of the mound(containing leaf MNodes )

    while (low <= high)
    {
        int mid = low + (high - low + 1) / 2;             // obtainig the index(according to tree array) of the
        int mid1 = get_ancestor(leaf_index + 1, mid) - 1; // ancestor at level mid and storing it's value in midval
        int midVal = (tree + mid1)->list->value;

        if (midVal > key)   // if the value is greater than the key(new element)
        {                   // the key can be inserted in the list at that MNode
            ans = mid1;     // but it may not be suitable position(it may be more upwards)
            high = mid - 1; // so we decrease high value to search above in the branch
        }
        else if (midVal < key) // if the value is smaller than the key
        {                      // the key should be inserted further down in the branch
            low = mid + 1;     // we increment low to search in the lower portion
        }
        else if (midVal == key)
        {
            ans = mid1; // if the value is equal to key it should be inserted at that position
            break;      // So we break the while loop there
        }
    }
    return ans;
}

// when a new element comes it's value is first compared the leaf-node's value
// if it's value is greater than it can't be inserted in the branch containing that leaf and visa-versa
// based on whether it can be inserted or not the following function returns true or false

bool insertion(int leaf_index, LNODE node)
{
    if (node->value > (tree + leaf_index)->list->value)
        return false;

    int insertion_index = binary_search(node->value, leaf_index);

    insertAtHead(tree + insertion_index, node);
    return true;
}

// This function creates a new LNode and initializes it with the value of the new element
// It finds a suitable branch by comparing the new value to the leaf-node's value

bool insertLNode(int value)
{
    LNODE node = createLNode(value);
    int count = 0;
    int leaf_index = -1;
    for (int i = 1; i <= THRESHOLD; i++) // the loop runs threshold(8) no of times at max
    {
        int random = rand();
        leaf_index = (int)pow(2, HEIGHT) - 1 + random % (int)pow(2, HEIGHT); // finds a random leaf-node at each iteration

        if (insertion(leaf_index, node) == true)
        {
            COUNT++;     // if the new element can be inserted into the branch containing that leaf
            return true; // we insert it there & increment total count of elements(COUNT) in the mound
        }
    }

    HEIGHT++;
    resizeTree();                                          // if the count exceeds the threshold value we add a new level in the mound
    insertAtHead((tree + 2 * (leaf_index + 1) - 1), node); // And insert the new LNode in the left child of the
                                                           // last compared leaf-node
    COUNT++;                                               // we increment the total COUNT of the elements in the mound
    return true;
}

// this function prints the mound i.e.  the tree array elements(MNodes) along with their list

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

    // this removes all the elements from the mound one by one
    int min, i = 0;
    fprintf(fptr1, "Extraction of values in ascending order:\n");
    while (min = extractMin(tree), min != T)
    {
        fprintf(fptr1, "%d.\t%d\n", i + 1, min);
        i++;
    }
    // printTree();

    for (int i = 0; i < pow(2, HEIGHT + 1) - 1; i++)
    {
        free((tree + i)->list);
    }
    free(tree);
    fclose(fptr);
    fclose(fptr1);
    return 0;
}
