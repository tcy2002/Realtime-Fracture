using System;
using UnityEngine;

namespace Fracture.MyTools
{
    public class MathTool
    {
        public const float Epsilon = 1e-4f;
        
        /// <summary>
        /// 判断两个浮点数是否近似相等
        /// </summary>
        /// <param name="a">浮点数1</param>
        /// <param name="b">浮点数2</param>
        /// <returns>是否近似相等</returns>
        public static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) < Epsilon;
        }
        
        /// <summary>
        /// 判断两个Vector3向量是否近似相等
        /// </summary>
        /// <param name="a">向量1</param>
        /// <param name="b">向量2</param>
        /// <returns>是否近似相等</returns>
        public static bool Approximately(Vector3 a, Vector3 b)
        {
            return Approximately(a.x, b.x) && 
                   Approximately(a.y, b.y) && 
                   Approximately(a.z, b.z);
        }
        
        /// <summary>
        /// 计算向量列表的平均值
        /// </summary>
        /// <param name="list">输入的向量列表</param>
        /// <returns>平均值</returns>
        public static Vector3 Average(Vector3[] list)
        {
            var avg = Vector3.zero;
            foreach (var v in list)
            {
                avg += v;
            }
            return avg / list.Length;
        }

        /// <summary>
        /// 计算网格的体积
        /// </summary>
        /// <param name="mesh">需要计算的网格体</param>
        /// <returns>体积</returns>
        public static float CalcVolume(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var volume = 0f;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];
                // 体积公式：V = 1/6 * (v1 x v2) * v3
                volume += Vector3.Dot(Vector3.Cross(v1, v2), v3) / 6;
            }
            return volume;
        }
        
        /// <summary>
        /// 计算四面体的外接球球心和半径
        /// </summary>
        /// <param name="vertices">四面体顶点，数目为4</param>
        /// <param name="center">返回的外接球球心</param>
        /// <param name="radius">返回的外接球半径</param>
        public static void CalcCircumsphere(Vector3[] vertices, out Vector3 center, out float radius)
        {
            // 顶点数目不为4，无法计算
            if (vertices.Length != 4)
            {
                throw new Exception("Vertices count must be 4.");
            }
            
            // 4个顶点
            var v1 = vertices[0];
            var v2 = vertices[1];
            var v3 = vertices[2];
            var v4 = vertices[3];
            
            // 3个边矢量
            var v1v2 = v2 - v1;
            var v1v3 = v3 - v1;
            var v1v4 = v4 - v1;
            
            // 3个边的中点
            var v1v2m = (v1 + v2) / 2;
            var v1v3m = (v1 + v3) / 2;
            var v1v4m = (v1 + v4) / 2;

            // 2个面的法矢量
            var v1v2v3n = Vector3.Cross(v1v2, v1v3);
            var v1v3v4n = Vector3.Cross(v1v3, v1v4);
            
            // 面1的外接圆圆心
            var v1v2n = Vector3.Cross(v1v2, v1v2v3n);
            var k1 = Vector3.Dot(v1v3m - v1v2m, v1v3) / Vector3.Dot(v1v2n, v1v3);
            var p1 = v1v2m + k1 * v1v2n;

            // 面2的外接圆圆心
            var v1v3n = Vector3.Cross(v1v3, v1v3v4n);
            var k2 = Vector3.Dot(v1v4m - v1v3m, v1v4) / Vector3.Dot(v1v3n, v1v4);
            var p2 = v1v3m + k2 * v1v3n;

            // 外接球球心
            var k3 = Vector3.Dot(p2 - p1, v1v4) / Vector3.Dot(v1v2v3n, v1v4);
            center = p1 + k3 * v1v2v3n;
            
            // 外接球半径
            radius = Vector3.Distance(center, v1);
        }
        
        /// <summary>
        /// 计算平面与线段的交点
        /// </summary>
        /// <param name="p">平面中心点</param>
        /// <param name="n">平面法矢量</param>
        /// <param name="v1">线段端点1</param>
        /// <param name="v2">线段端点2</param>
        /// <returns>交点，若线段与平面平行或不相交，返回Vector3.one * float.MaxValue</returns>
        public static Vector3 CalcIntersection(Vector3 p, Vector3 n, Vector3 v1, Vector3 v2)
        {
            var d = Vector3.Dot(v2 - v1, n);
            if (Approximately(d, 0))
            {
                // 线段与切割面平行
                return Vector3.one * float.MaxValue;
            }

            var t = Vector3.Dot(p - v1, n) / d;
            if (t < 0 || t > 1)
            {
                // 交点不在线段上
                return Vector3.one * float.MaxValue;
            }

            return v1 + t * (v2 - v1);
        }
        
        /// <summary>
        /// 判断顶点在平面的哪一侧
        /// </summary>
        /// <param name="p">平面中心点</param>
        /// <param name="n">平面法矢量</param>
        /// <param name="vertex">顶点坐标</param>
        /// <returns></returns>
        public static bool CalcSide(Vector3 p, Vector3 n, Vector3 vertex)
        {
            var d = Vector3.Dot(vertex - p, n);
            return d > 0;
        }

        /// <summary>
        /// 判断点是否在球内
        /// </summary>
        /// <param name="point">点坐标</param>
        /// <param name="center">球心坐标</param>
        /// <param name="radius">球半径</param>
        /// <returns>是否在球内</returns>
        public static bool IsInsideSphere(Vector3 point, Vector3 center, float radius)
        {
            var dist = Vector3.Distance(point, center);
            return dist < radius - Epsilon;
        }

        /// <summary>
        /// 判断3个点是否在同一条直线上
        /// </summary>
        /// <param name="point1">点1</param>
        /// <param name="point2">点2</param>
        /// <param name="point3">点3</param>
        /// <param name="middleIndex">中间点是哪一个，0，不共线，1：point1，2：point2,3：point3,</param>
        /// <returns>是否在同一直线上</returns>
        public static bool IsOnLine(Vector3 point1, Vector3 point2, Vector3 point3, out int middleIndex)
        {
            var v1 = point2 - point1;
            var v2 = point3 - point1;
            var n = Vector3.Cross(v1.normalized, v2.normalized);
            
            if (!Approximately(n.sqrMagnitude, 0))
            {
                middleIndex = 0;
                return false;
            }

            var d = Vector3.Dot(v1, v2);
            if (d < 0)
            {
                middleIndex = 1;
                return true;
            }

            d = Vector3.Dot(-v1, point3 - point2);
            if (d < 0)
            {
                middleIndex = 2;
                return true;
            }
            
            middleIndex = 3;
            return true;
        }

        /// <summary>
        /// 判断点是否在平面内
        /// </summary>
        /// <param name="point">平面内三个不共线点的坐标</param>
        /// <param name="vertices">三角形顶点</param>
        /// <returns>是否在平面内</returns>
        public static bool IsOnPlane(Vector3 point, Vector3[] vertices)
        {
            if (vertices.Length != 3)
            {
                throw new Exception("Vertices count must be 3.");
            }
            
            var v1 = vertices[0];
            var v2 = vertices[1];
            var v3 = vertices[2];
            
            var v1v2 = v2 - v1;
            var v1v3 = v3 - v1;
            var v1p = point - v1;
            
            var n = Vector3.Cross(v1v2, v1v3);
            var res = Vector3.Dot(v1p, n);

            return Mathf.Approximately(res, 0);
        }

        /// <summary>
        /// 将3个无符号整数按从小到大排序
        /// </summary>
        /// <param name="i1">无符号整数1</param>
        /// <param name="i2">无符号整数2</param>
        /// <param name="i3">无符号整数1</param>
        public static void Sort3(ref int i1, ref int i2, ref int i3)
        {
            // 冒泡排序
            if (i1 > i2)
            {
                (i1, i2) = (i2, i1);
            }
            if (i2 > i3)
            {
                (i2, i3) = (i3, i2);
            }
            if (i1 > i2)
            {
                (i1, i2) = (i2, i1);
            }
        }
    }
}
