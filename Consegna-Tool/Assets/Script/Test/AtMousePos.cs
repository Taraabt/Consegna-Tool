using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    public GameObject prefabToFollow; // Prefab da far seguire al cursore
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main; // Ottieni la camera principale
    }

    private void Update()
    {
        if (prefabToFollow != null)
        {
            Vector3 mousePosition = Input.mousePosition; // Ottieni la posizione del mouse in pixel
            mousePosition.z = 10f; // Imposta la profondità (distanza dalla camera)

            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition); // Converti in coordinate del mondo
            worldPosition.z = 0f; // Imposta la coordinata Z a 0 per un piano 2D

            prefabToFollow.transform.position = worldPosition; // Aggiorna la posizione del prefab
        }
        else
        {
            Debug.LogWarning("Prefab non assegnato! Assicurati di assegnare un prefab nel pannello Inspector.");
        }
    }
}