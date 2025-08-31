using System.Collections.Generic;
using UnityEngine;

public class Grid3DRenderer : MonoBehaviour
{
    [Header("World reference")]
    public WorldTester tester;

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject robotRPrefab;
    public GameObject robotGPrefab;
    public GameObject robotBPrefab;
    public GameObject jewelRPrefab;
    public GameObject jewelGPrefab;
    public GameObject jewelBPrefab;
    public GameObject box;

    [Header("Layout")]
    public float cellSize = 1.2f;
    public float yTile = 0f;
    public float yPiece = 0.1f;
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

                char target = World.grid[x, y].color;
                var rend = tile.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    Color c = Color.black;
                    if (target == 'R') c = new Color(0.6f, 0.2f, 0.2f);
                    else if (target == 'G') c = new Color(0.2f, 0.6f, 0.2f);
                    else if (target == 'B') c = new Color(0.2f, 0.3f, 0.7f);
                    rend.material.color = c;
                }
            }
        }

        if (box != null)
        {
            Vector3 centerLocal = new Vector3((U - 1) * cellSize * 0.5f, 0f, (V - 1) * cellSize * 0.5f);
            Vector3 centerPos = centerLocal + (centerBoard ? new Vector3(-(U - 1) * cellSize * 0.5f, 0f, -(V - 1) * cellSize * 0.5f) : Vector3.zero);
            var go = Instantiate(box, centerPos, Quaternion.identity, tilesRoot);
            go.name = "Box";
        }
    }

    public void RefreshPieces()
    {
        ClearPieces();

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

                if (cell.state == CellState.robot && cell.LayingRobot != null)
                {
                    var r = cell.LayingRobot.Color;
                    GameObject prefab = r == 'R' ? robotRPrefab : (r == 'G' ? robotGPrefab : robotBPrefab);
                    var go = Instantiate(prefab, p, Quaternion.identity, piecesRoot);

                    if (cell.LayingRobot.CarryingJewel != null)
                    {
                        // Carrying Jewel
                        if (cell.LayingRobot.Color == 'R')
                        {
                            var redBadge = Instantiate(jewelRPrefab);
                            redBadge.name = "Badge";

                            redBadge.transform.SetParent(go.transform, false);
                            redBadge.transform.localPosition = Vector3.up * 1.6f;
                            redBadge.transform.localRotation = Quaternion.identity;
                            DestroyImmediate(redBadge.GetComponent<Collider>());
                        }
                        if (cell.LayingRobot.Color == 'B')
                        {
                            var blueBadge = Instantiate(jewelBPrefab);
                            blueBadge.name = "Badge";

                            blueBadge.transform.SetParent(go.transform, false);
                            blueBadge.transform.localPosition = Vector3.up * 1.6f;
                            blueBadge.transform.localRotation = Quaternion.identity;
                            DestroyImmediate(blueBadge.GetComponent<Collider>());
                        }
                        if (cell.LayingRobot.Color == 'G')
                        {
                            var greenBadge = Instantiate(jewelGPrefab);
                            greenBadge.name = "Badge";

                            greenBadge.transform.SetParent(go.transform, false);
                            greenBadge.transform.localPosition = Vector3.up * 1.6f;
                            greenBadge.transform.localRotation = Quaternion.identity;
                            DestroyImmediate(greenBadge.GetComponent<Collider>());
                        }

                        // var jc = cell.LayingRobot.CarryingJewel.Color;
                        // var br = redBadge.GetComponent<Renderer>();
                        // if (br) br.material.color = (jc == 'R') ? Color.red : (jc == 'G' ? Color.green : Color.blue);
                    }
                }
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
    private World GetPrivateWorldFromTester()
    {
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