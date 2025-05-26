using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class InpaintingManager : MonoBehaviour
{
    [Header("Références")]
    public Camera mainCamera;

    [Header("Interface")]
    public Button inpaintButton;
    
    [Header("Highlight")]
    public Material highlightMaterial;
    public float highlightOpacity = 0.3f;

    [Header("Paramètres API")]
    public string apiUrl = "http://84.234.18.154:9000";
    public int apiTimeout = 30;
    
    // États du système
    private bool isSelectionMode = false;
    private bool isProcessing = false;
    
    // Highlight de la face complète
    private GameObject highlightFace = null;
    private string currentHighlightedFace = "";
    
    // Variables pour le remplacement
    private string selectedFaceName = "";
    private string selectedPanoId = "";

    void Start()
    {
        Debug.Log("[InpaintingManager] Initialisation...");

        if (mainCamera == null) mainCamera = Camera.main;
        if (inpaintButton != null) inpaintButton.onClick.AddListener(OnInpaintButtonClicked);

        if (highlightMaterial == null)
        {
            highlightMaterial = CreateHighlightMaterial();
        }

        Debug.Log("[InpaintingManager] Initialisation terminée");
    }
    
    void Update()
    {
        if (isSelectionMode && !isProcessing)
        {
            HandleCursorFaceDetection();
        }
    }
    
    private void OnInpaintButtonClicked()
    {
        if (!isSelectionMode)
        {
            EnterSelectionMode();
        }
        else
        {
            ExitSelectionMode();
        }
    }
    
    private void EnterSelectionMode()
    {
        isSelectionMode = true;
        Debug.Log("[InpaintingManager] Mode sélection activé - clic droit pour sélectionner une face");
    }

    private void ExitSelectionMode()
    {
        isSelectionMode = false;
        Debug.Log("[InpaintingManager] Mode sélection désactivé");
        ClearHighlight();
    }
    
    private void HandleCursorFaceDetection()
    {
        Vector3 cursorDirection = mainCamera.ScreenPointToRay(Input.mousePosition).direction;
        string faceName = GetFaceNameFromDirection(cursorDirection);
        string panoId = GetCurrentPanoId();
        
        if (faceName != currentHighlightedFace)
        {
            UpdateFaceHighlight(faceName);
            currentHighlightedFace = faceName;
        }
        
        Debug.Log($"[InpaintingManager] Face: {faceName} | PanoID: {panoId}");
        
        // Clic droit pour sélectionner
        if (Input.GetMouseButtonDown(1))
        {
            OnFaceSelected(faceName, panoId);
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitSelectionMode();
        }
    }
    
    private string GetFaceNameFromDirection(Vector3 direction)
    {
        CubemapFace face = GetCubemapFaceFromDirection(direction);
        return face.ToString();
    }
    
    private CubemapFace GetCubemapFaceFromDirection(Vector3 direction)
    {
        Vector3 absDirection = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
        
        if (absDirection.x > absDirection.y && absDirection.x > absDirection.z)
        {
            return direction.x > 0 ? CubemapFace.PositiveX : CubemapFace.NegativeX;
        }
        else if (absDirection.y > absDirection.z)
        {
            return direction.y > 0 ? CubemapFace.PositiveY : CubemapFace.NegativeY;
        }
        else
        {
            return direction.z > 0 ? CubemapFace.PositiveZ : CubemapFace.NegativeZ;
        }
    }
    
    private string GetCurrentPanoId()
    {    
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            GameObject activeSphere = cameraController.GetActiveSphere();
            if (activeSphere != null && DataHandler.Instance != null)
            {
                foreach (var pov in DataHandler.Instance.povDict.Values)
                {
                    if (pov.povObj == activeSphere)
                    {
                        return pov.panoid;
                    }
                }
            }
        }
        return "PanoID_Unknown";
    }
    
    private Material CreateHighlightMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        
        mat.color = new Color(1f, 0f, 0f, highlightOpacity);
        mat.renderQueue = 3001;
        
        return mat;
    }
    
    private void UpdateFaceHighlight(string faceName)
    {
        GameObject activeSphere = GetActiveSphere();
        if (activeSphere == null) return;
        
        // Récupérer le cubemap actuel
        Cubemap cubemap = GetCubemapFromActiveSphere();
        if (cubemap == null) return;
        
        // Créer l'objet de highlight s'il n'existe pas
        if (highlightFace == null)
        {
            CreateHighlightObject();
        }
        
        // Mettre à jour le mesh avec la bonne taille
        Vector3 sphereCenter = activeSphere.transform.position;
        float radius = activeSphere.transform.localScale.x * 0.5f;
        UpdateHighlightMesh(faceName, sphereCenter, radius);
    }
    
    private void PositionHighlightQuad(string faceName, Transform sphereTransform)
    {
        if (highlightFace == null) return;

        Vector3 position = sphereTransform.position;
        Vector3 scale = sphereTransform.localScale;
        float radius = scale.x * 0.5f;

        Vector3 direction = GetFaceDirection(faceName);
        highlightFace.transform.position = position + direction * radius;
        highlightFace.transform.rotation = Quaternion.LookRotation(direction);
        highlightFace.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f); // Doublé la taille
    }
    
    private Vector3 GetFaceDirection(string faceName)
    {
        switch (faceName)
        {
            case "PositiveX": return Vector3.right;
            case "NegativeX": return Vector3.left;
            case "PositiveY": return Vector3.up;
            case "NegativeY": return Vector3.down;
            case "PositiveZ": return Vector3.forward;
            case "NegativeZ": return Vector3.back;
            default: return Vector3.forward;
        }
    }
    
    private void CreateHighlightObject()
    {
        highlightFace = new GameObject("FaceHighlight");
        highlightFace.AddComponent<MeshFilter>();
        MeshRenderer renderer = highlightFace.AddComponent<MeshRenderer>();
        renderer.material = highlightMaterial;
        
        Debug.Log("[DEBUG] Objet FaceHighlight créé UNE SEULE FOIS");
    }
    
    private void UpdateHighlightMesh(string faceName, Vector3 sphereCenter, float radius)
    {
        if (highlightFace == null) return;
        
        Vector3[] corners = GetFaceCorners(faceName);
        
        int subdivisionsPerSide = 20;
        int totalVertices = (subdivisionsPerSide + 1) * (subdivisionsPerSide + 1);
        Vector3[] vertices = new Vector3[totalVertices];
        
        for (int y = 0; y <= subdivisionsPerSide; y++)
        {
            for (int x = 0; x <= subdivisionsPerSide; x++)
            {
                float u = (float)x / subdivisionsPerSide;
                float v = (float)y / subdivisionsPerSide;
                
                Vector3 point = BilinearInterpolation(corners[0], corners[1], corners[2], corners[3], u, v);
                vertices[y * (subdivisionsPerSide + 1) + x] = sphereCenter + point.normalized * radius;
            }
        }
        
        int[] triangles = new int[subdivisionsPerSide * subdivisionsPerSide * 6];
        int triangleIndex = 0;
        
        for (int y = 0; y < subdivisionsPerSide; y++)
        {
            for (int x = 0; x < subdivisionsPerSide; x++)
            {
                int bottomLeft = y * (subdivisionsPerSide + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + (subdivisionsPerSide + 1);
                int topRight = topLeft + 1;
                
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;
                
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }
        
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;
        
        highlightFace.GetComponent<MeshFilter>().mesh = mesh;
        highlightFace.SetActive(true);
        
        Debug.Log($"[DEBUG] Mesh haute résolution créé pour face {faceName} avec {vertices.Length} vertices");
    }
    
    private Vector3 BilinearInterpolation(Vector3 corner0, Vector3 corner1, Vector3 corner2, Vector3 corner3, float u, float v)
    {
        Vector3 bottomInterpolation = Vector3.Lerp(corner0, corner1, u);
        Vector3 topInterpolation = Vector3.Lerp(corner3, corner2, u);
        return Vector3.Lerp(bottomInterpolation, topInterpolation, v);
    }
    
    private Vector3[] GetFaceCorners(string faceName)
    {
        switch (faceName)
        {
            case "PositiveZ": // Front
                return new Vector3[] {
                    new Vector3(-1, -1, 1), new Vector3(1, -1, 1),  
                    new Vector3(1, 1, 1), new Vector3(-1, 1, 1)
                };
                
            case "NegativeZ": // Back
                return new Vector3[] {
                    new Vector3(1, -1, -1), new Vector3(-1, -1, -1), 
                    new Vector3(-1, 1, -1), new Vector3(1, 1, -1)
                };
                
            case "PositiveX": // Right
                return new Vector3[] {
                    new Vector3(1, -1, 1), new Vector3(1, -1, -1), 
                    new Vector3(1, 1, -1), new Vector3(1, 1, 1)
                };
                
            case "NegativeX": // Left
                return new Vector3[] {
                    new Vector3(-1, -1, -1), new Vector3(-1, -1, 1), 
                    new Vector3(-1, 1, 1), new Vector3(-1, 1, -1)
                };
                
            case "PositiveY": // Up
                return new Vector3[] {
                    new Vector3(-1, 1, -1), new Vector3(-1, 1, 1), 
                    new Vector3(1, 1, 1), new Vector3(1, 1, -1)
                };
                
            case "NegativeY": // Down
                return new Vector3[] {
                    new Vector3(-1, -1, 1), new Vector3(-1, -1, -1), 
                    new Vector3(1, -1, -1), new Vector3(1, -1, 1)
                };
                
            default:
                return new Vector3[] {
                    new Vector3(-1, -1, 1), new Vector3(1, -1, 1),
                    new Vector3(1, 1, 1), new Vector3(-1, 1, 1)
                };
        }
    }
    
    private void ClearHighlight()
    {
        if (highlightFace != null)
        {
            highlightFace.SetActive(false);
        }
        currentHighlightedFace = "";
    }
    
    private GameObject GetActiveSphere()
    {
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            return cameraController.GetActiveSphere();
        }
        return null;
    }
    
    private void OnFaceSelected(string faceName, string panoId)
    {
        if (isProcessing) return;
        
        Debug.Log($"[InpaintingManager] Face sélectionnée : {faceName} sur PanoID : {panoId}");
        
        // Sauvegarder la sélection pour le remplacement
        selectedFaceName = faceName;
        selectedPanoId = panoId;
        
        StartCoroutine(InpaintWorkflow(faceName, panoId));
    }
    
    private System.Collections.IEnumerator InpaintWorkflow(string faceName, string panoId)
    {
        isProcessing = true;
        Debug.Log("[InpaintingManager] Début du workflow d'inpainting...");
        
        // 1. Envoyer la face à IOPaint
        yield return StartCoroutine(SendFaceToIOPaint(faceName, panoId));
        
        // 2. Ouvrir IOPaint dans le navigateur
        Application.OpenURL(apiUrl);
        
        isProcessing = false;
        Debug.Log("[InpaintingManager] Workflow d'inpainting terminé");
    }
    
    private System.Collections.IEnumerator SendFaceToIOPaint(string faceName, string panoId)
    {
        Debug.Log($"[InpaintingManager] Envoi de la face {faceName} à IOPaint...");
        
        // Construire l'URL de l'image
        string faceFileName = GetFaceFileName(faceName);
        string baseUrl = GetCubemapsFolderUrl();
        Debug.Log($"[InpaintingManager] URL de base des cubemaps: {baseUrl}");
        
        string imageUrl = baseUrl + "/" + panoId + "/" + panoId + "_" + faceFileName + ".png";
        Debug.Log($"[InpaintingManager] URL complète de l'image: {imageUrl}");
        
        // Envoyer l'URL à IOPaint
        string jsonPayload = "{\"image_url\":\"" + imageUrl + "\"}";
        Debug.Log($"[InpaintingManager] Payload JSON: {jsonPayload}");
        
        string fullApiUrl = apiUrl + "/api/v1/unity_image_url";
        Debug.Log($"[InpaintingManager] URL de l'API: {fullApiUrl}");
        
        using (UnityWebRequest request = new UnityWebRequest(fullApiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = apiTimeout;
            
            Debug.Log("[InpaintingManager] Envoi de la requête...");
            yield return request.SendWebRequest();
            
            if (!request.isNetworkError && !request.isHttpError)
            {
                Debug.Log("[InpaintingManager] URL envoyée avec succès à IOPaint");
                Debug.Log($"[InpaintingManager] Réponse: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"[InpaintingManager] Erreur envoi IOPaint: {request.error}");
                Debug.LogError($"[InpaintingManager] Code d'erreur: {request.responseCode}");
                Debug.LogError($"[InpaintingManager] Réponse: {request.downloadHandler.text}");
            }
        }
    }
    
    private string GetFaceFileName(string faceName)
    {
        switch (faceName)
        {
            case "PositiveX": return "r"; // right
            case "NegativeX": return "l"; // left
            case "PositiveY": return "u"; // up
            case "NegativeY": return "d"; // down
            case "PositiveZ": return "f"; // front
            case "NegativeZ": return "b"; // back
            default: return "f";
        }
    }
    
    private Cubemap GetCubemapFromActiveSphere()
    {
        GameObject activeSphere = GetActiveSphere();
        if (activeSphere == null) return null;
        
        Renderer renderer = activeSphere.GetComponent<Renderer>();
        if (renderer == null) return null;
        
        Material material = renderer.material;
        if (material == null) return null;
        
        return material.GetTexture("_Tex") as Cubemap;
    }
    
    private string GetCubemapsFolderUrl()
    {
        ConfigurationSettings config = FindObjectOfType<POVManager>()?.configSettings;
        if (config != null)
        {
            return config.CubemapsUrl;
        }
        return "http://localhost:8000/images/cubemaps";
    }
    
    void OnDestroy()
    {
        if (highlightFace != null)
        {
            DestroyImmediate(highlightFace);
        }
    }
} 