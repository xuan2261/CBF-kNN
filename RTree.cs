

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class RTree
{
    private int m; // Số lượng con tối thiểu trong một nút
    private int M; // Số lượng con tối đa trong một nút
    private Node root; // Nút gốc của cây

    public RTree(int m, int M)
    {
        this.m = m;
        this.M = M;
        this.root = null;
    }

    // Lớp Rectangle biểu diễn hình chữ nhật
    public class Rectangle
    {
        public double MinX { get; set; } // Tọa độ X nhỏ nhất
        public double MinY { get; set; } // Tọa độ Y nhỏ nhất
        public double MaxX { get; set; } // Tọa độ X lớn nhất
        public double MaxY { get; set; } // Tọa độ Y lớn nhất

        public Rectangle(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        //kiểm tra hai hình chữ nhật có giao nhau hay không
        public bool Intersects(Rectangle other)
        {
            return !(other.MinX > MaxX || other.MaxX < MinX || other.MinY > MaxY || other.MaxY < MinY);
        }
    }

    // Lớp Node biểu diễn một nút trong cây R-Tree
    private class Node
    {
        public Rectangle MBR { get; set; } // Hình chữ nhật biên của nút
        public bool IsLeaf { get; set; } // Xác định xem nút có phải là nút lá hay không
        public List<Node> Children { get; set; } // Danh sách các nút con

        public Node(Rectangle mbr, bool isLeaf)
        {
            MBR = mbr;
            IsLeaf = isLeaf;
            Children = new List<Node>();
        }

        // Phương thức thêm một nút con vào nút hiện tại
        public void AddChild(Node child)
        {
            Children.Add(child);
        }

        // Phương thức xóa một nút con khỏi nút hiện tại
        public void RemoveChild(Node child)
        {
            Children.Remove(child);
        }

        // Phương thức kiểm tra xem nút có quá tải hay không
        public bool IsOverflow(RTree rtree)
        {
            return Children.Count > rtree.M;
        }
    }

    // Phương thức Insert để chèn một hình chữ nhật vào cây
    public void Insert(Rectangle mbr)
    {
        if (root == null)
        {
            root = new Node(mbr, true);
        }
        else
        {
            Node leaf = ChooseLeaf(root, mbr);
            leaf.AddChild(new Node(mbr, true));
            if (leaf.IsOverflow(this))
            {
                HandleOverflow(leaf);
            }
        }
    }

    private Node ChooseLeaf(Node node, Rectangle mbr)
    {
        if (node.IsLeaf)
        {
            return node;
        }
        else
        {
            double minIncrease = double.PositiveInfinity;
            Node selectedChild = null;
            foreach (Node child in node.Children)
            {
                double increase = CalculateIncrease(child.MBR, mbr);
                if (increase < minIncrease)
                {
                    minIncrease = increase;
                    selectedChild = child;
                }
            }
            return ChooseLeaf(selectedChild, mbr);
        }
    }

    private double CalculateIncrease(Rectangle mbr1, Rectangle mbr2)
    {
        double area1 = (mbr1.MaxX - mbr1.MinX) * (mbr1.MaxY - mbr1.MinY);
        double area2 = (mbr2.MaxX - mbr2.MinX) * (mbr2.MaxY - mbr2.MinY);
        double unionArea = (Math.Max(mbr1.MaxX, mbr2.MaxX) - Math.Min(mbr1.MinX, mbr2.MinX)) *
                           (Math.Max(mbr1.MaxY, mbr2.MaxY) - Math.Min(mbr1.MinY, mbr2.MinY));
        return unionArea - area1 - area2;
    }

    // Xử lý tràn của nút
    private void HandleOverflow(Node node)
    {
        // Tách nút bị tràn thành hai nút con
        Node[] splitNodes = SplitNode(node);

        if (node == root)
        {
            // Tạo một nút gốc mới và thêm hai nút con đã tách vào nút gốc mới
            root = new Node(null, false);
            root.AddChild(splitNodes[0]);
            root.AddChild(splitNodes[1]);
        }
        else
        {
            // Xác định nút cha của nút bị tràn
            Node parent = FindParent(root, node);

            // Xóa nút bị tràn khỏi nút cha
            parent.RemoveChild(node);

            // Thêm hai nút con đã tách vào nút cha
            parent.AddChild(splitNodes[0]);
            parent.AddChild(splitNodes[1]);

            if (parent.IsOverflow(this))
            {
                HandleOverflow(parent);
            }
        }
    }

    private Node[] SplitNode(Node node)
    {
        // Tạo hai nút con mới
        Node[] splitNodes = new Node[2];
        splitNodes[0] = new Node(null, node.IsLeaf);
        splitNodes[1] = new Node(null, node.IsLeaf);

        // Phân chia các nút con vào hai nút con mới
        List<Node> children = node.Children.OrderBy(c => CalculateExpansion(splitNodes[0].MBR, c.MBR)).ToList();

        int totalChildren = children.Count;
        int splitIndex = totalChildren / 2;

        splitNodes[0].Children.AddRange(children.GetRange(0, splitIndex));
        splitNodes[1].Children.AddRange(children.GetRange(splitIndex, totalChildren - splitIndex));

        // Tính toán MBR của hai nút con mới
        splitNodes[0].MBR = CalculateMBR(splitNodes[0].Children);
        splitNodes[1].MBR = CalculateMBR(splitNodes[1].Children);

        return splitNodes;
    }

    private Node FindParent(Node current, Node child)
    {
        if (current.IsLeaf || current.Children.Contains(child))
        {
            return current;
        }
        else
        {
            foreach (Node node in current.Children)
            {
                Node parent = FindParent(node, child);
                if (parent != null)
                {
                    return parent;
                }
            }
        }

        return null;
    }

    private Rectangle CalculateMBR(List<Node> nodes)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (Node node in nodes)
        {
            if (node.MBR.MinX < minX)
                minX = node.MBR.MinX;
            if (node.MBR.MinY < minY)
                minY = node.MBR.MinY;
            if (node.MBR.MaxX > maxX)
                maxX = node.MBR.MaxX;
            if (node.MBR.MaxY > maxY)
                maxY = node.MBR.MaxY;
        }

        return new Rectangle(minX, minY, maxX, maxY);
    }

    private double CalculateExpansion(Rectangle mbr, Rectangle newRect)
    {
        double expandedArea = (Math.Max(mbr.MaxX, newRect.MaxX) - Math.Min(mbr.MinX, newRect.MinX)) *
                              (Math.Max(mbr.MaxY, newRect.MaxY) - Math.Min(mbr.MinY, newRect.MinY));
        double originalArea = (mbr.MaxX - mbr.MinX) * (mbr.MaxY - mbr.MinY);
        return expandedArea - originalArea;
    }

    // Tìm kiếm các mbr giao với queryMbr
    public List<Rectangle> Search(Rectangle queryMbr)
    {
        List<Rectangle> result = new List<Rectangle>();
        Search(root, queryMbr, result);
        return result;
    }

    private void Search(Node node, Rectangle queryMbr, List<Rectangle> result)
    {
        if (node == null)
        {
            return;
        }

        if (node.IsLeaf)
        {
            foreach (Node child in node.Children)
            {
                if (child.MBR.Intersects(queryMbr))
                {
                    result.Add(child.MBR);
                }
            }
        }
        else
        {
            foreach (Node child in node.Children)
            {
                if (child.MBR.Intersects(queryMbr))
                {
                    Search(child, queryMbr, result);
                }
            }
        }
    }

    // Xóa mbr khỏi RTree
    public void Delete(Rectangle mbr)
    {
        // Tìm kiếm nút chứa MBR cần xóa
        Node nodeToDelete = SearchNode(root, mbr);

        if (nodeToDelete != null)
        {
            // Xóa MBR khỏi nút chứa nó
            nodeToDelete.Children.RemoveAll(c => c.MBR == mbr);

            // Kiểm tra xem nút chứa MBR có trở thành rỗng sau khi xóa hay không
            if (nodeToDelete.Children.Count == 0)
            {
                // Tìm nút cha của nút cần xóa
                Node parent = FindParent(root, nodeToDelete);

                if (parent != null)
                {
                    // Xóa nút khỏi nút cha
                    parent.Children.Remove(nodeToDelete);

                    // Kiểm tra xem nút cha có trở thành rỗng sau khi xóa nút con hay không
                    if (parent.Children.Count == 0)
                    {
                        // Nếu nút cha trở thành rỗng, loại bỏ nút cha
                        Delete(parent.MBR);
                    }
                    else if (parent.Children.Count < m)
                    {
                        // Nếu số lượng nút con trong nút cha giảm xuống dưới m, thực hiện quá trình tái phân phối
                        Reinsert(parent);
                    }
                }
                else
                {
                    // Nếu nút cần xóa là nút gốc, chỉ cần gán lại gốc là null
                    root = null;
                }
            }
        }
    }

    private Node SearchNode(Node current, Rectangle mbr)
    {
        if (current.IsLeaf)
        {
            // Nếu nút hiện tại là lá, kiểm tra xem có MBR nào khớp không
            foreach (Node child in current.Children)
            {
                if (child.MBR == mbr)
                {
                    return current;
                }
            }
        }
        else
        {
            // Nếu nút hiện tại không phải là lá, tiếp tục tìm kiếm trong từng nút con
            foreach (Node child in current.Children)
            {
                if (child.MBR.Intersects(mbr))
                {
                    Node foundNode = SearchNode(child, mbr);
                    if (foundNode != null)
                    {
                        return foundNode;
                    }
                }
            }
        }

        return null;
    }

    private void Reinsert(Node node)
    {
        List<Node> children = node.Children.ToList();

        // Xóa nút con khỏi nút cha
        node.Children.Clear();

        // Tái chèn nút con vào R-Tree
        foreach (Node child in children)
        {
            Insert(child.MBR);
        }
    }
}
