using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public abstract class SharedVariable<T> : ScriptableObject
{
    [NonSerialized] public T value;
}
