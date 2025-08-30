using System.Collections.Generic;
using UnityEngine;

public class Grid3DRenderer : MonoBehaviour
{
    [Header("World reference")]
    public WorldTester tester; // drag the WorldTester in the Inspector

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject robotRPrefab;
    public GameObject robotGPrefab;
    public GameObject robotBPrefab;
    public GameObject jewelRPrefab;
    public GameObject jewelGPrefab;
    public GameObject jewelBPrefab;

    [Header("Layout")]
    public float cellSize = 1.2f; // spacing between cells
    public float yTile = 0f;      // tile height (y)
    public float yPiece = 0.6f;   // piece height (y), a bit above the tile
    public bool centerBoard = true;

    // Internals
    private Transform tilesRoot;
    private Transform piecesRoot;
    private Dictionary<(int x,int y), GameObject> robotAt = new();
    private Dictionary<(int x,int y), GameObject> jewelAt = new();

    void Awake()
    {
        tilesRoot = new GameObject("Tiles").transform;
        tilesRoot.SetParent(transform, false);
        piecesRoot = new GameObject("Pieces").transform;
        piecesRoot.SetParent(transform, false);
    }

    public void BuildBoard()
    {
        ClearChildren(tilesRoot);
        ClearPieces();

        int U = tester != null ? testerWorld().U : 0;
        int V = tester != null ? testerWorld().V : 0;

        Vector3 offset = Vector3.zero;
        if (centerBoard)
        {
            // center the board around (0,0,0) on XZ plane
            float width = (U - 1) * cellSize;
            float depth = (V - 1) * cellSize;
            offset = new Vector3(-width * 0.5f, 0f, -depth * 0.5f);
        }

        for (int x = 0; x < U; x++)
        {
            for (int y = 0; y < V; y++)
            {
                Vector3 p = offset + new Vector3(x * cellSize, yTile, y * cellSize);
                var tile = Instantiate(tilePrefab, p, Quaternion.identity, tilesRoot);

                // optional: tint tile by target color
                char target = World.grid[x, y].color;
                var rend = tile.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    Color c = Color.gray;
                    if (target == 'R') c = new Color(0.6f, 0.2f, 0.2f);
                    else if (target == 'G') c = new Color(0.2f, 0.6f, 0.2f);
                    else if (target == 'B') c = new Color(0.2f, 0.3f, 0.7f);
                    rend.material.color = c;
                }
            }
        }
    }

    public void RefreshPieces()
    {
        ClearPieces(); // simple approach; later you can pool instead

        int U = testerWorld().U;
        int V = testerWorld().V;

        Vector3 offset = Vector3.zero;
        if (centerBoard)
        {
            float width = (U - 1) * cellSize;
            float depth = (V - 1) * cellSize;
            offset = new Vector3(-width * 0.5f, 0f, -depth * 0.5f);
        }

        for (int x = 0; x < U; x++)
        {
            for (int y = 0; y < V; y++)
            {
                var cell = World.grid[x, y];
                Vector3 p = offset + new Vector3(x * cellSize, yPiece, y * cellSize);

                // Robot?
                if (cell.state == CellState.robot && cell.LayingRobot != null)
                {
                    var r = cell.LayingRobot.Color;
                    GameObject prefab = r == 'R' ? robotRPrefab : (r == 'G' ? robotGPrefab : robotBPrefab);
                    var go = Instantiate(prefab, p, Quaternion.identity, piecesRoot);

                    // (Optional) add a tiny badge for carried jewel
                    if (cell.LayingRobot.CarryingJewel != null)
                    {
                        var badge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        badge.transform.SetParent(go.transform, false);
                        badge.transform.localPosition = Vector3.up * 0.6f;
                        var jc = cell.LayingRobot.CarryingJewel.Color;
                        var br = badge.GetComponent<Renderer>();
                        if (br) br.material.color = (jc == 'R') ? Color.red : (jc == 'G' ? Color.green : Color.blue);
                        DestroyImmediate(badge.GetComponent<Collider>()); // cosmetic
                    }
                }
                // Jewel?
                else if (cell.state == CellState.jewel && cell.LayingJewel != null)
                {
                    var j = cell.LayingJewel.Color;
                    GameObject prefab = j == 'R' ? jewelRPrefab : (j == 'G' ? jewelGPrefab : jewelBPrefab);
                    Instantiate(prefab, p, Quaternion.identity, piecesRoot);
                }
            }
        }
    }

    private World testerWorld() => GetPrivateWorldFromTester();

    // Helper: access the World instance your tester constructed
    private World GetPrivateWorldFromTester()
    {
        // Expose a getter in WorldTester if you prefer; for now we’ll assume:
        // add a public property World CurrentWorld { get; } in WorldTester and return it here.
        return tester.CurrentWorld;
    }

    private void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    private void ClearPieces()
    {
        ClearChildren(piecesRoot);
        robotAt.Clear();
        jewelAt.Clear();
    }
}