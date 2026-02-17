using UnityEngine;
using ModTool.Interface;

public class ZoneTeleporter : ModBehaviour
{
    [Header("Start Zone Bounds")]
    [SerializeField] private float startZoneMinX = 0f;
    [SerializeField] private float startZoneMaxX = 10f;
    [SerializeField] private float startZoneMinZ = 0f;
    [SerializeField] private float startZoneMaxZ = 10f;

    [Header("End Zone Bounds")]
    [SerializeField] private float endZoneMinX = 50f;
    [SerializeField] private float endZoneMaxX = 60f;
    [SerializeField] private float endZoneMinZ = 50f;
    [SerializeField] private float endZoneMaxZ = 60f;

    [Header("Gizmo Settings")]
    [SerializeField] private Color startZoneColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color endZoneColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private float gizmoHeight = 5f;

    [Header("Teleport Settings")]
    [SerializeField] private float teleportHeightOffset = 1f; // 1 meter above terrain

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showPlayerPositionGizmo = true;

    private GameObject playerHuman;
    private bool hasBeenTeleported = false;
    private float searchTimer = 0f;
    private const float SEARCH_INTERVAL = 0.5f;

    private Terrain terrain;
    private float startZoneTerrainHeight = 0f;
    private float endZoneTerrainHeight = 0f;

    private void Start()
    {
        // Find terrain in scene
        terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            terrain = FindObjectOfType<Terrain>();
        }

        if (terrain != null)
        {
            UpdateTerrainHeights();
            if (enableDebugLogs)
            {
                Debug.Log(string.Format("Terrain found. Start Zone Height: {0:F2}, End Zone Height: {1:F2}",
                    startZoneTerrainHeight, endZoneTerrainHeight));
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("No terrain found in scene! Using raycast method for height detection.");
            }
        }

        FindPlayerHuman();
    }

    private void Update()
    {
        // Continuously try to find player if not found
        if (playerHuman == null)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= SEARCH_INTERVAL)
            {
                searchTimer = 0f;
                FindPlayerHuman();
            }
            return;
        }

        Vector3 playerPos = playerHuman.transform.position;

        // Debug player position every frame
        if (enableDebugLogs && Time.frameCount % 60 == 0) // Log once per second at 60fps
        {
            Debug.Log(string.Format("Player_Human Position: X={0:F2}, Y={1:F2}, Z={2:F2}",
                playerPos.x, playerPos.y, playerPos.z));
        }

        // Check if player is within start zone bounds
        bool inStartZone = IsWithinBounds(playerPos, startZoneMinX, startZoneMaxX, startZoneMinZ, startZoneMaxZ);

        if (enableDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log(string.Format("In Start Zone: {0} | X in range [{1}, {2}]: {3} | Z in range [{4}, {5}]: {6}",
                inStartZone,
                startZoneMinX, startZoneMaxX, (playerPos.x >= startZoneMinX && playerPos.x <= startZoneMaxX),
                startZoneMinZ, startZoneMaxZ, (playerPos.z >= startZoneMinZ && playerPos.z <= startZoneMaxZ)));
        }

        if (inStartZone)
        {
            if (!hasBeenTeleported)
            {
                TeleportToEndZone();
                hasBeenTeleported = true;
            }
        }
        else
        {
            // Reset teleport flag when player leaves start zone
            if (hasBeenTeleported)
            {
                hasBeenTeleported = false;
                if (enableDebugLogs)
                {
                    Debug.Log("Teleport flag reset - player left start zone");
                }
            }
        }
    }

    private void UpdateTerrainHeights()
    {
        Vector3 startZoneCenter = new Vector3(
            (startZoneMinX + startZoneMaxX) / 2f,
            0f,
            (startZoneMinZ + startZoneMaxZ) / 2f
        );

        Vector3 endZoneCenter = new Vector3(
            (endZoneMinX + endZoneMaxX) / 2f,
            0f,
            (endZoneMinZ + endZoneMaxZ) / 2f
        );

        startZoneTerrainHeight = GetTerrainHeight(startZoneCenter);
        endZoneTerrainHeight = GetTerrainHeight(endZoneCenter);
    }

    private float GetTerrainHeight(Vector3 worldPosition)
    {
        if (terrain != null)
        {
            // Use Unity's terrain sampling
            return terrain.SampleHeight(worldPosition);
        }
        else
        {
            // Fallback: Use raycast from high above
            RaycastHit hit;
            Vector3 rayStart = new Vector3(worldPosition.x, 1000f, worldPosition.z);

            if (Physics.Raycast(rayStart, Vector3.down, out hit, 2000f))
            {
                return hit.point.y;
            }

            // Default to 0 if nothing found
            return 0f;
        }
    }

    private void FindPlayerHuman()
    {
        playerHuman = GameObject.Find("Player_Human");

        if (playerHuman != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("SUCCESS: Player_Human found at position: " + playerHuman.transform.position);
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("Player_Human object not found! Searching for similar names...");

                // Search for objects with "Player" in the name
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                bool foundSimilar = false;
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Player") || obj.name.Contains("player"))
                    {
                        Debug.Log("Found object with similar name: " + obj.name);
                        foundSimilar = true;
                    }
                }

                if (!foundSimilar)
                {
                    Debug.LogWarning("No objects with 'Player' in name found in scene");
                }
            }
        }
    }

    private bool IsWithinBounds(Vector3 position, float minX, float maxX, float minZ, float maxZ)
    {
        return position.x >= minX && position.x <= maxX &&
               position.z >= minZ && position.z <= maxZ;
    }

    private void TeleportToEndZone()
    {
        // Calculate center point of end zone
        float centerX = (endZoneMinX + endZoneMaxX) / 2f;
        float centerZ = (endZoneMinZ + endZoneMaxZ) / 2f;

        // Get terrain height at end zone and add 1 meter offset
        float targetY = endZoneTerrainHeight + teleportHeightOffset;

        Vector3 oldPosition = playerHuman.transform.position;
        Vector3 teleportPosition = new Vector3(centerX, targetY, centerZ);

        playerHuman.transform.position = teleportPosition;

        if (enableDebugLogs)
        {
            Debug.Log(string.Format("TELEPORTED! From ({0:F2}, {1:F2}, {2:F2}) to ({3:F2}, {4:F2}, {5:F2}) | Terrain Height: {6:F2}",
                oldPosition.x, oldPosition.y, oldPosition.z,
                teleportPosition.x, teleportPosition.y, teleportPosition.z,
                endZoneTerrainHeight));
        }
    }

    private void OnDrawGizmos()
    {
        // Update terrain heights in editor for gizmo preview
        if (!Application.isPlaying)
        {
            terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                terrain = FindObjectOfType<Terrain>();
            }

            if (terrain != null)
            {
                UpdateTerrainHeights();
            }
        }

        // Draw Start Zone
        DrawZoneGizmo(startZoneMinX, startZoneMaxX, startZoneMinZ, startZoneMaxZ,
            startZoneTerrainHeight, startZoneColor, "START");

        // Draw End Zone
        DrawZoneGizmo(endZoneMinX, endZoneMaxX, endZoneMinZ, endZoneMaxZ,
            endZoneTerrainHeight, endZoneColor, "END");

        // Draw teleport target position
        float centerX = (endZoneMinX + endZoneMaxX) / 2f;
        float centerZ = (endZoneMinZ + endZoneMaxZ) / 2f;
        Vector3 teleportTarget = new Vector3(centerX, endZoneTerrainHeight + teleportHeightOffset, centerZ);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(teleportTarget, 0.5f);
        Gizmos.DrawLine(teleportTarget, teleportTarget + Vector3.up * 2f);

        // Draw player position if found
        if (showPlayerPositionGizmo && playerHuman != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerHuman.transform.position, 0.5f);
            Gizmos.DrawLine(playerHuman.transform.position, playerHuman.transform.position + Vector3.up * 2f);
        }
    }

    private void DrawZoneGizmo(float minX, float maxX, float minZ, float maxZ,
        float terrainHeight, Color color, string label)
    {
        Gizmos.color = color;

        // Calculate center and size - now positioned at terrain height
        Vector3 center = new Vector3(
            (minX + maxX) / 2f,
            terrainHeight + (gizmoHeight / 2f),
            (minZ + maxZ) / 2f
        );

        Vector3 size = new Vector3(
            maxX - minX,
            gizmoHeight,
            maxZ - minZ
        );

        Gizmos.DrawCube(center, size);

        // Draw wireframe for clarity
        Gizmos.color = new Color(color.r, color.g, color.b, 1f);
        Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
        // Draw label
        UnityEditor.Handles.Label(center + Vector3.up * (gizmoHeight / 2f + 1f), 
            string.Format("{0}\nH:{1:F1}", label, terrainHeight));
#endif
    }
}
