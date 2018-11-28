using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalBlackboard : ScriptableObject
{
    #region Singleton

    private static GlobalBlackboard _instance;
    public static GlobalBlackboard Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = Resources.Load<GlobalBlackboard>( "global_blackboard" );

                if(_instance == null)
                    _instance = CreateInstance<GlobalBlackboard>( );
            }

            return _instance;
        }
    }

    #endregion

    [NonSerialized] private Dictionary<string, object> _blackboard = new Dictionary<string, object>( );

    public void Set( string valueName, object value )
    {
        if(!_blackboard.ContainsKey( valueName ))
        {
            _blackboard.Add( valueName, value );
            return;
        }

        _blackboard[valueName] = value;
    }

    public T Get<T>( string valueName )
    {
        if(!_blackboard.ContainsKey( valueName ))
        {
            T defaultValue = default( T );

            _blackboard.Add( valueName, defaultValue );

            return defaultValue;
        }

        return (T)_blackboard[valueName];
    }
}
