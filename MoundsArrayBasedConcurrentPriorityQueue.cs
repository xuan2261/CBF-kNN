using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemokNNRtree
{
    public class MNode
    {
        public int value;
        public MNode next;

        public MNode(int val)
        {
            value = val;
            next = null;
        }
    }

    public class MoundsArrayBasedConcurrentPriorityQueue
    {
        private MNode[] tree;

        public MoundsArrayBasedConcurrentPriorityQueue()
        {
            tree = new MNode[100]; // Kích thước cây mounds (heap) tùy ý
        }

        public void InsertAtBeginning(int index, int value)
        {
            MNode newNode = new MNode(value);
            newNode.next = tree[index];
            tree[index] = newNode;
        }

        public void PrintList(MNode head)
        {
            MNode current = head;
            while (current != null)
            {
                Console.Write(current.value + " ");
                current = current.next;
            }
            Console.WriteLine();
        }

        public void Moundify()
        {
            // Xây dựng cây mounds
            for (int i = 1; i < tree.Length; i++)
            {
                if (tree[i] != null && tree[i].next != null)
                {
                    MNode current = tree[i];
                    while (current.next != null)
                    {
                        if (current.value > current.next.value)
                        {
                            int temp = current.value;
                            current.value = current.next.value;
                            current.next.value = temp;
                        }
                        current = current.next;
                    }
                }
            }
        }

        public MNode RandLeaf(int depth)
        {
            Random random = new Random();
            int index = random.Next((int)Math.Pow(2, depth), (int)Math.Pow(2, depth + 1));
            return tree[index];
        }

        public int Val(MNode head)
        {
            if (head == null)
                throw new InvalidOperationException("Priority queue is empty.");

            return head.value;
        }

        public void Insert(int value, int depth)
        {
            int index = FindInsertionPoint(value, depth);
            InsertAtBeginning(index, value);
            Moundify();
        }

        public int BinarySearch(int value, int start, int end)
        {
            while (start <= end)
            {
                int mid = (start + end) / 2;
                if (tree[mid] == null)
                    return mid;

                int cmp = tree[mid].value.CompareTo(value);
                if (cmp == 0)
                    return mid;
                else if (cmp < 0)
                    end = mid - 1;
                else
                    start = mid + 1;
            }
            return start;
        }

        public int FindInsertionPoint(int value, int depth)
        {
            int start = (int)Math.Pow(2, depth);
            int end = (int)Math.Pow(2, depth + 1) - 1;
            return BinarySearch(value, start, end);
        }

        public int ExtractMin()
        {
            if (tree[1] == null)
                throw new InvalidOperationException("Priority queue is empty.");

            int min = tree[1].value;
            tree[1] = tree[1].next;
            Moundify();
            return min;
        }

        public void Swap(int index1, int index2)
        {
            MNode temp = tree[index1];
            tree[index1] = tree[index2];
            tree[index2] = temp;
        }
    }
}
