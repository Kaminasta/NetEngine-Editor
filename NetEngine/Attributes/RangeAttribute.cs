﻿namespace NetEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class RangeAttribute : Attribute
{
    public float Min { get; }
    public float Max { get; }

    public RangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
