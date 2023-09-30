using System.Collections.Generic;
using Fracture.MyTools;
using UnityEngine;

namespace Fracture.MyVoronoi
{
    public class DelaunayCalculator
    {
        private readonly TriangleManager _manager = new();
        
        /// <summary>
        /// 立体三角剖分：Bowyer-Watson算法
        /// </summary>
        /// <param name="points">种子点列表</param>
        public void Triangulate(Vector3[] points)
        {
            _manager.Clear();
            AddBoundingBox(points);
            BowyerWatson(points);
            RemoveBoundingBox();
        }

        /// <summary>
        /// 获取顶点数量
        /// </summary>
        /// <returns>顶点数量</returns>
        public int GetPointCount()
        {
            return _manager.PointCount;
        }

        /// <summary>
        /// 获取指定顶点
        /// </summary>
        /// <param name="index">顶点索引</param>
        /// <returns>顶点坐标</returns>
        public Vector3 GetPointAt(int index)
        {
            if (index < 0 || index >= _manager.PointCount)
            {
                return Vector3.zero;
            }
            var point = _manager.GetPointAt(index);
            if (point == null)
            {
                return Vector3.zero;
            }
            return point.Position;
        }

        /// <summary>
        /// 获取与指定顶点相邻的顶点列表
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<int> GetAdjacentPoints(int index)
        {
            var adjacentPoints = new List<int>();
            foreach (var triangle in _manager.GetTriangles())
            {
                if (triangle.HasPoint(index))
                {
                    foreach (var point in triangle.PointIndices)
                    {
                        if (point != index && !adjacentPoints.Contains(point))
                        {
                            adjacentPoints.Add(point);
                        }
                    }
                }
            }
            return adjacentPoints;
        }

        /// <summary>
        /// 生成包围盒（超四面体）
        /// </summary>
        /// <param name="points">种子点列表</param>
        private void AddBoundingBox(Vector3[] points)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            // 找到包围盒的最小点和最大点
            foreach (var p in points)
            {
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.z < min.z) min.z = p.z;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
                if (p.z > max.z) max.z = p.z;
            }
            
            // 设置最小包围盒，以防止退化
            min -= Vector3.one * 0.1f;
            max += Vector3.one * 0.1f;
            
            // 计算包围盒4个顶点
            var v1 = min;
            var v2 = new Vector3(max.x, min.y, max.z);
            var v3 = new Vector3(max.x, max.y, min.z);
            var v4 = new Vector3(min.x, max.y, max.z);
            
            // 扩展至中心的3倍距离以包围所有可能的点
            var center = MathTool.Average(new[] { v1, v2, v3, v4 });
            v1 = center + (v1 - center) * 3;
            v2 = center + (v2 - center) * 3;
            v3 = center + (v3 - center) * 3;
            v4 = center + (v4 - center) * 3;
            
            // 添加包围盒的顶点
            _manager.AddPointDirectly(v1);
            _manager.AddPointDirectly(v2);
            _manager.AddPointDirectly(v3);
            _manager.AddPointDirectly(v4);

            // 添加包围盒的三角面片
            _manager.AddTriangleDirectly(0, 1, 2);
            _manager.AddTriangleDirectly(1, 0, 3);
            _manager.AddTriangleDirectly(2, 1, 3);
            _manager.AddTriangleDirectly(2, 3, 0);

            // 添加包围盒的四面体
            _manager.AddTetrahedron(0, 1, 2, 3, 0, 1, 2, 3);
        }
        
        /// <summary>
        /// Bowyer-Watson算法生成四面体网格
        /// </summary>
        /// <param name="points">种子点列表</param>
        private void BowyerWatson(Vector3[] points)
        {
            foreach (var p in points)
            {
                // 寻找不符合Delaunay要求的四面体
                var badTetrahedrons = new List<int>();
                for (var i = 0; i < _manager.TetrahedronCount; i++)
                {
                    var t = _manager.GetTetrahedronAt(i);
                    if (MathTool.IsInsideSphere(p, t.Circumcenter, t.Circumradius))
                    {
                        badTetrahedrons.Add(i);
                    }
                }

                // 寻找需要删除和保留的三角面片
                var badTriangles = new List<int>();
                var goodTriangles = new List<int>();
                // 重复出现，说明是公共面，需要被删除
                foreach (var i in badTetrahedrons)
                {
                    var tetrahedron = _manager.GetTetrahedronAt(i);
                    foreach (var ti in tetrahedron.TriangleIndices)
                    {
                        if (goodTriangles.Contains(ti))
                        {
                            goodTriangles.Remove(ti);
                            badTriangles.Add(ti);
                        }
                        else
                        {
                            goodTriangles.Add(ti);
                        }
                    }
                }

                // 点p在三角形所在平面内，说明这个面不需要构建新的四面体，需要被删除
                for (var i = 0; i < goodTriangles.Count; i++)
                {
                    var triangle = _manager.GetTriangleAt(goodTriangles[i]);
                    if (triangle == null)
                    {
                        return;
                    }
                    var vertices = new Vector3[3];
                    for (var j = 0; j < 3; j++)
                    {
                        var point = _manager.GetPointAt(triangle.PointIndices[j]);
                        if (point == null) { return; }
                        vertices[j] = point.Position;
                    }
                    if (MathTool.IsOnPlane(p, vertices))
                    {
                        badTriangles.Add(goodTriangles[i]);
                        goodTriangles.RemoveAt(i--);
                    }
                }

                // 删除需要删除的四面体
                for (var i = badTetrahedrons.Count - 1; i >= 0; i--)
                {
                    _manager.RemoveTetrahedronAt(badTetrahedrons[i]);
                }
                
                // 删除需要删除的三角面片
                for (var i = badTriangles.Count - 1; i >= 0; i--)
                {
                    _manager.RemoveTriangleAt(badTriangles[i]);
                }

                // 添加新的顶点、三角面片与四面体
                var vi = _manager.AddPoint(p);
                foreach (var i in goodTriangles)
                {
                    var triangle = _manager.GetTriangleAt(i);
                    if (triangle == null)
                    {
                        continue;
                    }
                    var v1 = triangle.PointIndices[0];
                    var v2 = triangle.PointIndices[1];
                    var v3 = triangle.PointIndices[2];
                    
                    var t1 = _manager.AddTriangle(vi, v1, v2);
                    var t2 = _manager.AddTriangle(vi, v2, v3);
                    var t3 = _manager.AddTriangle(vi, v3, v1);

                    _manager.AddTetrahedron(v1, v2, v3, vi, t1, t2, t3, i);
                }
            }
        }

        /// <summary>
        /// 删除包围盒（超四面体），仅保留顶点和三角面片，四面体信息将被删除
        /// </summary>
        private void RemoveBoundingBox()
        {
            // 删除三角形
            var newTriangles = new OrderedHash<Triangle>();
            var oldTriangles = _manager.GetTriangles();
            foreach (var triangle in oldTriangles)
            {
                if (triangle.PointIndices[0] >= 4 && triangle.PointIndices[1] >= 4 && triangle.PointIndices[2] >= 4)
                {
                    triangle.PointIndices[0] -= 4;
                    triangle.PointIndices[1] -= 4;
                    triangle.PointIndices[2] -= 4;
                    newTriangles.Add(triangle);
                }
            }
            _manager.SetTriangles(newTriangles);

            // 删除顶点
            for (var i = 0; i < 4; i++)
            {
                _manager.RemovePointAt(0);
            }

            // 清空四面体
            _manager.ClearTetrahedrons();
        }
    }
}
