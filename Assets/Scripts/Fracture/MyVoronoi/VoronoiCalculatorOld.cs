using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fracture.MyTools;
using UnityEngine;

namespace Fracture.MyVoronoi
{
    public class VoronoiCalculatorOld
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
                    continue;
                }
                newMesh.RecalculateNormals();
                newMesh.RecalculateTangents();
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
            var inters1 = new List<Vector3>();
            var inters2 = new List<Vector3>();

            foreach (var triangle in manager.GetTriangles())
            {
                var pointIndex1 = triangle.PointIndices[0];
                var pointIndex2 = triangle.PointIndices[1];
                var pointIndex3 = triangle.PointIndices[2];
                
                var point1 = manager.GetPointAt(pointIndex1);
                var point2 = manager.GetPointAt(pointIndex2);
                var point3 = manager.GetPointAt(pointIndex3);

                var side1 = MathTool.CalcSide(p, n, point1.Position);
                var side2 = MathTool.CalcSide(p, n, point2.Position);
                var side3 = MathTool.CalcSide(p, n, point3.Position);

                if (side1 && side2 && side3)
                {
                    // 三个顶点都在分割面上侧，无需操作
                    continue;
                }
                if (!side1 && !side2 && !side3)
                {
                    // 三个顶点都在分割面下侧，直接加入新网格
                    var newPointIndex1 = newDiagram.AddPoint(point1.Position, point1.PolygonId);
                    var newPointIndex2 = newDiagram.AddPoint(point2.Position, point1.PolygonId);
                    var newPointIndex3 = newDiagram.AddPoint(point3.Position, point1.PolygonId);
                    newDiagram.AddTriangleDirectly(newPointIndex1, newPointIndex2, newPointIndex3);
                    continue;
                }
                
                // 三个顶点位于切割面两侧，需要进行切割，删除原三角形，添加新三角形
                Vector3 p1, p2, p3;
                bool side;

                if (side1 == side2)
                {
                    p1 = point1.Position;
                    p2 = point2.Position;
                    p3 = point3.Position;
                    side = side3;
                }
                else if (side1 == side3)
                {
                    p1 = point3.Position;
                    p2 = point1.Position;
                    p3 = point2.Position;
                    side = side2;
                }
                else
                {
                    p1 = point2.Position;
                    p2 = point3.Position;
                    p3 = point1.Position;
                    side = side1;
                }
                    
                // 计算交点
                var interPoint1 = MathTool.CalcIntersection(p, n, p1, p3);
                var interPoint2 = MathTool.CalcIntersection(p, n, p2, p3);
                if (interPoint1.x > 1e6 || interPoint2.x > 1e6)
                {
                    continue;
                }
                
                // 添加交点
                var interPointIndex1 = newDiagram.AddPoint(interPoint1, point1.PolygonId);
                var interPointIndex2 = newDiagram.AddPoint(interPoint2, point1.PolygonId);
                inters1.Add(side ? interPoint1 : interPoint2);
                inters2.Add(side ? interPoint2 : interPoint1);
                
                // 添加三角形
                if (side)
                {
                    var newPointIndex1 = newDiagram.AddPoint(p1, point1.PolygonId);
                    var newPointIndex2 = newDiagram.AddPoint(p2, point1.PolygonId);
                    newDiagram.AddTriangleDirectly(newPointIndex1, interPointIndex2, interPointIndex1);
                    newDiagram.AddTriangleDirectly(newPointIndex1, newPointIndex2, interPointIndex2);
                }
                else
                {
                    var newPointIndex3 = newDiagram.AddPoint(p3, point1.PolygonId);
                    newDiagram.AddTriangleDirectly(newPointIndex3, interPointIndex1, interPointIndex2);
                }
            }

            // 为切割面添加顶点和三角形
            if (inters1.Count == 0)
            {
                return newDiagram;
            }
            newDiagram.BeginNewPolygon();
            
            // 添加顶点
            var center = MathTool.Average(inters1.ToArray());
            var centerIndex = newDiagram.AddPoint(center);

            // 添加三角形
            for (var i = 0; i < inters1.Count; i++)
            {
                var pointIndex1 = newDiagram.AddPoint(inters1[i]);
                var pointIndex2 = newDiagram.AddPoint(inters2[i]);
                newDiagram.AddTriangleDirectly(centerIndex, pointIndex1, pointIndex2);
            }

            return newDiagram;
        }
    }
}
