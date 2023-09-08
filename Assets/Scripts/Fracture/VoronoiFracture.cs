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
        private readonly VoronoiCalculatorOld _voronoiCalculatorOld = new();
        private readonly PointsGenerator _pointsGenerator = new();

        public PointsGenerator.FracType type = PointsGenerator.FracType.SemiSphere;
        public float range = 0.4f;
        public int numSurface = 8;
        public int numInside = 4;
        public int numOutside = 4;
        public float minForce = 100.0f;
        public float minVolume = 0.05f;
        public float gapTime = 0.2f;
        public float restitution = 0.5f;

        private bool _flag;
        private Mesh _mesh;
        private float _volume;
        private float _startTime = 0.0f;
        private float _minVolume;
        
        public bool ifSpring;
        public bool ifCurve;
        
        void Start()
        {
            _mesh = GetComponent<MeshFilter>().mesh;
            _volume = MathTool.CalcVolume(_mesh);
            _pointsGenerator.Type = type;
            _pointsGenerator.Range = new Vector3(
                range / transform.localScale.x,
                range / transform.localScale.y,
                range / transform.localScale.z
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
            
            var points = GeneratePoints(other.contacts[0].point, other.contacts[0].normal);
            GenerateNewMeshes(points);

            // 销毁原来的网格体
            Destroy(gameObject);
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
        Vector3[] GeneratePoints(Vector3 collisionPoint, Vector3 collisionNormal)
        {
            // 计算物体坐标系下的碰撞点、碰撞法线
            collisionPoint = transform.InverseTransformPoint(collisionPoint);
            collisionNormal = -transform.InverseTransformDirection(collisionNormal);
            
            // 生成碎片种子点
            return _pointsGenerator.GeneratePoints(collisionPoint, collisionNormal);
        }
        
        /// <summary>
        /// 生成新的网格体：在此函数中加入对各个碎块的物理约束，详见下方TODO
        /// </summary>
        /// <param name="points">种子点列表</param>
        void GenerateNewMeshes(Vector3[] points)
        {
            // 计算碰撞点处的Voronoi图分割
            List<Mesh> newMeshes;
            List<List<int>> adjacentList;
            if (ifCurve)
            {
                _voronoiCalculatorOld.Calculate(_mesh, points, out newMeshes, out adjacentList);
            }
            else
            {
                _voronoiCalculator.Calculate(_mesh, points, out newMeshes, out adjacentList);
            }
            var newFragments = new List<GameObject>();

            // 生成新的网格体
            for (var i = 0; i < newMeshes.Count; i++)
            {
                var newMesh = newMeshes[i];
                if (newMesh == null)
                {
                    newFragments.Add(null);
                    continue;
                }
                var mass = MathTool.CalcVolume(newMesh);
                if (mass < 0.0001f)
                {
                    newFragments.Add(null);
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

                // 添加其他组件
                foreach (var c in GetComponents<MonoBehaviour>())
                {
                    var s = newGameObject.AddComponent(c.GetType());
                    foreach (var field in c.GetType().GetFields())
                    {
                        field.SetValue(s, field.GetValue(c));
                    }
                }
                
                newFragments.Add(newGameObject);
            }

            if (ifSpring)
            {
                HashSet<(int, int)> connectedPairs = new HashSet<(int, int)>();
                for (var i = 0; i < newFragments.Count; i++)
                {
                    if (newFragments[i] == null)
                    {
                        continue;
                    }
                    var neighbors = adjacentList[i];
                    foreach (var neighborIndex in neighbors)
                    {
                        if (newFragments[neighborIndex] == null)
                        {
                            continue;
                        }
                        var pair = (Mathf.Min(i, neighborIndex), Mathf.Max(i, neighborIndex));
                        if (connectedPairs.Contains(pair))
                        {
                            continue;
                        }
                        var joint = newFragments[i].AddComponent<SpringJoint>();
                        joint.connectedBody = newFragments[neighborIndex].GetComponent<Rigidbody>();
                        joint.damper = 10;
                        joint.spring = 100000;
                        joint.breakForce = 0.01f;
                        joint.breakTorque = 0.01f;
                        joint.minDistance = 0;
                        joint.maxDistance = 0;
                        joint.enableCollision = true;
                        joint.anchor = points[i];
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = points[neighborIndex];
                        Vector3 v1 = newFragments[i].transform.TransformPoint(points[i]);
                        Vector3 v2 = newFragments[neighborIndex].transform.TransformPoint(points[neighborIndex]);
                        joint.tolerance = Vector3.Distance(v1, v2);
                        connectedPairs.Add(pair);
                    }
                }

            }
        }
    }
}
