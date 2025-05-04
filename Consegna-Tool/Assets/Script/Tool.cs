using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
public class PrefabSpawner : EditorWindow
{

    Vector2 scrollviewpos; 
    private string searchbar; 
    private Rect sceneViewRect; 
    List<GameObject> list = new List<GameObject>();
    GameObject instance;
    Vector3 pos;
    bool isSpawned=true;
    bool canIstantiate = true;
    int layerMask=0;
    Material prefabMaterial;


    [MenuItem("Tools/Prefab Spawner Tool")]
    private static void ShowWindow()
    {
        GetWindow<PrefabSpawner>("Prefab Spawner");
    }

    private void OnEnable()
    {
        searchbar = "";
        sceneViewRect.size = new Vector2(300, 200);
        SceneView.duringSceneGui += DrawOnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawOnSceneGUI;
    }

    private void DrawOnSceneGUI(SceneView view)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseMove)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000, 1 << 0)&&!isSpawned)
            {
                canIstantiate = true;
                Debug.Log("Cursor Position in World: " + hit.point);
                Snap(hit);
                pos.x = RoundFloat(hit.point.x);
                pos.z= RoundFloat(hit.point.z);
                instance.transform.position = new Vector3(pos.x, hit.point.y + instance.transform.localScale.y / 2, pos.z);
                pos = instance.transform.position;
                instance.GetComponent<MeshRenderer>().material.color = Color.green;
            }
            else if(!isSpawned)
            {
                canIstantiate = false;
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float enter;
                if (groundPlane.Raycast(ray, out enter))
                {
                    instance.GetComponent<MeshRenderer>().material.color = Color.red;
                    Vector3 worldPosition = ray.GetPoint(enter);
                    instance.transform.position = new Vector3(worldPosition.x, worldPosition.y + instance.transform.localScale.y / 2, worldPosition.z);
                    Debug.Log("Posizione cursore nel mondo (senza hit): " + worldPosition);
                }
            }
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return&&canIstantiate)
        {
            isSpawned = true;
            instance.transform.position = pos;
            instance.gameObject.layer=layerMask;
            instance.GetComponent<MeshRenderer>().material = prefabMaterial;
        }

        sceneViewRect = GUI.Window(0, sceneViewRect, DrawSceneViewRect, "Tool");
        if (IsMouseOver() && Event.current.button == 0 && e.type == EventType.MouseDrag)
        {
            sceneViewRect.position += Event.current.delta;
            view.Repaint();
            e.Use();
        }
    }


    private void Snap(RaycastHit hit)
    {

    }

    private float RoundFloat(float x)
    {
        float parteDecimale = Mathf.Abs(x) - (float)Math.Floor(Mathf.Abs(x));
        int primaCifra = (int)(parteDecimale * 100) % 100 + 1;
        bool negative = false;
        Debug.Log(primaCifra);
        if (x <= 0)
        {
            negative = true;
        }
        else
        {
            negative = false;
        }

        if (primaCifra < 25 && primaCifra >= 0&&!negative)
        {
            x = Mathf.Floor(x);
        }
        else if (primaCifra >= 25 && primaCifra < 50&&!negative)
        {
            x = Mathf.Floor(x) + 0.5f;
        }
        else if (primaCifra >= 50 && primaCifra < 75 && !negative)
        {
            x = Mathf.Ceil(x)-0.5f;
        }
        else if(primaCifra >=75&&!negative)
        {
            x = Mathf.Ceil(x);
        }

        if (primaCifra < 25 && primaCifra >= 0&&negative)
        {
            x=Mathf.Ceil(x);
        }
        else if (primaCifra >= 25 && primaCifra < 50&&negative)
        {
            x = Mathf.Ceil(x) - 0.5f;
        }
        else if (primaCifra >= 50 && primaCifra < 75 && negative)
        {
            x = Mathf.Floor(x) + 0.5f;
        }
        else if (primaCifra >= 75&&negative)
        {
            x = Mathf.Floor(x);
        }
        return x;

    }

    private void DrawSceneViewRect(int index)
    {
        Event e = Event.current;
        if (e.type == EventType.ScrollWheel)
        {
            scrollviewpos.y += e.delta.y *5;
            e.Use();
        }
        list = GetPrefabs();

        searchbar = EditorGUILayout.TextField("Search: ", searchbar);
        scrollviewpos = GUILayout.BeginScrollView(scrollviewpos, GUILayout.Height(150));

        for (int i = 0; i < list.Count; i++)
        {
            if (!string.IsNullOrEmpty(searchbar.ToLower()) && !list[i].name.ToLower().Contains(searchbar.ToLower()))
                continue;
            GUILayout.BeginHorizontal();
            Texture2D assetpreview = AssetPreview.GetAssetPreview(list[i]);
            GUILayout.Label(assetpreview, GUILayout.Width(50), GUILayout.Height(50));
            if (GUILayout.Button(list[i].name, GUILayout.Height(50))&&isSpawned)
            {
                isSpawned = false;
                instance = (GameObject)PrefabUtility.InstantiatePrefab(list[i]);
                instance.transform.position = Vector3.zero;
                prefabMaterial = instance.GetComponent<MeshRenderer>().sharedMaterial;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    private bool IsMouseOver()
    {
        Vector2 relativePointerPos = Event.current.mousePosition - sceneViewRect.position;
        if (relativePointerPos.x < sceneViewRect.width && relativePointerPos.y < sceneViewRect.height && relativePointerPos.x > 0 && relativePointerPos.y > 0)
        {
            return true;
        }
        return false;
    }

    private List<GameObject> GetPrefabs()
    {
        List<GameObject> gameobject = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            //Debug.Log(path + " unity " + guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab)
            {
                gameobject.Add(prefab);
            }
        }
        return gameobject;
    }

}