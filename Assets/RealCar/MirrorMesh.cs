using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MirrorMesh : MonoBehaviour
{
    [SerializeField]
    private MeshFilter filter;

    private List<Vector3> vertexList = new List<Vector3>();
    private List<Vector3> normalList = new List<Vector3>();
    private List<Vector2> uvList = new List<Vector2>();
    private List<int> indexList = new List<int>();

    [SerializeField]
    private float offset = 0.3f;

    // Start is called before the first frame update
    void Start()
    {
        var mesh = new Mesh();

        vertexList.Add(new Vector3(-5, 0, -5));//0番頂点
        vertexList.Add(new Vector3(5, 0, -5)); //1番頂点
        vertexList.Add(new Vector3(-5, 0, 5)); //2番頂点
        vertexList.Add(new Vector3(5, 0, 5));  //3番頂点

        normalList.Add(new Vector3(-offset, 1.0f, -offset / 2.0f).normalized);//0番頂点
        normalList.Add(new Vector3(offset, 1.0f, -offset / 2.0f).normalized); //1番頂点
        normalList.Add(new Vector3(-offset, 1.0f, offset / 2.0f).normalized); //2番頂点
        normalList.Add(new Vector3(offset, 1.0f, offset / 2.0f).normalized);  //3番頂点

        uvList.Add(new Vector2(0, 0));
        uvList.Add(new Vector2(1, 0));
        uvList.Add(new Vector2(0, 1));
        uvList.Add(new Vector2(1, 1));

        indexList.AddRange(new[] { 0, 2, 1, 1, 2, 3 }); //0-2-1の頂点で1三角形。 1-2-3の頂点で1三角形。

        mesh.SetVertices(vertexList);   //meshに頂点群をセット
        mesh.SetNormals(normalList);
        mesh.SetUVs(0, uvList);         //meshにテクスチャのuv座標をセット（今回は割愛)
        mesh.SetIndices(indexList.ToArray(), MeshTopology.Triangles, 0);//メッシュにどの頂点の順番で面を作るかセット

        filter.mesh = mesh;
    }
}
