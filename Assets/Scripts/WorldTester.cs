using System.Collections.Generic;
using UnityEngine;

public class WorldTester : MonoBehaviour
{
    [SerializeField] private Grid3DRenderer renderer3D;

    public float tickSeconds = 0.25f;
    private float _acc;
    private World world;
    private List<Robot> robots = new();

    public World CurrentWorld => world;

    void Start()
    {
        world = new World();
        world.Start();       // your World’s initializer
        world.PrintGrid();   // prints to Unity Console
        world.CheckCompletion(); // checks if any jewel is in the right place

        robots.Add(world.robotRed);
        robots.Add(world.robotGreen);
        robots.Add(world.robotBlue);

        renderer3D.BuildBoard();     // build tiles once
        renderer3D.RefreshPieces();  // draw initial robots/jewels
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 1) Sense phase (read-only)
            foreach (var r in robots) r.Sense();

            // 2) Deliberate phase (choose intents; still read-only w.r.t world)
            foreach (var r in robots) r.Deliberate();

            // 3) Act phase (mutate grid/positions)
            foreach (var r in robots) r.Act();

            // 4) Print new world
            world.PrintGrid();

            // 5) 3D grid
            renderer3D.RefreshPieces();

        }
    }
}
