using System;
using System.Collections.Generic;

public class Zone
{
    public Cell north;
    public Cell south;
    public Cell east;
    public Cell west;

    public Zone(int x, int y)
    {
        var g = World.grid;
        int U = g.GetLength(0);
        int V = g.GetLength(1);

        north = (y + 1 < V) ? g[x, y + 1] : null;
        south = (y - 1 >= 0) ? g[x, y - 1] : null;
        east = (x + 1 < U) ? g[x + 1, y] : null;
        west = (x - 1 >= 0) ? g[x - 1, y] : null;
    }
    
    public override string ToString()
    {
        string N = north != null ? north.ToString() : "null";
        string S = south != null ? south.ToString() : "null";
        string E = east  != null ? east.ToString()  : "null";
        string W = west  != null ? west.ToString()  : "null";

        return $"Zone:\n  North -> {N}\n  South -> {S}\n  East  -> {E}\n  West  -> {W}";
    }

}
