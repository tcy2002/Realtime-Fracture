using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fracture.MyTools
{
    /// <summary>
    /// 顶点
    /// </summary>
    public class Point : IEquatable<Point>
    {
        public Vector3 Position;
        public int PolygonId;
        
        public Point(Vector3 position, int polygonId)
        {
            Position = position;
            PolygonId = polygonId;
        }

        public override int GetHashCode()
        {
            var ix = Mathf.RoundToInt(Position.x * 1000);
            var iy = Mathf.RoundToInt(Position.y * 1000);
            var iz = Mathf.RoundToInt(Position.z * 1000);
            var iw = PolygonId;
            return HashCode.Combine(ix, iy, iz, iw);
        }

        public bool Equals(Point other)
        {
            return MathTool.Approximately(Position, other.Position) && PolygonId == other.PolygonId;
        }
    }

    /// <summary>
    /// 三角形
    /// </summary>
    public class Triangle : IEquatable<Triangle>
    {
        public int[] PointIndices;
        public int Index;

        public Triangle(int pointIndex1, int pointIndex2, int pointIndex3, int index)
        {
            PointIndices = new[] { pointIndex1, pointIndex2, pointIndex3 };
            Index = index;
        }

        public bool HasPoint(int i)
        {
            return PointIndices[0] == i ||
                   PointIndices[1] == i ||
                   PointIndices[2] == i;
        }

        public override int GetHashCode()
        {
            var ix = PointIndices[0];
            var iy = PointIndices[1];
            var iz = PointIndices[2];
            MathTool.Sort3(ref ix, ref iy, ref iz);
            return HashCode.Combine(ix, iy, iz);
        }

        public bool Equals(Triangle other)
        {
            var ix = PointIndices[0];
            var iy = PointIndices[1];
            var iz = PointIndices[2];
            var ox = other.PointIndices[0];
            var oy = other.PointIndices[1];
            var oz = other.PointIndices[2];
            MathTool.Sort3(ref ix, ref iy, ref iz);
            MathTool.Sort3(ref ox, ref oy, ref oz);
            return ix == ox && iy == oy && iz == oz;
        }
    }
    
    /// <summary>
    /// 四面体
    /// </summary>
    public class Tetrahedron
    {
        public readonly int[] TriangleIndices;
        public readonly Vector3 Circumcenter;
        public readonly float Circumradius;

        public Tetrahedron(Vector3[] vertices, int[] triangleIndices)
        {
            if (vertices.Length != 4 || triangleIndices.Length != 4)
            {
                throw new Exception("Tetrahedron must have 4 vertices and 4 triangle indices.");
            }
            TriangleIndices = triangleIndices;
            MathTool.CalcCircumsphere(vertices, out var center, out var radius);
            Circumcenter = center;
            Circumradius = radius;
        }
        
        public bool HasTriangle(int i)
        {
            return TriangleIndices[0] == i ||
                   TriangleIndices[1] == i ||
                   TriangleIndices[2] == i ||
                   TriangleIndices[3] == i;
        }
    }
        
    public class TriangleManager
    {
        public OrderedHash<Point> _points = new();
        public OrderedHash<Triangle> _triangles = new();
        public List<Tetrahedron> _tetrahedrons = new();

        private int _maxPointIndex = -1;
        private int _maxTriangleIndex = -1;
        private int _polygonId = 0;
        
        public int PointCount => _points.Count;
        public int TriangleCount => _triangles.Count;
        public int TetrahedronCount => _tetrahedrons.Count;

        /// <summary>
        /// 计算中心点
        /// </summary>
        /// <returns>中心点位置</returns>
        public Vector3 Center() {
            var center = Vector3.zero;
            foreach (var point in _points)
            {
                center += point.Position;
            }
            return center / _points.Count;
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
            _points.Clear();
            _triangles.Clear();
            _tetrahedrons.Clear();
            _maxPointIndex = -1;
            _maxTriangleIndex = -1;
        }
        
        /// <summary>
        /// 开始一个新的面
        /// </summary>
        public void BeginNewPolygon()
        {
            _polygonId++;
        }

        /// <summary>
        /// 添加一个点
        /// </summary>
        /// <param name="point">添加的点</param>
        /// <param name="polygonId">该点所在的多边形id</param>
        /// <returns>点索引</returns>
        public int AddPoint(Vector3 point, int polygonId = -1)
        {
            var newPoint = new Point(point, polygonId != -1 ? polygonId : _polygonId);
            if (polygonId > _polygonId)
            {
                _polygonId = polygonId;
            }
            
            // 如果已经存在，则直接返回索引
            var index = _points.IndexOf(newPoint);
            if (index != -1)
            {
                return index;
            }
            
            // 否则添加点
            _points.Add(newPoint);
            return ++_maxPointIndex;
        }
        
        /// <summary>
        /// 直接添加一个点，不检查是否重复
        /// </summary>
        /// <param name="point">添加的点</param>
        /// <param name="polygonId">该点所在的多边形id</param>
        /// <returns>点索引</returns>
        public int AddPointDirectly(Vector3 point, int polygonId = -1)
        {
            var newPoint = new Point(point, polygonId != -1 ? polygonId : _polygonId);
            if (polygonId > _polygonId)
            {
                _polygonId = polygonId;
            }
            
            _points.Add(newPoint);
            return ++_maxPointIndex;
        }

        /// <summary>
        /// 根据索引获取点
        /// </summary>
        /// <param name="index">点索引</param>
        /// <returns>点</returns>
        public Point GetPointAt(int index)
        {
            return _points[index];
        }
        
        /// <summary>
        /// 删除一个点
        /// </summary>
        /// <param name="index">需要删除的点索引</param>
        /// <returns>是否删除成功</returns>
        public bool RemovePointAt(int index)
        {
            return _points.RemoveAt(index);
        }
        
        /// <summary>
        /// 清空所有点，同时清空所有三角形和四面体
        /// </summary>
        public void ClearPoints()
        {
            _points.Clear();
            _maxPointIndex = -1;
            ClearTriangles();
            ClearTetrahedrons();
        }
        
        /// <summary>
        /// 获取所有点
        /// </summary>
        /// <returns>点列表</returns>
        public OrderedHash<Point> GetPoints()
        {
            return _points;
        }
        
        /// <summary>
        /// 设置新的点列表
        /// </summary>
        /// <param name="newPoints">新的点列表</param>
        public void SetPoints(OrderedHash<Point> newPoints)
        {
            _points = newPoints;
        }

        /// <summary>
        /// 给出三个顶点点的索引，添加一个三角形；此方法开销较大，尽量避免使用
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns>三角形索引</returns>
        public int AddTriangle(int p1, int p2, int p3)
        {
            var newTriangle = new Triangle(p1, p2, p3, ++_maxTriangleIndex);
            
            // 如果已经存在，则直接返回索引
            var index = _triangles.IndexOf(newTriangle);
            if (index != -1)
            {
                _maxTriangleIndex--;
                return _triangles[index].Index;
            }
            
            // 否则添加三角形
            _triangles.Add(newTriangle);
            return newTriangle.Index;
        }

        /// <summary>
        /// 直接添加一个三角形，重复时不添加，不检查顶点的多边形id
        /// </summary>
        /// <param name="p1">顶点1</param>
        /// <param name="p2">顶点2</param>
        /// <param name="p3">顶点3</param>
        /// <returns>三角形索引</returns>
        public int AddTriangleDirectly(int p1, int p2, int p3)
        {
            var newTriangle = new Triangle(p1, p2, p3, ++_maxTriangleIndex);
            _triangles.Add(newTriangle);
            return newTriangle.Index;
        }
        
        /// <summary>
        /// 获取三角形
        /// </summary>
        /// <param name="index">三角形索引</param>
        /// <returns>三角形</returns>
        public Triangle GetTriangleAt(int index)
        {
            // 如果索引超出范围，或者索引不正确，则遍历向前查找
            if (index >= _triangles.Count || _triangles[index].Index > index)
            {
                for (var i = index >= _triangles.Count ? _triangles.Count - 1 : index; i >= 0; i--)
                {
                    if (_triangles[i].Index == index)
                    {
                        return _triangles[i];
                    }
                }
                return null;
            }
            
            // 如果没有发生变化，则直接返回
            return _triangles[index];
        }
        
        /// <summary>
        /// 删除一个三角形
        /// </summary>
        /// <param name="index">需要删除的三角形索引</param>
        /// <returns>是否删除成功</returns>
        public bool RemoveTriangleAt(int index)
        {
            // 如果索引超出范围，或者索引不正确，则遍历向前查找
            if (index >= _triangles.Count || _triangles[index].Index > index)
            {
                for (var i = index >= _triangles.Count ? _triangles.Count - 1 : index; i >= 0; i--)
                {
                    if (_triangles[i].Index == index)
                    {
                        return _triangles.RemoveAt(i);
                    }
                }
            }
            
            // 如果没有发生变化，则直接删除
            return _triangles.RemoveAt(index);
        }
        
        /// <summary>
        /// 获取所有三角形
        /// </summary>
        /// <returns>三角形列表</returns>
        public OrderedHash<Triangle> GetTriangles()
        {
            return _triangles;
        }

        /// <summary>
        /// 设置新的三角形列表
        /// </summary>
        /// <param name="newTriangles">新的三角形列表</param>
        public void SetTriangles(OrderedHash<Triangle> newTriangles)
        {
            _triangles = newTriangles;
        }
        
        /// <summary>
        /// 清空所有三角形，同时清空所有四面体
        /// </summary>
        public void ClearTriangles()
        {
            _triangles.Clear();
            _maxTriangleIndex = -1;
            ClearTetrahedrons();
        }

        /// <summary>
        /// 给定四个点的索引，添加一个四面体
        /// </summary>
        /// <param name="p1">顶点1索引</param>
        /// <param name="p2">顶点2索引</param>
        /// <param name="p3">顶点3索引</param>
        /// <param name="p4">顶点4索引</param>
        /// <param name="t1">三角形1索引</param>
        /// <param name="t2">三角形2索引</param>
        /// <param name="t3">三角形3索引</param>
        /// <param name="t4">三角形4索引</param>
        public void AddTetrahedron(int p1, int p2, int p3, int p4, int t1, int t2, int t3, int t4)
        {
            var vs = new Vector3[4];
            vs[0] = _points[p1].Position;
            vs[1] = _points[p2].Position;
            vs[2] = _points[p3].Position;
            vs[3] = _points[p4].Position;
            var tis = new[] { t1, t2, t3, t4 };
            _tetrahedrons.Add(new Tetrahedron(vs, tis));
        }
        
        /// <summary>
        /// 获取一个四面体
        /// </summary>
        /// <param name="index">四面体索引</param>
        /// <returns>四面体</returns>
        public Tetrahedron GetTetrahedronAt(int index)
        {
            return _tetrahedrons[index];
        }
        
        /// <summary>
        /// 删除一个四面体
        /// </summary>
        /// <param name="index">需要删除的四面体索引</param>
        /// <returns>是否删除成功</returns>
        public bool RemoveTetrahedronAt(int index)
        {
            if (index < 0 || index >= _tetrahedrons.Count)
            {
                return false;
            }
            _tetrahedrons.RemoveAt(index);
            return true;
        }
        
        /// <summary>
        /// 获取所有四面体
        /// </summary>
        /// <returns>四面体列表</returns>
        public List<Tetrahedron> GetTetrahedrons()
        {
            return _tetrahedrons;
        }

        /// <summary>
        /// 设置新的四面体列表
        /// </summary>
        /// <param name="newTetrahedrons">新的四面体列表</param>
        public void SetTetrahedrons(List<Tetrahedron> newTetrahedrons)
        {
            _tetrahedrons = newTetrahedrons;
        }
        
        /// <summary>
        /// 清空所有四面体
        /// </summary>
        public void ClearTetrahedrons()
        {
            _tetrahedrons.Clear();
        }

        /// <summary>
        /// 导出到网格数据
        /// </summary>
        /// <returns>导出的网格体</returns>
        public Mesh ExportToMesh()
        {
            // 添加顶点
            var vertices = new Vector3[_points.Count];
            for (var i = 0; i < _points.Count; i++)
            {
                vertices[i] = _points[i].Position;
            }

            // 添加三角形
            var triangles = new int[_triangles.Count * 3];
            var j = 0;
            foreach (var triangle in _triangles)
            {
                triangles[j] = triangle.PointIndices[0];
                triangles[j + 1] = triangle.PointIndices[1];
                triangles[j + 2] = triangle.PointIndices[2];
                j += 3;
            }
            
            if (vertices.Length < 3 || triangles.Length < 4)
            {
                return null;
            }
            return new Mesh
            {
                vertices = vertices, 
                triangles = triangles
            };
        }

        /// <summary>
        /// 从网格数据导入
        /// </summary>
        /// <param name="mesh"></param>
        public void ImportFromMesh(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            
            var added = new int[vertices.Length];
            for (var i = 0; i < added.Length; i++)
            {
                added[i] = -1;
            }
            var addedCount = 0;

            var queue = new Queue<int>();
            
            // 添加点
            while (addedCount < vertices.Length)
            {
                // 找到一个未添加的点
                for (var i = 0; i < added.Length; i++)
                {
                    if (added[i] == -1)
                    {
                        queue.Enqueue(i);
                        added[i] = AddPoint(vertices[i]);
                        addedCount++;
                        break;
                    }
                }
                
                // 添加该点及其相邻的点
                while (queue.Count != 0)
                {
                    var p = queue.Dequeue();
                    for (var i = 0; i < triangles.Length; i += 3)
                    {
                        if (triangles[i] != p && triangles[i + 1] != p && triangles[i + 2] != p)
                        {
                            continue;
                        }
                        var v1 = triangles[i] != p ? triangles[i] : triangles[i + 1];
                        var v2 = triangles[i + 2] != p ? triangles[i + 2] : triangles[i + 1];
                        if (added[v1] == -1)
                        {
                            queue.Enqueue(v1);
                            added[v1] = AddPoint(vertices[v1]);
                            addedCount++;
                        }
                        if (added[v2] == -1)
                        {
                            queue.Enqueue(v2);
                            added[v2] = AddPoint(vertices[v2]);
                            addedCount++;
                        }
                    }
                }
                
                // 添加下一个多边形
                BeginNewPolygon();
            }

            // 添加三角形
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var v1 = added[triangles[i]];
                var v2 = added[triangles[i + 1]];
                var v3 = added[triangles[i + 2]];
                AddTriangleDirectly(v1, v2, v3);
            }
        }
    }
}
