using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public class BuildingTool : EditorWindow
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
    string prefabName;


    [MenuItem("Tools/BuildingTool")]
    private static void ShowWindow()
    {
        GetWindow<BuildingTool>("BuildingTool");
    }

    private void OnEnable()
    {
        searchbar = "";
        prefabName = "";
        sceneViewRect.size = new Vector2(200, 200);
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
                pos=hit.point;
                Snap(hit);
                pos.x = RoundFloat(pos.x);
                pos.z= RoundFloat(pos.z);
                pos.y =RoundFloat( hit.point.y+instance.GetComponent<Collider>().bounds.size.y / 2);

                instance.transform.position = new Vector3(pos.x, pos.y, pos.z);
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
                    //Debug.Log("Posizione cursore nel mondo (senza hit): " + worldPosition);
                }
            }
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow)
        {
            instance.transform.Rotate(0,90,0);
        }

        if(e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
        {
            instance.transform.Rotate(90,0 , 0);
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return&&canIstantiate)
        {
            isSpawned = true;
            instance.transform.position = pos;
            instance.gameObject.layer=layerMask;
            instance.GetComponent<MeshRenderer>().material = prefabMaterial;
            instance.tag = "Module";
        }

        sceneViewRect = GUI.Window(0, sceneViewRect, DrawSceneViewRect, "BuildingTool");
        if (IsMouseOver() && Event.current.button == 0 && e.type == EventType.MouseDrag)
        {
            sceneViewRect.position += Event.current.delta;
            view.Repaint();
            e.Use();
        }
    }

    private void Snap(RaycastHit hit)
    {
        GameObject[] module = GameObject.FindGameObjectsWithTag("Module");
        for (int i = 0; i < module.Length; i++)
        {
            if (Vector3.Distance(module[i].transform.position, hit.point) <1.1f)
            {
                if (hit.point.x < 0)
                {
                    pos.x = module[i].transform.position.x - module[i].transform.GetComponent<Collider>().bounds.size.x;
                }
                else
                {
                    pos.x = module[i].transform.position.x + module[i].transform.GetComponent<Collider>().bounds.size.x;
                }
                if (hit.point.z < 0)
                {
                    pos.z = module[i].transform.position.z - module[i].transform.GetComponent<Collider>().bounds.size.z;
                }
                else
                {
                    pos.z = module[i].transform.position.z + module[i].transform.GetComponent<Collider>().bounds.size.z;

                    return;
                }
            }
        }
    }

    private float RoundFloat(float x)
    {
        float parteDecimale = Mathf.Abs(x) - (float)Math.Floor(Mathf.Abs(x));
        int primaCifra = (int)(parteDecimale * 100) % 100 + 1;
        bool negative = false;
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
        scrollviewpos = GUILayout.BeginScrollView(scrollviewpos, GUILayout.Height(100));

        for (int i = 0; i < list.Count; i++)
        {
            if (!string.IsNullOrEmpty(searchbar.ToLower()) && !list[i].name.ToLower().Contains(searchbar.ToLower()))
                continue;
            GUILayout.BeginHorizontal();
            Texture2D assetpreview = AssetPreview.GetAssetPreview(list[i]);
            GUILayout.Label(assetpreview, GUILayout.Width(50), GUILayout.Height(30));
            if (GUILayout.Button(list[i].name, GUILayout.Height(30)))
            {
                if (isSpawned)
                {
                    isSpawned = false;
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(list[i]);
                    instance.transform.position = Vector3.zero;
                    prefabMaterial = instance.GetComponent<MeshRenderer>().sharedMaterial;
                }else if (!isSpawned)
                {
                    DestroyImmediate(instance);
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(list[i]);
                    prefabMaterial = instance.GetComponent<MeshRenderer>().sharedMaterial;
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        prefabName = EditorGUILayout.TextField("PrefabName: ", prefabName);
        if (GUILayout.Button("SavePrefab", GUILayout.Width(190), GUILayout.Height(30)))
        {
            SavePrefab("Module");
        }
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

    private void SavePrefab(string tag)
    {
        GameObject[] obj = GameObject.FindGameObjectsWithTag(tag);
        if (obj.Length == 0)
        {
            return;
        }

        GameObject parent = new GameObject(prefabName);

        foreach (GameObject gameObject in obj)
        {
            gameObject.transform.SetParent(parent.transform);
        }

        string prefabFolder = "Assets/Save";
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }
        if (prefabName=="")
        {
            prefabName = "NewPrefab";
        }
        string prefabPath = Path.Combine(prefabFolder, prefabName + ".prefab");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(parent, prefabPath);
        DestroyImmediate(parent);
        prefabName = "";
    }

}