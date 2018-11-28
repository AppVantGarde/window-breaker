using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialMove : object
{
    public Player player;

    public SpecialMove( Player player ) { this.player = player; }

    public virtual void SpecialMoveStarted( SpecialMoveType previousMove, int specialMoveFlags ) { }

    public virtual void SpecialMoveEnded( SpecialMoveType previousMove, SpecialMoveType nextMove ) { }

    public virtual bool CanDoSpecialMove( bool forceCheck = false ) { return true; }

    public virtual bool CanBeOverridenBy( SpecialMoveType specialMove ) { return true; }

    public virtual bool CanOverride( SpecialMoveType specialMove ) { return true; }
}

public struct PendingSpecialMove
{
    public SpecialMoveType specialMove;

    public int specialMoveFlags;
}

public enum SpecialMoveType
{
    None,

    Idle,
    Aim,
    Throw,

    Count
}
