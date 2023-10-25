//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace DemokNNRtree
//{
//    internal class ConcurrentPriorityQueue
//    {
//    }
//}

using System;
using System.Collections.Generic;
using System.Threading;

public class ConcurrentPriorityQueue<T> where T : IComparable<T>
{
    private readonly List<List<T>> mound;
    private readonly object lockObject = new object();

    public ConcurrentPriorityQueue()
    {
        mound = new List<List<T>>();
    }

    public void Insert(T item)
    {
        lock (lockObject)
        {
            int index = BinarySearch(item);
            if (index < 0)
                index = ~index;

            if (index >= mound.Count)
            {
                mound.Add(new List<T>() { item });
            }
            else
            {
                mound[index].Add(item);
                Moundify(index);
            }

            Monitor.PulseAll(lockObject);
        }
    }

    public bool TryExtractMin(out T result)
    {
        lock (lockObject)
        {
            while (mound.Count == 0 || mound[0].Count == 0)
                Monitor.Wait(lockObject);

            result = mound[0][0];
            mound[0].RemoveAt(0);
            if (mound[0].Count == 0)
                mound.RemoveAt(0);

            return true;
        }
    }

    public bool TryTop(out T result)
    {
        lock (lockObject)
        {
            while (mound.Count == 0 || mound[0].Count == 0)
                Monitor.Wait(lockObject);

            result = mound[0][0];
            return true;
        }
    }

    public bool TryRemoveMin(out T result)
    {
        lock (lockObject)
        {
            while (mound.Count == 0 || mound[0].Count == 0)
                Monitor.Wait(lockObject);

            result = mound[0][0];
            mound[0].RemoveAt(0);
            if (mound[0].Count == 0)
                mound.RemoveAt(0);

            return true;
        }
    }

    private int BinarySearch(T item)
    {
        int left = 0;
        int right = mound.Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int compareResult = item.CompareTo(mound[mid][0]);

            if (compareResult == 0)
                return mid;
            if (compareResult < 0)
                right = mid - 1;
            else
                left = mid + 1;
        }

        return ~left;
    }

    private void Moundify(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (mound[index][0].CompareTo(mound[parentIndex][0]) < 0)
            {
                Swap(index, parentIndex);
                index = parentIndex;
            }
            else
            {
                break;
            }
        }
    }

    private void Swap(int i, int j)
    {
        List<T> temp = mound[i];
        mound[i] = mound[j];
        mound[j] = temp;
    }
}

//Đây là một lớp ConcurrentPriorityQueue đã được sửa đổi. Lớp này triển khai một hàng đợi ưu tiên có khả năng đồng thời (concurrent) cho các phương thức chèn, truy xuất và xóa phần tử.

//Dưới đây là mô tả ngắn về các thành phần và phương thức trong lớp:

//mound: Đây là một danh sách chứa các mục hàng đợi theo một số mức ưu tiên. Mỗi mức ưu tiên là một danh sách các phần tử có cùng mức ưu tiên.
//lockObject: Đây là một đối tượng khóa (lock object) dùng để đồng bộ hóa truy cập đến hàng đợi cùng lúc từ nhiều luồng (threads).
//ConcurrentPriorityQueue(): Đây là hàm khởi tạo của lớp, tạo một hàng đợi ưu tiên rỗng.
//Insert(T item): Phương thức này chèn một phần tử vào hàng đợi ưu tiên. Nó tìm vị trí thích hợp để chèn phần tử và sau đó cập nhật cấu trúc của hàng đợi.
//TryExtractMin(out T result): Phương thức này cố gắng lấy phần tử có giá trị nhỏ nhất từ hàng đợi và gán vào biến result. Nếu hàng đợi rỗng, phương thức này sẽ chờ cho đến khi có phần tử để lấy.
//TryTop(out T result): Phương thức này cố gắng lấy phần tử có giá trị nhỏ nhất từ hàng đợi và gán vào biến result, nhưng không xóa phần tử đó khỏi hàng đợi. Nếu hàng đợi rỗng, phương thức này sẽ chờ cho đến khi có phần tử để lấy.
//TryRemoveMin(out T result): Phương thức này cố gắng lấy và xóa phần tử có giá trị nhỏ nhất từ hàng đợi, và gán vào biến result. Nếu hàng đợi rỗng, phương thức này sẽ chờ cho đến khi có phần tử để lấy.
//Ngoài ra, lớp còn có các phương thức hỗ trợ như BinarySearch, Moundify và Swap, được sử dụng để tìm kiếm nhị phân, duy trì tính chất của hàng đợi ưu tiên và hoán đổi các phần tử trong hàng đợi.

//Lớp ConcurrentPriorityQueue này sử dụng đồng bộ hóa bằng cách sử dụng khối lock và các phương thức Monitor.Wait và Monitor.PulseAll để đảm bảo tính nhất quán và an toàn trong truy cập đồng thời từ nhiều luồng.
