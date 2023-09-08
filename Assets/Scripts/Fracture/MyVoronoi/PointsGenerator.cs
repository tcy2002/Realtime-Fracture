using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fracture.MyVoronoi
{
    public class PointsGenerator
    {
        public enum FracType
        {
            SemiSphere
        }

        public FracType Type { get; set; } = FracType.SemiSphere;
        public Vector3 Range { get; set; } = new (0.5f, 0.5f, 0.5f);
        public int NumInside { get; set; }
        public int NumSurface { get; set; }
        public int NumOutside { get; set; }
        
        private float _outsideRadius = 1.2f;
        
        /// <summary>
        /// 产生破碎种子点
        /// </summary>
        /// <param name="collisionPoint">碰撞点</param>
        /// <param name="collisionNormal">碰撞点法矢量</param>
        /// <returns>种子点列表</returns>
        public Vector3[] GeneratePoints(Vector3 collisionPoint, Vector3 collisionNormal)
        {
            switch (Type)
            {
                case FracType.SemiSphere:
                    return GenerateSemiSpherePoints(collisionPoint, collisionNormal);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 在碰撞点附近的半球范围内随机生成种子点
        /// </summary>
        /// <param name="collisionPoint">碰撞点</param>
        /// <param name="collisionNormal">碰撞点法矢量</param>
        /// <returns>种子点列表</returns>
        private Vector3[] GenerateSemiSpherePoints(Vector3 collisionPoint, Vector3 collisionNormal)
        {
            var points = new Vector3[NumSurface + NumInside + NumOutside];
            var rotation = Quaternion.FromToRotation(Vector3.forward, -collisionNormal);
            
            // 在表面生成随机点
            for (var i = 0; i < NumSurface; i++)
            {
                var theta = Random.Range(0, 2 * Mathf.PI);
                var radius = Random.Range(0, 1.0f);
                var x = radius * Mathf.Cos(theta);
                var y = radius * Mathf.Sin(theta);
                var point = new Vector3(x, y, 0);
                points[i] = rotation * point;
                points[i].x *= Range.x;
                points[i].y *= Range.y;
                points[i].z *= Range.z;
                points[i] += collisionPoint;
            }
            
            // 在半球内生成随机点
            for (var i = NumSurface; i < NumSurface + NumInside; i++)
            {
                var theta = Random.Range(0, Mathf.PI / 2);
                var phi = Random.Range(0, 2 * Mathf.PI);
                var radius = Random.Range(0, 1.0f);
                var x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                var y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
                var z = radius * Mathf.Cos(theta);
                var point = new Vector3(x, y, z);
                points[i] = rotation * point;
                points[i].x *= Range.x;
                points[i].y *= Range.y;
                points[i].z *= Range.z;
                points[i] += collisionPoint;
            }
            
            // 在半球外生成随机点
            for (var i = NumSurface + NumInside; i < NumSurface + NumInside + NumOutside; i++)
            {
                var theta = Random.Range(0, Mathf.PI / 2);
                var phi = Random.Range(0, 2 * Mathf.PI);
                var x = _outsideRadius * Mathf.Sin(theta) * Mathf.Cos(phi);
                var y = _outsideRadius * Mathf.Sin(theta) * Mathf.Sin(phi);
                var z = _outsideRadius * Mathf.Cos(theta);
                var point = new Vector3(x, y, z);
                points[i] = rotation * point;
                points[i].x *= Range.x;
                points[i].y *= Range.y;
                points[i].z *= Range.z;
                points[i] += collisionPoint;
            }

            return points;
        }
    }
}
