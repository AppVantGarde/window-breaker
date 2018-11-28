using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public abstract class SharedPersistentVariable<T> : ScriptableObject
{
    [NonSerialized] private T _value;
    [NonSerialized] private bool _hasReadFromPersistentData;

    public T defaultValue;

    public T Value
    {
        get
        {
            if(!_hasReadFromPersistentData)
            {
                _hasReadFromPersistentData = true;

                string valueKey = "persistentValue_" + name;

                _value = defaultValue;

                if(!SaveGame.Instance.KeyValueExists( valueKey ))
                    SaveGame.Instance.SaveValue( valueKey, _value );
                else
                    SaveGame.Instance.GetValue( valueKey, out _value );
            }

            return _value;
        }

        set
        {
            _value = value;

            SaveGame.Instance.SaveValue( "persistentValue_" + name, _value );

            SaveGame.Instance.Save( );
        }
    }
}
