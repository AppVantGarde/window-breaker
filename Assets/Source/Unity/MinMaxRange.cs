using System;

public class MinMaxRange : Attribute
{
    public float minimum;
    public float maximum;

    public MinMaxRange(float min, float max) { minimum = min; maximum = max; }
}
