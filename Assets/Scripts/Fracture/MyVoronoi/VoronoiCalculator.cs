using System.Collections.Generic;
using Fracture.MyTools;
using UnityEngine;

namespace Fracture.MyVoronoi
{
    public class VoronoiCalculator
    {
        private readonly DelaunayCalculator _delaunayCalculator = new();

        /// <summary>
        /// 计算Voronoi图分割
        /// </summary>
        /// <param name="mesh">网格体</param>
        /// <param name="points">破碎种子点</param>
        /// <returns>破碎块网格体列表</returns>
        public void Calculate(Mesh mesh, Vector3[] points, out List<Mesh> meshes, out List<List<int>> adjacentList)
        {
            _delaunayCalculator.Triangulate(points);
            meshes = SegMesh(mesh);
            adjacentList = GetAdjacentList();
        }
        
        /// <summary>
        /// 获取领接表
        /// </summary>
        /// <returns>领接表</returns>
        private List<List<int>> GetAdjacentList()
        {
            var list = new List<List<int>>();
            var count = _delaunayCalculator.GetPointCount();
            for (var i = 0; i < count; i++)
            {
                list.Add(_delaunayCalculator.GetAdjacentPoints(i));
            }
            return list;
        }

        /// <summary>
        /// 切割网格体
        /// </summary>
        /// <param name="mesh">需要切割的网格体</param>
        /// <returns>切割后的网格体列表</returns>
        private List<Mesh> SegMesh(Mesh mesh)
        {
            var newMeshes = new List<Mesh>();
            var count = _delaunayCalculator.GetPointCount();

            for (var i = 0; i < count; i++)
            {
                var newMesh = SegPoint(i, mesh);
                if (newMesh == null)
                {
                    newMeshes.Add(newMesh);
                    continue;
                }
                newMesh.RecalculateNormals();
                newMeshes.Add(newMesh);
            }

            return newMeshes;
        }
        
        /// <summary>
        /// 切割Delaunay剖分中一个顶点对应的Voronoi区块
        /// </summary>
        /// <param name="pi">Delaunay剖分顶点索引</param>
        /// <param name="mesh">需要切割的网格体</param>
        /// <returns>切割得到的区块</returns>
        private Mesh SegPoint(int pi, Mesh mesh)
        {
            var point = _delaunayCalculator.GetPointAt(pi);
            var adjacentPoints = _delaunayCalculator.GetAdjacentPoints(pi);
            var diagram = new TriangleManager();
            diagram.ImportFromMesh(mesh);
            
            // 以每一对顶点的中垂面作为分割面
            foreach (var i in adjacentPoints)
            {
                var otherPoint = _delaunayCalculator.GetPointAt(i);
                var center = (point + otherPoint) / 2;
                var normal = (otherPoint - point).normalized;
                diagram = SegPlane(center, normal, diagram);
            }

            return diagram.ExportToMesh();
        }
        
        /// <summary>
        /// 根据分割面切割网格体，只保留分割面一侧的网格
        /// </summary>
        /// <param name="p">分割面中心点</param>
        /// <param name="n">分割面法矢量（保留法矢量异侧的网格）</param>
        /// <param name="manager">需要切割的网格体（三角网格）</param>
        /// <returns>切割得到的网格体</returns>
        public TriangleManager SegPlane(Vector3 p, Vector3 n, TriangleManager manager)
        {
            var newDiagram = new TriangleManager();
            var newTrianglePoints = new Dictionary<int, List<Point>>();
            var newIntersectPoints = new Dictionary<int, List<Point>>();
            var polygonNormals = new Dictionary<int, Vector3>();

            foreach (var triangle in manager.GetTriangles())
            {
                var points = new Vector3[3];
                var side = new bool[3];
                var polygonId = 0;

                // 读取三角形顶点数据
                for (var i = 0; i < 3; i++)
                {
                    var pointIndex = triangle.PointIndices[i];
                    var point = manager.GetPointAt(pointIndex);
                    if (point == null)
                    {
                        return manager;
                    }
                    points[i] = point.Position;
                    side[i] = MathTool.CalcSide(p, n, points[i]);
                    polygonId = point.PolygonId;
                }

                // 三个顶点都在分割面上侧，无需操作
                if (side[0] && side[1] && side[2])
                {
                    continue;
                }
                
                // 三个顶点都在分割面下侧，直接加入新网格
                if (!side[0] && !side[1] && !side[2])
                {
                    var newPointIndex1 = newDiagram.AddPoint(points[0], polygonId);
                    var newPointIndex2 = newDiagram.AddPoint(points[1], polygonId);
                    var newPointIndex3 = newDiagram.AddPoint(points[2], polygonId);
                    newDiagram.AddTriangleDirectly(newPointIndex1, newPointIndex2, newPointIndex3);
                    continue;
                }
                
                // 检查是否已经记录该多边形的顶点和交点
                if (!newTrianglePoints.ContainsKey(polygonId))
                {
                    newTrianglePoints.Add(polygonId, new List<Point>());
                }
                if (!newIntersectPoints.ContainsKey(polygonId))
                {
                    newIntersectPoints.Add(polygonId, new List<Point>());
                }
                if (!polygonNormals.ContainsKey(polygonId))
                {
                    var normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
                    polygonNormals.Add(polygonId, normal);
                }

                // 三个顶点在分割面异侧，需要计算交点
                for (var i = 0; i < 3; i++)
                {
                    // 添加原顶点
                    var trianglePoint = new Point(points[i], -1);
                    if (!side[i] && !newTrianglePoints[polygonId].Contains(trianglePoint))
                    {
                        newTrianglePoints[polygonId].Add(trianglePoint);
                    }
                    
                    // 添加交点
                    var point = MathTool.CalcIntersection(p, n, points[i], points[(i + 1) % 3]);
                    var interPoint = new Point(point, -1);
                    
                    // 如果没有交点或者交点已经存在，则跳过
                    if (point.x > 1e6 || newIntersectPoints[polygonId].Contains(interPoint))
                    {
                        continue;
                    }
                    
                    // 否则检查交点是否与其他交点共线，如果是则删除中间点，只保留两个端点
                    var size = newIntersectPoints[polygonId].Count;
                    var flag = false;
                    for (var j = 0; j < size - 1; j++)
                    {
                        for (var k = j + 1; k < size; k++)
                        {
                            if (MathTool.IsOnLine(newIntersectPoints[polygonId][j].Position,
                                    newIntersectPoints[polygonId][k].Position, 
                                    point, out var middleIndex))
                            {
                                flag = true;
                                if (middleIndex == 1 || middleIndex == 2)
                                {
                                    newIntersectPoints[polygonId].RemoveAt(middleIndex == 1 ? j : k);
                                    newIntersectPoints[polygonId].Add(interPoint);
                                }
                                break;
                            }
                        }
                        if (flag)
                        {
                            break;
                        }
                    }
                    if (!flag)
                    {
                        newIntersectPoints[polygonId].Add(interPoint);
                    }
                }
            }
            
            // 为被切割的三角形添加新的顶点和三角形
            foreach (var trianglePoints in newTrianglePoints)
            {
                var polygonId = trianglePoints.Key;
                var points = trianglePoints.Value;
                points.AddRange(newIntersectPoints[polygonId]);
                if (points.Count < 3)
                {
                    return newDiagram;
                }
                AddTriangles(ref newDiagram, points, polygonNormals[polygonId], polygonId);
            }
            
            // 为切割面添加顶点和三角形
            newDiagram.BeginNewPolygon();
            var intersectPoints = new List<Point>();
            foreach (var intersectPoint in newIntersectPoints)
            {
                foreach (var point in intersectPoint.Value)
                {
                    if (!intersectPoints.Contains(point))
                    {
                        intersectPoints.Add(point);
                    }
                }
            }
            if (intersectPoints.Count < 3)
            {
                return newDiagram;
            }
            AddTriangles(ref newDiagram, intersectPoints, n);

            return newDiagram;
        }

        /// <summary>
        /// 为一个面添加三角形
        /// </summary>
        /// <param name="manager">三角网格</param>
        /// <param name="points">顶点列表</param>
        /// <param name="normal">该面的法矢量（或近似方向）</param>
        /// <param name="polygonId">该面的id</param>
        private void AddTriangles(ref TriangleManager manager, List<Point> points, Vector3 normal, int polygonId = -1)
        {
            // 按照与第一个点的角度排序
            var firstPoint = points[0].Position;
            points.RemoveAt(0);
            points.Sort((p1, p2) =>
            {
                var vec1 = p1.Position - firstPoint;
                var vec2 = p2.Position - firstPoint;
                if (Vector3.Dot(Vector3.Cross(vec1, vec2), normal) < 0)
                {
                    return 1;
                }
                return -1;
            });

            // 添加顶点和三角形
            var pointIndices = new int[points.Count + 1];
            pointIndices[0] = manager.AddPoint(firstPoint, polygonId);
            pointIndices[1] = manager.AddPoint(points[0].Position, polygonId);
            for (var i = 0; i < points.Count - 1; i++)
            {
                pointIndices[i + 2] = manager.AddPoint(points[i + 1].Position, polygonId);
                manager.AddTriangleDirectly(pointIndices[0], pointIndices[i + 1], pointIndices[i + 2]);
            }
        }
    }
}
