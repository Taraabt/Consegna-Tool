using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class PropsScatterer : EditorWindow
{
    [MenuItem("Tools/PropScatterer")]
    public static void OpenScatterer() => GetWindow<PropsScatterer>();

    public float radius = 2f;
    public int spawnCount = 8;
    public GameObject spawnPrefab = null;
    public Material previewMaterial = null;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;
    SerializedProperty propPreviewMaterial;

    RandomData[] randomData;
    //Vector2[] randomPoints;
    //List<RaycastHit> hitPts = new List<RaycastHit>();
    List<Pose> hitPoses = new List<Pose>();

    const float TAU = 6.28318530718f; // Costante per 2pigreco

    GameObject[] prefabs;

    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        propSpawnPrefab = so.FindProperty("spawnPrefab");
        propPreviewMaterial = so.FindProperty("previewMaterial");

        GenerateRandomPoints();

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        Rect rect = new Rect(8, 8, 200, 20);

        foreach (GameObject prefab in prefabs)
        {
            Texture icon = AssetPreview.GetAssetPreview(prefab);
            if (GUI.Toggle(rect, spawnPrefab == prefab, new GUIContent(prefab.name, icon)))
                spawnPrefab = prefab;
            rect.y += rect.height + 2;
        }

        Handles.EndGUI();


        Transform camTF = sceneView.camera.transform;
        if (Event.current.type == EventType.MouseMove) sceneView.Repaint();

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        //Ray ray = new Ray(camTF.position, camTF.forward); NON SERVE PIU

        if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
        {
            TrySpawnObjects(hitPoses);
        }


        ReadScrollWheel();

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;



            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hit.normal, camTF.up).normalized; // la tangente e il prodotto vettoriale tra la normale e l'up della camera
            Vector3 hitBiTangent = Vector3.Cross(hitNormal, hitTangent);
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(5, hit.point, hit.point + hitTangent);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(5, hit.point, hit.point + hitBiTangent);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(5, hit.point, hit.point + hitNormal);

            Handles.color = Color.white;

            Ray GetTangentRay(Vector2 tangentSpacePos)
            {
                Vector3 rayOrigin = hit.point + (hitTangent * tangentSpacePos.x + hitBiTangent * tangentSpacePos.y) * radius;
                rayOrigin += hitNormal * 2f;
                Vector3 rayDirection = -hit.normal;

                return new Ray(rayOrigin, rayDirection);
            }


            const int circleDetail = 64;
            Vector3[] points = new Vector3[circleDetail];

            for (int i = 0; i < circleDetail; i++)
            {
                float t = i / (float)circleDetail - 1;
                float angRad = t * TAU;

                Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
                Ray r = GetTangentRay(dir);

                if (Physics.Raycast(r, out RaycastHit cHit))
                {
                    points[i] = cHit.point + cHit.normal * 0.02f;
                }
                else
                {
                    points[i] = r.origin;
                }
            }

            Handles.DrawAAPolyLine(points);
            Handles.DrawAAPolyLine(points[circleDetail - 1], points[0]);
            //Handles.DrawWireDisc(hit.point, hit.normal, radius);


            //hitPts = new List<RaycastHit>();
            hitPoses = new List<Pose>();

            foreach (RandomData rndDataPt in randomData)
            {
                Ray ptRay = GetTangentRay(rndDataPt.pointOnDisc);

                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    Quaternion randomRot = Quaternion.Euler(0f, 0f, rndDataPt.randAngle);
                    Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randomRot * Quaternion.Euler(90f, 0, 0));
                    Pose pose = new Pose(ptHit.point, rot);


                    DrawSphere(ptHit.point);

                    //Mesh mesh = spawnPrefab.GetComponent<MeshFilter>().sharedMesh;
                    //previewMaterial.SetPass(0);
                    //Graphics.DrawMeshNow(mesh, pose.position, pose.rotation);

                    Matrix4x4 poseToWorldMtx = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);

                    MeshFilter[] filters = spawnPrefab.GetComponentsInChildren<MeshFilter>();

                    foreach (MeshFilter filter in filters)
                    {
                        Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
                        Matrix4x4 childToWorldMtx = poseToWorldMtx * childToPose;

                        Mesh mesh = filter.sharedMesh;
                        Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
                        mat.SetPass(0);
                        Graphics.DrawMeshNow(mesh, childToWorldMtx);
                    }

                    Handles.DrawAAPolyLine(ptHit.point, ptHit.point + ptHit.normal);
                    hitPoses.Add(pose);
                }
            }
        }

    }

    private void DrawSphere(Vector3 p)
    {
        Handles.SphereHandleCap(-1, p, Quaternion.identity, 0.1f, EventType.Repaint);
    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        //propRadius.floatValue = propRadius.floatValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnCount);
        //propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnPrefab);
        EditorGUILayout.PropertyField(propPreviewMaterial);

        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);

            Repaint();
        }
    }

    void GenerateRandomPoints()
    {
        randomData = new RandomData[spawnCount];

        for (int i = 0; i < spawnCount; ++i)
        {
            randomData[i].SetRandomValues();
        }
    }

    void ReadScrollWheel()
    {
        Event e = Event.current;

        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;

        holdingAlt = Event.current.modifiers.HasFlag(EventModifiers.Alt);

        holdingAlt = e.alt;

        if (e.type == EventType.ScrollWheel && holdingAlt)
        {
            so.Update();

            float radiusValue = propRadius.floatValue;
            radiusValue = Mathf.Max(1f, radiusValue + e.delta.y * -0.1f);
            propRadius.floatValue = radiusValue;

            so.ApplyModifiedProperties();
            e.Use();
            Repaint();
        }

    }

    void TrySpawnObjects(List<Pose> poses)
    {
        if (spawnPrefab == null) return;

        foreach (Pose pose in poses)
        {
            GameObject spawnedObj = PrefabUtility.InstantiatePrefab(spawnPrefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(spawnedObj, "Spawn Object");

            spawnedObj.transform.position = pose.position;


            spawnedObj.transform.rotation = pose.rotation;
        }

        GenerateRandomPoints();

    }

}


public struct RandomData
{
    public Vector2 pointOnDisc;
    public float randAngle;

    public void SetRandomValues()
    {
        pointOnDisc = Random.insideUnitCircle;
        randAngle = Random.value * 360;
    }
}
