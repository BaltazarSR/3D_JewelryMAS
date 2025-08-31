using System.Collections.Generic;
using UnityEngine;

public class WorldTester : MonoBehaviour
{
    [SerializeField] private Grid3DRenderer renderer3D;

    public float tickSeconds = .25f;
    public float simulationDuration = 30f; //Tmax
    private float _acc;
    private float _elapsed;
    private bool _running = true;
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
        if (!_running) return;

        _acc += Time.deltaTime;
        _elapsed += Time.deltaTime;

        // stop after time limit
        if (_elapsed >= simulationDuration || world.CheckCompletion())
        {
            Debug.Log($"Simulation finished in {_elapsed:F2} seconds.");
            Debug.Log($"Total numbers of moves by the three robots {world.robotRed.numMoves + world.robotBlue.numMoves + world.robotGreen.numMoves}.");

            _running = false;
            return;
        }

        // advance one tick when enough time has passed
        if (_acc >= tickSeconds)
        {
            _acc = 0f;
            Tick();
        }
    }
    private void Tick()
    {
        // 1) Sense
        foreach (var r in robots) r.Sense();

        // 2) Deliberate
        foreach (var r in robots) r.Deliberate();

        // 3) Act
        foreach (var r in robots) r.Act();

        // 4) Print
        world.PrintGrid();

        // 5) Update 3D view
        renderer3D.RefreshPieces();
    }
}
