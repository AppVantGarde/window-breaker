using System;

/// <summary>
/// 
/// </summary>
[Serializable] public struct RangedFloat
{
    public float minimum;
    public float maximum;

    public float Value { get { return UnityEngine.Random.Range(minimum, maximum); } }
}
