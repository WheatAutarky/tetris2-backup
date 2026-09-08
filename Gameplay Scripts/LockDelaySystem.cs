using UnityEngine;

public class LockDelaySystem
{
    private readonly GridScript gridScript;

    public LockDelaySystem(GridScript gridScript)
    {
        this.gridScript = gridScript;
    }

    private readonly float lockDelay = 2f;
    private float lockDelayTimer = 0f;
    private const int MOVE_RESET_LIMIT = 15;
    private int moveResetCount = 0; 

    
    public bool ShouldLock(GameObject currentPiece)
    {
        if (gridScript.CanMoveDown(currentPiece))
        {
            lockDelayTimer = 0f;
            return false;
        }

        lockDelayTimer += Time.deltaTime;
        //Debug.Log("Move reset count: " + movementSystem.moveResetCount);

        if (lockDelayTimer >= lockDelay || moveResetCount > MOVE_RESET_LIMIT)
            return true;
        
        return false;
    }

    public void RegisterMoveReset()
    {
        lockDelayTimer = 0f;
        moveResetCount++;
    }

    public void ClearMoveResets()
    {
        lockDelayTimer = 0f;
        moveResetCount = 0;
    }

}
