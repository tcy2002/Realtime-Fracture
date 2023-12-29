using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fracture.MyVoronoi
{
    public struct SamPoint
    {
        public enum SamType
        {
            Inside,
            Surface,
            Outside
        }
        public Vector3 Position { get; set; }
        public SamType Type { get; set; }
    }
    
    public class PointsGenerator
    {
        public enum FracType
        {
            BlobBlast,
            RangeBlast
        }

        public FracType Type { get; set; } = FracType.BlobBlast;
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
        public void GeneratePoints(Vector3 collisionPoint, Vector3 collisionNormal, out SamPoint[] points)
        {
            switch (Type)
            {
                case FracType.BlobBlast:
                    GenerateBlobPoints(collisionPoint, collisionNormal, out points);
                    break;
                case FracType.RangeBlast:
                    GenerateRangePoints(collisionPoint, collisionNormal, out points);
                    break;
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
        private void GenerateBlobPoints(Vector3 collisionPoint, Vector3 collisionNormal, out SamPoint[] points)
        {
            points = new SamPoint[NumInside + NumSurface + NumOutside];
            var rotation = Quaternion.FromToRotation(Vector3.forward, -collisionNormal);
            
            // 在表面生成随机点
            for (var i = 0; i < NumSurface; i++)
            {
                var point = Vector3.one;
                while (point.x < -0.501f || point.x > 0.501f || point.y < -0.501f || point.y > 0.501f || point.z < -0.501f || point.z > 0.501f)
                {
                    var theta = Random.Range(0, 2 * Mathf.PI);
                    var radius = Random.Range(0, 1.0f);
                    var x = radius * Mathf.Cos(theta);
                    var y = radius * Mathf.Sin(theta);
                    point = rotation * new Vector3(x, y, 0);
                    point.x *= Range.x;
                    point.y *= Range.y;
                    point.z *= Range.z;
                    point += collisionPoint;
                }
                points[i].Position = point;
                points[i].Type = SamPoint.SamType.Surface;
            }
            
            // 在半球内生成随机点
            for (var i = NumSurface; i < NumSurface + NumInside; i++)
            {
                var point = Vector3.one;
                while (point.x < -0.501f || point.x > 0.501f || point.y < -0.501f || point.y > 0.501f || point.z < -0.501f || point.z > 0.501f)
                {
                    var theta = Random.Range(0, Mathf.PI / 2);
                    var phi = Random.Range(0, 2 * Mathf.PI);
                    var radius = Random.Range(0, 1.0f);
                    var x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                    var y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
                    var z = radius * Mathf.Cos(theta);
                    point = rotation * new Vector3(x, y, z);
                    point.x *= Range.x;
                    point.y *= Range.y;
                    point.z *= Range.z;
                    point += collisionPoint;
                }
                points[i].Position = point;
                points[i].Type = SamPoint.SamType.Inside;
            }
            
            // 在半球外生成随机点
            for (var i = NumSurface + NumInside; i < NumOutside + NumSurface + NumInside; i++)
            {
                var point = Vector3.one;
                while (point.x < -0.501f || point.x > 0.501f || point.y < -0.501f || point.y > 0.501f || point.z < -0.501f || point.z > 0.501f)
                {
                    var theta = Random.Range(0, Mathf.PI / 2);
                    var phi = Random.Range(0, 2 * Mathf.PI);
                    var x = _outsideRadius * Mathf.Sin(theta) * Mathf.Cos(phi);
                    var y = _outsideRadius * Mathf.Sin(theta) * Mathf.Sin(phi);
                    var z = _outsideRadius * Mathf.Cos(theta);
                    point = rotation * new Vector3(x, y, z);
                    point.x *= Range.x;
                    point.y *= Range.y;
                    point.z *= Range.z;
                    point += collisionPoint;
                }
                points[i].Position = point;
                points[i].Type = SamPoint.SamType.Outside;
            }
        }
        
        /// <summary>
        /// 在碰撞点附近的半球范围内随机生成种子点
        /// </summary>
        /// <param name="collisionPoint">碰撞点</param>
        /// <param name="collisionNormal">碰撞点法矢量</param>
        /// <returns>种子点列表</returns>
        private void GenerateRangePoints(Vector3 collisionPoint, Vector3 collisionNormal, out SamPoint[] points)
        {
            points = new SamPoint[NumInside + NumSurface + NumOutside];
            var rotation = Quaternion.FromToRotation(Vector3.forward, -collisionNormal);
            
            // 在表面生成随机点
            for (var i = 0; i < NumSurface; i++)
            {
                var point = Vector3.one;
                while (point.x < -0.501f || point.x > 0.501f || point.y < -0.501f || point.y > 0.501f || point.z < -0.501f || point.z > 0.501f)
                {
                    var theta = Random.Range(0, 2 * Mathf.PI);
                    var radius = Random.Range(0, 2.0f);
                    var x = radius * Mathf.Cos(theta);
                    var y = radius * Mathf.Sin(theta);
                    point = rotation * new Vector3(x, y, 0);
                    point.x *= Range.x;
                    point.y *= Range.y;
                    point.z *= Range.z;
                    point += collisionPoint;
                }
                points[i].Position = point;
                points[i].Type = SamPoint.SamType.Surface;
            }
            
            // 在半球内生成随机点
            for (var i = NumSurface; i < NumSurface + NumInside; i++)
            {
                var point = Vector3.one;
                while (point.x < -0.501f || point.x > 0.501f || point.y < -0.501f || point.y > 0.501f || point.z < -0.501f || point.z > 0.501f)
                {
                    var theta = Random.Range(0, Mathf.PI / 2);
                    var phi = Random.Range(0, 2 * Mathf.PI);
                    var radiusXy = Random.Range(0, 2.0f);
                    var radiusZ = Random.Range(0, 0.5f);
                    var x = radiusXy * Mathf.Sin(theta) * Mathf.Cos(phi);
                    var y = radiusXy * Mathf.Sin(theta) * Mathf.Sin(phi);
                    var z = radiusZ * Mathf.Cos(theta);
                    point = rotation * new Vector3(x, y, z);
                    point.x *= Range.x;
                    point.y *= Range.y;
                    point.z *= Range.z;
                    point += collisionPoint;
                }
                points[i].Position = point;
                points[i].Type = SamPoint.SamType.Inside;
            }
            
            // 在半球外生成随机点
            for (var i = NumSurface + NumInside; i < NumOutside + NumSurface + NumInside; i++)
            {
                var point = Vector3.one;
                while (point.x < -0.501f || point.x > 0.501f || point.y < -0.501f || point.y > 0.501f || point.z < -0.501f || point.z > 0.501f)
                {
                    var theta = Random.Range(0, Mathf.PI / 2);
                    var phi = Random.Range(0, 2 * Mathf.PI);
                    var radiusXy = 2.0f;
                    var radiusZ = 0.5f;
                    var x = radiusXy * _outsideRadius * Mathf.Sin(theta) * Mathf.Cos(phi);
                    var y = radiusXy * _outsideRadius * Mathf.Sin(theta) * Mathf.Sin(phi);
                    var z = radiusZ * _outsideRadius * Mathf.Cos(theta);
                    point = rotation * new Vector3(x, y, z);
                    point.x *= Range.x;
                    point.y *= Range.y;
                    point.z *= Range.z;
                    point += collisionPoint;
                }
                points[i].Position = point;
                points[i].Type = SamPoint.SamType.Outside;
            }
        }
    }
}
