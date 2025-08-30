using System;
using System.Collections.Generic;
public enum CellState { jewel, robot, free }

public class Cell
{
    public CellState state;
    public char color;
    public Jewel LayingJewel;
    public Robot LayingRobot;
    public int x, y;
    public bool correct;

    public Cell(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.state = CellState.free;
        this.color = ' ';
        this.LayingJewel = null;
        this.LayingRobot = null;
        this.correct = false;
    }

    public char ShowColor()
    {
        return color;
    }

    public CellState ShowState()
    {
        return state;
    }

    public override string ToString()
    {
        string jewelInfo = LayingJewel != null
            ? $"Jewel(Color:{LayingJewel.Color}, Pos:({LayingJewel.X},{LayingJewel.Y}))"
            : "None";

        return $"Cell({x},{y}) State:{state}, TargetColor:'{color}', Jewel:{jewelInfo}, Complete:{correct}";
    }

    public Cell Clone()
    {
        Cell clone = new Cell(this.x, this.y)
        {
            state = this.state,
            color = this.color,
            LayingJewel = this.LayingJewel,
            correct = this.correct
        };
        return clone;
    }

}