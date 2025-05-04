using UnityEngine;
using UnityEditor;

public class LevelDesignTool : EditorWindow
{
    public GameObject[] modules; // Array di prefab dei moduli
    private GameObject previewInstance; // Istanza del modulo in anteprima
    private GameObject selectedModule; // Modulo attualmente selezionato
    private Material previewMaterial; // Materiale per l'anteprima

    [MenuItem("Tools/Level Design Tool")]
    public static void ShowWindow()
    {
        GetWindow<LevelDesignTool>("Level Design Tool");
    }

    private void OnEnable()
    {
        // Carica i prefab dei moduli
        modules = Resources.LoadAll<GameObject>("Modules");

        // Controlla se i moduli sono stati caricati
        if (modules.Length == 0)
        {
            Debug.LogError("Nessun modulo trovato nella cartella Resources/Modules. Assicurati di avere prefab validi.");
        }

        // Crea un materiale per l'anteprima
        previewMaterial = new Material(Shader.Find("Standard"));
        previewMaterial.color = Color.yellow; // Colore iniziale per l'anteprima
    }

    private void OnGUI()
    {
        GUILayout.Label("Seleziona un Modulo", EditorStyles.boldLabel);

        // Disegna i pulsanti per la selezione dei moduli
        if (modules.Length == 0)
        {
            GUILayout.Label("Nessun modulo disponibile.");
        }
        else
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (GUILayout.Button("Seleziona Modulo " + (i + 1)))
                {
                    SelectModule(i);
                }
            }
        }

        // Istruzioni per l'utente
        GUILayout.Label("Usa il mouse per posizionare il modulo.");
        GUILayout.Label("Clicca per spawnare il modulo.");
    }

    private void Update()
    {
        // Aggiorna la posizione dell'anteprima
        UpdatePreview();
    }

    private void SelectModule(int index)
    {
        if (index >= 0 && index < modules.Length)
        {
            selectedModule = modules[index];

            // Se c'è già un'istanza di anteprima, distruggila
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
            }

            // Crea una nuova istanza di anteprima
            previewInstance = Instantiate(selectedModule);
            // Applica il materiale di anteprima
            Renderer renderer = previewInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = previewMaterial; // Usa il materiale di anteprima
            }
            else
            {
                Debug.LogError("Il prefab selezionato non ha un componente Renderer.");
            }
        }
    }

    private void UpdatePreview()
    {
        if (previewInstance != null)
        {
            // Posiziona l'anteprima in corrispondenza del cursore del mouse
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 position = hit.point;
                position.y = 0; // Assicurati che il modulo sia posizionato a terra
                previewInstance.transform.position = position;

                // Cambia il colore in base alla validità della posizione
                if (IsValidPosition(position))
                {
                    previewInstance.GetComponent<Renderer>().material.color = Color.green; // Posizione valida
                }
                else
                {
                    previewInstance.GetComponent<Renderer>().material.color = Color.red; // Posizione non valida
                }

                // Se l'utente clicca con il tasto sinistro del mouse, spawn il modulo
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    SpawnModule();
                    Event.current.Use(); // Consuma l'evento per evitare che venga gestito da Unity
                }
            }
            else
            {
                Debug.LogWarning("Raycast non ha colpito nulla.");
            }
        }
        else
        {
            Debug.LogWarning("L'istanza di anteprima è null.");
        }
    }

    private void SpawnModule()
    {
        if (selectedModule != null && previewInstance != null && IsValidPosition(previewInstance.transform.position))
        {
            // Instanzia il modulo selezionato nella posizione dell'anteprima
            Instantiate(selectedModule, previewInstance.transform.position, Quaternion.identity);
            DestroyImmediate(previewInstance); // Distruggi l'anteprima dopo aver spawnato il modulo
        }
    }

    private bool IsValidPosition(Vector3 position)
    {
        // Implementa la logica per verificare se la posizione è valida
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        return colliders.Length == 0; // Posizione valida se non ci sono collisioni
    }
}