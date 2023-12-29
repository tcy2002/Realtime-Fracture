using System;
using System.Collections.Generic;
using Fracture.MyTools;
using UnityEngine;
using Fracture.MyVoronoi;

namespace Fracture
{
    public class VoronoiFracture : MonoBehaviour
    {
        private readonly VoronoiCalculator _voronoiCalculator = new();
        private readonly PointsGenerator _pointsGenerator = new();

        public float range = 0.4f;
        public int numSurface = 8;
        public int numInside = 4;
        public int numOutside = 4;
        public float minForce = 100.0f;
        public float minVolume = 0.05f;
        public float gapTime = 0.2f;
        public float restitution = 0.5f;
        public float explosionForce = 10.0f;
        public PointsGenerator.FracType explosionType = PointsGenerator.FracType.RangeBlast;

        private bool _flag;
        private Mesh _mesh;
        private float _volume;
        private float _startTime;
        private float _minVolume;

        void Start()
        {
            _mesh = GetComponent<MeshFilter>().mesh;
            _volume = MathTool.CalcVolume(_mesh);
            _pointsGenerator.Type = explosionType;
            var mag = explosionType == PointsGenerator.FracType.RangeBlast ? 2.0f : 1.0f;
            _pointsGenerator.Range = new Vector3(
                mag * range / transform.localScale.x,
                mag * range / transform.localScale.y,
                mag * range / transform.localScale.z
            );
            _pointsGenerator.NumInside = numInside;
            _pointsGenerator.NumSurface = numSurface;
            _pointsGenerator.NumOutside = numOutside;
            _minVolume = minVolume / (transform.localScale.x * transform.localScale.y * transform.localScale.z);
        }

        void Update()
        {
            if (_startTime < gapTime)
            {
                _startTime += Time.deltaTime;
            }
        }

        void OnCollisionEnter(Collision other)
        {
            if (_startTime < gapTime || !other.gameObject.CompareTag("Bullet") || _flag)
            {
                return;
            }
            
            // 计算垂直于碰撞面的相对速度
            var relativeVelocity = Vector3.Project(other.relativeVelocity, other.contacts[0].normal);

            // 当体积、碰撞力大于阈值时才进行碎裂
            var force = CalcForce(relativeVelocity, other.rigidbody.mass) / _volume;
            if (_volume < _minVolume || force < minForce)
            {
                return;
            }
            
            _flag = true; // 防止多次碰撞
            
            GeneratePoints(other.contacts[0].point, other.contacts[0].normal, out var points);
            GenerateNewMeshes(points);

            // 销毁原来的网格体和子弹
            Destroy(gameObject);
            Destroy(other.gameObject);
        }

        /// <summary>
        /// 计算碰撞力
        /// </summary>
        /// <param name="relativeVelocity">相对速度</param>
        /// <param name="otherMass">碰撞物体的质量</param>
        /// <returns>碰撞力</returns>
        float CalcForce(Vector3 relativeVelocity, float otherMass)
        {
            var time = Time.deltaTime;
            float mass;
            try
            {
                mass = GetComponent<Rigidbody>().mass;
            }
            catch
            {
                mass = 1;
            }
            var velocity = (1 + restitution) * relativeVelocity.magnitude * otherMass / (mass + otherMass);
            return mass * velocity / time;
        }
        
        /// <summary>
        /// 生成破碎种子点
        /// </summary>
        /// <param name="collisionPoint">世界坐标系下的碰撞点</param>
        /// <param name="collisionNormal">世界坐标系下的碰撞法线</param>
        /// <returns>种子点列表</returns>
        void GeneratePoints(Vector3 collisionPoint, Vector3 collisionNormal, out SamPoint[] points)
        {
            // 计算物体坐标系下的碰撞点、碰撞法线
            collisionPoint = transform.InverseTransformPoint(collisionPoint);
            collisionNormal = -transform.InverseTransformDirection(collisionNormal);
            
            // 生成碎片种子点
            _pointsGenerator.GeneratePoints(collisionPoint, collisionNormal, out points);
        }
        
        /// <summary>
        /// 生成新的网格体：在此函数中加入对各个碎块的物理约束，详见下方TODO
        /// </summary>
        /// <param name="points">种子点列表</param>
        void GenerateNewMeshes(SamPoint[] points)
        {
            // 计算碰撞点处的Voronoi图分割
            _voronoiCalculator.Calculate(_mesh, points, out var insideMeshes, out var outsideMeshes);

            // 生成新的残余网格体，为复合网格体
            if (outsideMeshes.Count > 0)
            {
                var gameObject = new GameObject();
                gameObject.transform.position = transform.position;
                gameObject.transform.rotation = transform.rotation;
                gameObject.transform.localScale = transform.localScale;
                
                // 添加子网格体
                var mf = gameObject.AddComponent<MeshFilter>();
                var mr = gameObject.AddComponent<MeshRenderer>();
                var combine = new CombineInstance[outsideMeshes.Count];
                for (var i = 0; i < outsideMeshes.Count; i++)
                {
                    combine[i].mesh = outsideMeshes[i];
                    combine[i].transform = Matrix4x4.identity;
                }
                mf.mesh = new Mesh();
                mf.mesh.CombineMeshes(combine);
                mr.material = GetComponent<MeshRenderer>().material;
            
                // 添加刚体和碰撞体
                var rb0 = gameObject.AddComponent<Rigidbody>();
                var mass0 = .0f;
                foreach (var mesh in outsideMeshes)
                {
                    mass0 += MathTool.CalcVolume(mesh);
                }
                rb0.mass = mass0;
                rb0.isKinematic = true;
                var mc0 = gameObject.AddComponent<MeshCollider>();
                mc0.sharedMesh = mf.mesh;
                mc0.convex = false;
            }

            // 生成新的破碎网格体并添加向外爆破的速度，目前仅支持破碎一次
            foreach (var newMesh in insideMeshes)
            {
                if (newMesh == null)
                {
                    continue;
                }
                var mass = MathTool.CalcVolume(newMesh);
                if (mass < 0.0001f)
                {
                    continue;
                }
                
                var newGameObject = new GameObject();
                
                // 保持原来的位置、旋转、缩放
                newGameObject.transform.position = transform.position;
                newGameObject.transform.rotation = transform.rotation;
                newGameObject.transform.localScale = transform.localScale;
                
                // 添加网格体和材质
                newGameObject.AddComponent<MeshFilter>().mesh = newMesh;
                newGameObject.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;

                // 添加刚体和碰撞体
                var rb = newGameObject.AddComponent<Rigidbody>();
                rb.mass = mass;
                var mag = explosionType == PointsGenerator.FracType.RangeBlast ? 4.0f : 1.0f;
                rb.AddExplosionForce(explosionForce * mag, transform.position, 
                    range * mag, 0, ForceMode.Impulse);
                try
                {
                    var mc = newGameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = newMesh;
                    mc.convex = true;
                }
                catch (Exception)
                {
                    // 忽略
                }
            }
        }
    }
}
