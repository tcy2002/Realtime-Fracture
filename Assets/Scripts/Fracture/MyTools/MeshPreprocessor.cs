using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fracture.MyTools
{
    public class MeshPreprocessor : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            var mash = GetComponent<MeshFilter>().mesh;
            var newMesh = Preprocess(mash);
            GetComponent<MeshFilter>().mesh = newMesh;
        }

        /// <summary>
        /// 对网格进行预处理：去除重复顶点，去除重复三角形，去除孤立顶点
        /// </summary>
        /// <param name="mesh">需要预处理的网格体</param>
        /// <returns>处理后的网格体</returns>
        private Mesh Preprocess(Mesh mesh)
        {
            var newMesh = new Mesh();
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var normals = mesh.normals;
            
            
            
            return newMesh;
        }
    }
}
