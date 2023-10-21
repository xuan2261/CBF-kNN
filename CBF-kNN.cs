using CSharpFunctionalExtensions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

public class RTree
{
    private const int MaxEntries = 4;
    private const int MinEntries = MaxEntries / 2;

    public class Rectangle
    {
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }

        public Rectangle(double xMin, double yMin, double xMax, double yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        public bool Intersects(Rectangle other)
        {
            return XMin <= other.XMax && XMax >= other.XMin && YMin <= other.YMax && YMax >= other.YMin;
        }
    }

    public class Entry
    {
        public Rectangle BoundingBox { get; set; }
        public object Value { get; set; }
    }

    public class Node
    {
        public List<Entry> Entries { get; set; } = new List<Entry>();
        public Rectangle BoundingBox { get; set; }
        public bool IsLeaf => Children.Count == 0;
        public List<Node> Children { get; set; } = new List<Node>();
    }

    public Node Root { get; set; } = new Node();

    public void Insert(DataPoint point)
    {
        var entry = new Entry { BoundingBox = new Rectangle(point.X, point.Y, point.X, point.Y), Value = point };
        if (Root.Children.Count == 0)
        {
            InsertEntry(entry, Root, 0);
        }
        else
        {
            var targetNode = ChooseNode(entry.BoundingBox, Root.Children);
            InsertEntry(entry, targetNode, 0);
        }
    }

    public List<Entry> GetEntries(Node node)
    {
        return node.Entries;
    }

    private void InsertEntry(Entry entry, Node node, int level)
    {
        node.Entries.Add(entry);
        node.BoundingBox = CombineRectangles(node.BoundingBox, entry.BoundingBox);
        if (node.Entries.Count > MaxEntries)
        {
            if (level == 0)
            {
                SplitRoot();
            }
            else
            {
                SplitNode(node, level);
            }
        }
    }

    private void SplitRoot()
    {
        var oldRoot = Root;
        var newRoot = new Node();
        Root = newRoot;
        SplitNode(oldRoot, 0);
        var child1 = oldRoot;
        var child2 = oldRoot.Children[0];
        newRoot.Entries.Add(new Entry { BoundingBox = child1.BoundingBox, Value = child1 });
        newRoot.Entries.Add(new Entry { BoundingBox = child2.BoundingBox, Value = child2 });
        newRoot.BoundingBox = CombineRectangles(child1.BoundingBox, child2.BoundingBox);
    }

    private void SplitNode(Node node, int level)
    {
        var (entry1, entry2) = ChooseEntriesToSplit(node.Entries);
        var child1 = CreateNodeWithEntry(entry1);
        var child2 = CreateNodeWithEntry(entry2);
        node.Entries.Clear();
        node.Children.Clear();
        node.Children.Add(child1);
        node.Children.Add(child2);
        node.BoundingBox = CombineRectangles(child1.BoundingBox, child2.BoundingBox);

        foreach (var entry in node.Entries)
        {
            var targetNode = ChooseNode(entry.BoundingBox, node.Children);
            InsertEntry(entry, targetNode, level); // Use the same level value
        }
    }

    private Node CreateNodeWithEntry(Entry entry)
    {
        var node = new Node();
        node.Entries.Add(entry);
        node.BoundingBox = entry.BoundingBox;
        return node;
    }

    private (Entry, Entry) ChooseEntriesToSplit(List<Entry> entries)
    {
        var pairs = FindEntryPairs(entries);
        var (entry1, entry2) = ChoosePairWithMaxEnlargement(pairs);
        return (entry1, entry2);
    }

    private List<(Entry, Entry)> FindEntryPairs(List<Entry> entries)
    {
        List<(Entry, Entry)> pairs = new List<(Entry, Entry)>();

        if (entries.Count < 2)
        {
            return pairs;
        }

        for (int i = 0; i < entries.Count - 1; i++)
        {
            for (int j = i + 1; j < entries.Count; j++)
            {
                pairs.Add((entries[i], entries[j]));
            }
        }

        return pairs;
    }

    private (Entry, Entry) ChoosePairWithMaxEnlargement(List<(Entry, Entry)> pairs)
    {
        double maxEnlargement = double.MinValue;
        (Entry, Entry) selectedPair = (null, null);

        foreach (var pair in pairs)
        {
            var combinedRect = CombineRectangles(pair.Item1.BoundingBox, pair.Item2.BoundingBox);
            var enlargement = CalculateEnlargement(combinedRect, pair.Item1.BoundingBox) + CalculateEnlargement(combinedRect, pair.Item2.BoundingBox);

            if (enlargement > maxEnlargement)
            {
                maxEnlargement = enlargement;
                selectedPair = pair;
            }
        }

        return selectedPair;
    }

    private double CalculateEnlargement(Rectangle largerRect, Rectangle smallerRect)
    {
        double enlargedArea = (Math.Max(largerRect.XMax, smallerRect.XMax) - Math.Min(largerRect.XMin, smallerRect.XMin)) *
                              (Math.Max(largerRect.YMax, smallerRect.YMax) - Math.Min(largerRect.YMin, smallerRect.YMin));
        double originalArea = (largerRect.XMax - largerRect.XMin) * (largerRect.YMax - largerRect.YMin);

        return enlargedArea - originalArea;
    }

    private Rectangle CombineRectangles(Rectangle rect1, Rectangle rect2)
    {
        if (rect1 == null)
        {
            return rect2;
        }

        if (rect1.Equals(rect2))
        {
            return rect1;
        }

        double xMin = Math.Min(rect1.XMin, rect2.XMin);
        double yMin = Math.Min(rect1.YMin, rect2.YMin);
        double xMax = Math.Max(rect1.XMax, rect2.XMax);
        double yMax = Math.Max(rect1.YMax, rect2.YMax);

        return new Rectangle(xMin, yMin, xMax, yMax);
    }

    private Node ChooseNode(Rectangle boundingBox, List<Node> nodes)
    {
        Node selectedNode = null;
        double minEnlargement = double.MaxValue;

        foreach (var node in nodes)
        {
            var combinedRect = CombineRectangles(node.BoundingBox, boundingBox);
            var enlargement = CalculateEnlargement(combinedRect, node.BoundingBox);

            if (enlargement < minEnlargement)
            {
                minEnlargement = enlargement;
                selectedNode = node;
            }
        }

        return selectedNode;
    }

    public double CalculateMinDist(Rectangle rectangle, DataPoint point)
    {
        double dx = Math.Max(Math.Max(rectangle.XMin - point.X, 0), point.X - rectangle.XMax);
        double dy = Math.Max(Math.Max(rectangle.YMin - point.Y, 0), point.Y - rectangle.YMax);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public double CalculateEuclideanDist(DataPoint point1, DataPoint point2)
    {
        double dx = point1.X - point2.X;
        double dy = point1.Y - point2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

public class DataPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class ConcurrentPriorityQueue<TKey, TValue>
{
    private ConcurrentDictionary<TKey, TValue> dictionary;
    private ConcurrentPriorityQueueComparer<TKey> comparer;

    public ConcurrentPriorityQueue()
    {
        dictionary = new ConcurrentDictionary<TKey, TValue>();
        comparer = new ConcurrentPriorityQueueComparer<TKey>();
    }

    public void TryAdd(TKey key, TValue value)
    {
        dictionary.TryAdd(key, value);
    }

    public bool TryRemoveMin(out TKey minKey, out TValue minValue)
    {
        minKey = default(TKey);
        minValue = default(TValue);

        if (dictionary.IsEmpty)
            return false;

        var minKeyValuePair = dictionary.OrderBy(kvp => kvp.Key, comparer).FirstOrDefault();
        if (minKeyValuePair.Key != null)
        {
            dictionary.TryRemove(minKeyValuePair.Key, out minValue);
            minKey = minKeyValuePair.Key;
            return true;
        }

        return false;
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        return dictionary.TryRemove(key, out value);
    }

    public bool ContainsKey(TKey key)
    {
        return dictionary.ContainsKey(key);
    }

    public int Count
    {
        get { return dictionary.Count; }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return dictionary.GetEnumerator();
    }
}


public class ConcurrentPriorityQueueComparer<TKey> : IComparer<TKey>
{
    public int Compare(TKey x, TKey y)
    {
        if (x is IComparable<TKey> comparable)
        {
            return comparable.CompareTo(y);
        }

        throw new ArgumentException("The type of TKey should implement the IComparable<TKey> interface.");
    }
}

public class ConcurrentKNN
{
    public class Element
    {
        public enum ElementType { InternalNode, ExternalNode, DataPoint }

        public ElementType Type { get; set; }
        public double MinDist { get; set; }
        public double EuclideanDist { get; set; }
        public object Value { get; set; }
    }

    public static ConcurrentPriorityQueue<double, Element> CPQ { get; set; }

    public static void PreCBFKNN(object q, RTree tree, int k, int c, ConcurrentPriorityQueue<double, Element> RQ)
    {
        int i = 1;
        double minKey;
        Element minValue;

        CPQ = new ConcurrentPriorityQueue<double, Element>();
        CPQ.TryAdd(0, new Element { Type = Element.ElementType.InternalNode, MinDist = 0, Value = tree.Root });

        while (i <= k && CPQ.TryRemoveMin(out minKey, out minValue))
        {
            var ele = minValue;

            if (ele == null)
                continue;

            if (ele.Type == Element.ElementType.InternalNode)
            {
                var node = ele.Value as RTree.Node;
                if (node != null)
                {
                    var entries = tree.GetEntries(node);
                    foreach (var internalEntry in entries)
                    {
                        var minDist = tree.CalculateMinDist(internalEntry.BoundingBox, (DataPoint)q);
                        CPQ.TryAdd(minDist, new Element { Type = Element.ElementType.InternalNode, MinDist = minDist, Value = internalEntry });
                    }
                }
            }
            else if (ele.Type == Element.ElementType.ExternalNode)
            {
                var externalEntry = ele.Value as RTree.Node;
                if (externalEntry != null)
                {
                    var externalEntries = tree.GetEntries(externalEntry);
                    foreach (var externalDataPoint in externalEntries)
                    {
                        var euclideanDist = tree.CalculateEuclideanDist((DataPoint)externalDataPoint.Value, (DataPoint)q);
                        CPQ.TryAdd(euclideanDist, new Element { Type = Element.ElementType.DataPoint, MinDist = euclideanDist, Value = externalDataPoint });
                    }
                }
            }
            else if (ele.Type == Element.ElementType.DataPoint)
            {
                RQ.TryAdd(ele.MinDist, ele);
                i++;
            }
        }
    }

    public static List<DataPoint> CBFKNN(DataPoint q, int k, int c, int p, List<DataPoint> data)
    {
        var partitions = PartitionData(data, p / c);
        var RQs = new List<ConcurrentPriorityQueue<double, Element>>(); // Khởi tạo danh sách RQs

        var tasks = new List<Task>();
        for (int i = 0; i < p / c; i++)
        {
            var partition = partitions[i];
            var RQi = new ConcurrentPriorityQueue<double, Element>();
            var RTi = BuildRTree(partition);

            var parameters = Tuple.Create(q, RTi, k, c, RQi);
            tasks.Add(Task.Factory.StartNew(() => PreCBFKNN(q, RTi, k, c, RQi)));
            RQs.Add(RQi); // Thêm RQi vào danh sách RQs
        }

        Task.WaitAll(tasks.ToArray());

        var mergedRQ = new ConcurrentPriorityQueue<double, Element>();
        foreach (var RQi in RQs)
        {
            foreach (var kvp in RQi)
            {
                mergedRQ.TryAdd(kvp.Key, kvp.Value);
            }
        }

        var result = new List<DataPoint>();
        int count = 0;
        foreach (var kvp in mergedRQ)
        {
            result.Add((DataPoint)kvp.Value.Value);
            count++;
            if (count == k)
                break;
        }

        return result;
    }

    private static List<List<DataPoint>> PartitionData(List<DataPoint> data, int numPartitions)
    {
        var partitions = new List<List<DataPoint>>();
        int partitionSize = (int)Math.Ceiling((double)data.Count / numPartitions);
        for (int i = 0; i < data.Count; i += partitionSize)
        {
            var partition = data.Skip(i).Take(partitionSize).ToList();
            partitions.Add(partition);
        }
        return partitions;
    }

    private static RTree BuildRTree(List<DataPoint> data)
    {
        var tree = new RTree();
        foreach (var point in data)
        {
            tree.Insert(point);
        }
        return tree;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // Ví dụ minh họa với 10 điểm dữ liệu đầu vào
        var data = new List<DataPoint>
        {
            new DataPoint { X = 1, Y = 2 },
            new DataPoint { X = 2, Y = 3 },
            new DataPoint { X = 3, Y = 4 },
            new DataPoint { X = 4, Y = 5 },
            new DataPoint { X = 5, Y = 6 },
            new DataPoint { X = 6, Y = 7 },
            new DataPoint { X = 7, Y = 8 },
            new DataPoint { X = 8, Y = 9 },
            new DataPoint { X = 9, Y = 10 },
            new DataPoint { X = 10, Y = 11 }
        };

        var q = new DataPoint { X = 3, Y = 5 };
        var k = 3;
        var c = 2;
        var p = 4;

        var result = ConcurrentKNN.CBFKNN(q, k, c, p, data);

        Console.WriteLine("K nearest neighbors:");
        foreach (var neighbor in result)
        {
            Console.WriteLine("({0}, {1})", neighbor.X, neighbor.Y);
        }

        Console.ReadKey();
    }
}