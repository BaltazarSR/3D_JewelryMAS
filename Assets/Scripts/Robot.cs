using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public enum Mission { Collect, Deliver }
public enum IntentType { Idle, Move, PickUp, Drop }

public struct Intent
{
    public IntentType Type;
    public int TargetX, TargetY;   // for Move or PickUp-from
}

public class Robot
{
    public int X, Y;
    public char Color;
    public bool Carrying;
    public Jewel CarryingJewel;
    public Zone CurrentZone;
    public Intent NextIntent;
    public int numMoves = 0;

    // ---- Heat map fields ----
    private int[,] heat = null!;
    private Mission mission, lastMission;
    private float heatWeight = 3f; // tune this (2..5 works well)
    private System.Random rng = new System.Random();

    public void Sense()
    {
        CurrentZone = new Zone(X, Y);
        EnsureHeatInit();
        AddKnowledge(CurrentZone);

        UnityEngine.Debug.Log(CurrentZone);

        mission = Carrying ? Mission.Deliver : Mission.Collect;

        if (heat == null || mission != lastMission) ResetHeat();
        lastMission = mission;
    }

    public void Deliberate()
    {
        // 1) If carrying
        if (Carrying)
        {
            // If adjacent to target and is free
            var target = IsMyTargetHere(CurrentZone);
            if (target != "" && World.grid[GetX(target), GetY(target)].state == CellState.free)
            {
                NextIntent = new Intent { Type = IntentType.Drop, TargetX = GetX(target), TargetY = GetY(target) };
                return;
            }
        }

        // 2) If not carrying & a neighbor has my jewel: pick it up
        if (!Carrying)
        {
            var jewelTarget = IsMyJewelHere(CurrentZone);
            if (jewelTarget != "")
            {
                NextIntent = new Intent { Type = IntentType.PickUp, TargetX = GetX(jewelTarget), TargetY = GetY(jewelTarget) };
                return;
            }
        }

        // Look for cell
        var goal = (mission == Mission.Deliver)
            ? FindNearestEmptyTargetCell()
            : FindNearestJewelCell();

        // Cell found
        if (goal.HasValue)
        {
            var (tx, ty) = HeatAwareStepToward(goal.Value.gx, goal.Value.gy);
            if (tx == X && ty == Y)
            {
                // No better step found → try any cooler free neighbor or idle
                var any = FindCoolestFreeNeighbor();
                NextIntent = any.HasValue
                    ? new Intent { Type = IntentType.Move, TargetX = any.Value.x, TargetY = any.Value.y }
                    : new Intent { Type = IntentType.Idle };
            }
            else
            {
                NextIntent = new Intent { Type = IntentType.Move, TargetX = tx, TargetY = ty };
            }
        }
        else
        {
            // No goal found (e.g., no jewels or no empty targets) → roam a bit or idle
            var roam = FindCoolestFreeNeighbor();
            NextIntent = roam.HasValue
                ? new Intent { Type = IntentType.Move, TargetX = roam.Value.x, TargetY = roam.Value.y }
                : new Intent { Type = IntentType.Idle };
        }


    }

    public void Act()
    {
        switch (NextIntent.Type)
        {
            case IntentType.Drop:
                UnityEngine.Debug.Log("Drop");
                DropAt(World.grid[NextIntent.TargetX, NextIntent.TargetY]);
                break;

            case IntentType.PickUp:
                UnityEngine.Debug.Log("PickUp");
                PickUpFrom(World.grid[NextIntent.TargetX, NextIntent.TargetY]);
                break;

            case IntentType.Move:
                UnityEngine.Debug.Log($"Move intent to ({NextIntent.TargetX},{NextIntent.TargetY}) from ({X},{Y})");

                numMoves++;

                int dx = NextIntent.TargetX - X;
                int dy = NextIntent.TargetY - Y;

                // If not cross-adjacent, clamp to one-step towards target
                if (Math.Abs(dx) + Math.Abs(dy) != 1)
                {
                    int stepX = X, stepY = Y;
                    if (Math.Abs(dx) >= Math.Abs(dy))
                        stepX = X + Math.Sign(dx); // step in X
                    else
                        stepY = Y + Math.Sign(dy); // step in Y

                    UnityEngine.Debug.LogWarning(
                        $"Non-adjacent target detected; clamping to ({stepX},{stepY}).");

                    NextIntent.TargetX = stepX;
                    NextIntent.TargetY = stepY;
                }

                if (TryMoveTo(NextIntent.TargetX, NextIntent.TargetY))
                {
                    heat[X, Y] += 1;
                }
                break;
        }

        NextIntent = new Intent { Type = IntentType.Idle };
    }

    // ---------- Heat helpers ----------

    private void EnsureHeatInit()
    {
        if (heat != null) return;
        var g = World.grid;
        heat = new int[g.GetLength(0), g.GetLength(1)];
        lastMission = Carrying ? Mission.Deliver : Mission.Collect;
    }

    private void ResetHeat()
    {
        var g = World.grid;
        int U = g.GetLength(0), V = g.GetLength(1);
        heat = new int[U, V]; // zeros
        // Optionally seed the current tile with a bit of heat so we don’t hover
        heat[X, Y] = 1;
    }

    // ---------- Goal finders ----------

    private (int gx, int gy)? FindNearestJewelCell()
    {
        var sk = World.sharedKnowledge;
        int U = sk.GetLength(0);
        int V = sk.GetLength(1);
        (int gx, int gy)? best = null;
        int bestD = int.MaxValue;

        for (int x = 0; x < U; x++)
            for (int y = 0; y < V; y++)
            {
                var c = sk[x, y];
                if (c == null) continue;
                if (c.state == CellState.jewel && c.LayingJewel != null && c.LayingJewel.Color == Color)
                {
                    int d = ManDist(X, Y, x, y);
                    if (d < bestD) { bestD = d; best = (x, y); }
                }
            }
        return best;
    }

    private (int gx, int gy)? FindNearestEmptyTargetCell()
    {
        var sk = World.sharedKnowledge;
        int U = sk.GetLength(0);
        int V = sk.GetLength(1);

        (int gx, int gy)? best = null;
        int bestD = int.MaxValue;

        for (int x = 0; x < U; x++)
            for (int y = 0; y < V; y++)
            {
                var c = sk[x, y];
                if (c == null) continue;
                if (c.color == Color && c.state == CellState.free)
                {
                    int d = ManDist(X, Y, x, y);
                    if (d < bestD) { bestD = d; best = (x, y); }
                }
            }
        return best;
    }

    // ---------- Heat-aware steering ----------

    private (int nx, int ny) HeatAwareStepToward(int gx, int gy)
    {
        var candidates = new List<(int x, int y)>
        {
            (X, Y + 1), (X, Y - 1), (X + 1, Y), (X - 1, Y)
        };

        (int x, int y) best = (X, Y);
        float bestScore = ScoreCell(X, Y, gx, gy, stay: true);

        // Shuffle candidates a bit to break ties fairly
        Shuffle(candidates);

        foreach (var (cx, cy) in candidates)
        {
            if (!InBounds(cx, cy)) continue;
            if (World.grid[cx, cy].state != CellState.free) continue;

            float s = ScoreCell(cx, cy, gx, gy);
            if (s < bestScore)
            {
                bestScore = s;
                best = (cx, cy);
            }
        }
        return best;
    }

    private (int x, int y)? FindCoolestFreeNeighbor()
    {
        var dirs = new (int dx, int dy)[] { (0, 1), (0, -1), (1, 0), (-1, 0) };
        (int x, int y)? best = null;
        int bestHeat = int.MaxValue;

        foreach (var (dx, dy) in dirs)
        {
            int nx = X + dx, ny = Y + dy;
            if (InBounds(nx, ny) && World.grid[nx, ny].state == CellState.free)
            {
                int h = heat[nx, ny];
                if (h < bestHeat) { bestHeat = h; best = (nx, ny); }
            }
        }
        return best;
    }

    private float ScoreCell(int cx, int cy, int gx, int gy, bool stay = false)
    {
        // Base attraction to goal:
        int d = ManDist(cx, cy, gx, gy);

        // Repulsion from recently visited tiles:
        float repulsion = heat[cx, cy] * heatWeight;

        // Slight penalty for staying still so we don’t freeze unless necessary
        float stayPenalty = stay ? 0.25f : 0f;

        return d + repulsion + stayPenalty;
    }

    private void Shuffle<T>(IList<T> list)
    {
        // Fisher-Yates
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ---------- Actions & utilities ----------

    private void DropAt(Cell c)
    {
        if (!Carrying || c == null) return;
        if (c.color != Color) return;
        if (c.state != CellState.free) return;

        c.state = CellState.jewel;
        CarryingJewel.X = c.x;
        CarryingJewel.Y = c.y;
        c.LayingJewel = CarryingJewel;
        c.correct = true;

        Carrying = false;
        CarryingJewel = null;
        // Mission will switch on next Sense() → heat resets automatically
    }

    private void PickUpFrom(Cell c)
    {
        if (Carrying || c == null) return;
        if (c.state == CellState.jewel && c.LayingJewel != null && c.LayingJewel.Color == Color)
        {
            Carrying = true;
            CarryingJewel = c.LayingJewel;

            c.LayingJewel = null;
            c.state = CellState.free;
            // Mission will switch on next Sense() → heat resets automatically
        }
    }

    private bool TryMoveTo(int nx, int ny)
    {
        if (!InBounds(nx, ny)) return false;

        int dx = Math.Abs(nx - X);
        int dy = Math.Abs(ny - Y);
        if (!((dx == 1 && dy == 0) || (dx == 0 && dy == 1))) return false;

        var dest = World.grid[nx, ny];
        if (dest.state != CellState.free) return false;

        World.grid[X, Y].state = CellState.free; // vacate
        World.grid[X, Y].LayingRobot = null;
        X = nx; Y = ny;
        World.grid[X, Y].state = CellState.robot; // occupy
        World.grid[X, Y].LayingRobot = this;
        return true;
    }

    private string IsMyTargetHere(Zone zone)
    {
        if (zone.north != null && zone.north.color == Color && zone.north.state == CellState.free) return "north";
        if (zone.south != null && zone.south.color == Color && zone.south.state == CellState.free) return "south";
        if (zone.east != null && zone.east.color == Color && zone.east.state == CellState.free) return "east";
        if (zone.west != null && zone.west.color == Color && zone.west.state == CellState.free) return "west";
        return "";
    }

    private string IsMyJewelHere(Zone zone)
    {
        if (zone.north != null && zone.north.state == CellState.jewel && zone.north.LayingJewel != null && zone.north.LayingJewel.Color == Color && zone.north.correct == false) return "north";
        if (zone.south != null && zone.south.state == CellState.jewel && zone.south.LayingJewel != null && zone.south.LayingJewel.Color == Color && zone.south.correct == false) return "south";
        if (zone.east != null && zone.east.state == CellState.jewel && zone.east.LayingJewel != null && zone.east.LayingJewel.Color == Color && zone.east.correct == false) return "east";
        if (zone.west != null && zone.west.state == CellState.jewel && zone.west.LayingJewel != null && zone.west.LayingJewel.Color == Color && zone.west.correct == false) return "west";
        return "";
    }

    private static bool InBounds(int x, int y)
    {
        var g = World.grid;
        int U = g.GetLength(0), V = g.GetLength(1);
        return x >= 0 && x < U && y >= 0 && y < V;
    }

    private static int ManDist(int x1, int y1, int x2, int y2)
        => Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

    private void AddKnowledge(Zone currentZone)
    {
        var sk = World.sharedKnowledge;
        var g = World.grid;

        void copy(Cell c)
        {
            if (c == null) return;
            sk[c.x, c.y] = g[c.x, c.y].Clone();
        }

        copy(currentZone.north);
        copy(currentZone.south);
        copy(currentZone.east);
        copy(currentZone.west);
    }

    private int GetX(string direction)
    {
        if (direction == "east")
        {
            return X + 1;
        }
        if (direction == "west")
        {
            return X - 1;
        }
        else
        {
            return X;
        }
    }

    private int GetY(string direction)
    {
        if (direction == "north")
        {
            return Y + 1;
        }
        if (direction == "south")
        {
            return Y - 1;
        }
        else
        {
            return Y;
        }
    }

}

