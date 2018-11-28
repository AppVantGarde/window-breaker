using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInfo : MonoBehaviour
{
    [Header( "Skybox" )]
    public Color topColor;
    public Color middleColor;
    public Color bottomColor;

    public float gameTime = 5;
    public int ammoCount;
    public int windowCount;

    private bool _allSetsCompleted;
    private int _activeWindowSetIndex;
    public BreakableWindowSet[ ] breakableSets;

    public void Awake( )
    {
        _activeWindowSetIndex = 0;
    }

    public void Update( )
    {
        if(_allSetsCompleted)
            return;

        bool setCompleted = true;
        for(int i = 0; i < breakableSets[_activeWindowSetIndex].windows.Length; i++)
        {
            if(!breakableSets[_activeWindowSetIndex].windows[i].hasBeenBroken)
            {
                setCompleted = false;
                break;
            }
        }

        if(setCompleted)
        {
            if(++_activeWindowSetIndex > breakableSets.Length - 1)
            {
                _activeWindowSetIndex = breakableSets.Length - 1;
                _allSetsCompleted = true;
            }
        }
    }

    public BreakableWindowSet GetActiveWindowSet( )
    {
        return breakableSets[_activeWindowSetIndex];
    }

    public bool CompletedAllSets( )
    {
        return _allSetsCompleted;
    }

    public int TotalWindowCount( )
    {
        int result = 0;
        for(int i = 0; i < breakableSets.Length; i++)
            result += breakableSets[i].windows.Length;

        return result;
    }
}




