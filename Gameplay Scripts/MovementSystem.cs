using UnityEngine;

public class MovementSystem
{

    private readonly GridScript gridScript;
    public MovementSystem(GridScript gridScript)
    {
        this.gridScript = gridScript;
    }

    private enum Direction { None, Left, Right }
    private Direction activeDirection = Direction.None;

    private float DAS = 0.1f; //NES Tetris: 0.27f ---- My Settings: 0.1f
    private float ARR = 0.005f; //NES Tetris: 0.1f ---- My Settings: 0.005f
    private float baseMovementFrequency = 0.8f;
    private float softDropFrequency = 0.05f;
    private float movementFrequency = 0.8f;
    private float passedTime = 0;
    private const int BOARD_WIDTH = 10;


    private class DirectionState //can probably use this to splinter off to an own file for PieceHandler
    {
        public float dasTimer;
        public float arrTimer;
        public Vector3 MoveVector;
    }
    private DirectionState left = new DirectionState{MoveVector = Vector3.left};
    private DirectionState right = new DirectionState{MoveVector = Vector3.right};




    public bool HandleMovement(InputSnapshot frameInput, GameObject currentPiece)
    {
        bool movedLeft = TryInitialMove(frameInput.LeftPressed, Direction.Left, left, currentPiece);
        bool movedRight = TryInitialMove(frameInput.RightPressed, Direction.Right, right, currentPiece);

        UpdateActiveDirection(frameInput);

        HandleDAS(left, frameInput.LeftHeld);
        HandleDAS(right,frameInput.RightHeld);

        if (activeDirection == Direction.Left)
            HandleARR(left, frameInput.LeftHeld, currentPiece);

        else if (activeDirection == Direction.Right)
            HandleARR(right, frameInput.RightHeld, currentPiece);
            
        return movedLeft || movedRight;
    }

    private bool TryInitialMove(bool pressed, Direction direction, DirectionState state, GameObject currentPiece)
    {
        if (!pressed)
            return false;
        
        bool moved = MoveTetromino(state.MoveVector, currentPiece);
        state.dasTimer = DAS;
        state.arrTimer = 0f;
        activeDirection = direction;

        return moved;
    }

    public bool MoveTetromino(Vector3 direction, GameObject currentPiece)
    {
        currentPiece.transform.position += direction;
        if (!gridScript.IsValidPosition(currentPiece.transform))
        {
            currentPiece.transform.position -= direction;
            return false;
        }
        return true;
    }

    private void UpdateActiveDirection(InputSnapshot frameInput)
    {
        if (!frameInput.LeftHeld && activeDirection == Direction.Left && frameInput.RightHeld)
            activeDirection = Direction.Right;
    
        else if (!frameInput.RightHeld && activeDirection == Direction.Right && frameInput.LeftHeld)
            activeDirection = Direction.Left;
        
        else if (!frameInput.LeftHeld && !frameInput.RightHeld)
            activeDirection = Direction.None;
    }

    private void HandleDAS(DirectionState state, bool held)
    {
        if (held) 
            state.dasTimer -= Time.deltaTime;
    }

    private void HandleARR (DirectionState state, bool held, GameObject currentPiece)
    {
        if (!held || state.dasTimer > 0f) 
            return;
        
        state.arrTimer -= Time.deltaTime;
        int moves = 0;

        while (state.arrTimer <= 0f && moves < BOARD_WIDTH)
        {
            MoveTetromino(state.MoveVector, currentPiece);
            state.arrTimer += ARR;
            moves++;
        }
    }

    public void HandleSoftDrop(InputSnapshot frameInput)
    {
        if (frameInput.SoftDropPressed) { passedTime = 0; }
        movementFrequency = frameInput.SoftDropHeld ? softDropFrequency : baseMovementFrequency;
    }

    public bool HandleHardDrop(InputSnapshot frameInput, GameObject currentPiece)
    {
        if (!frameInput.HardDropPressed)
            return false;
  
        while (gridScript.CanMoveDown(currentPiece))
        {
            currentPiece.transform.position += Vector3.down;
        }
        return true;
    }

    public bool ShouldApplyGravity()
    {
        /* Gravity */
        passedTime += Time.deltaTime;
        if (passedTime >= movementFrequency)
        {
            passedTime -= movementFrequency;
            return true;
        }
        return false;
    }
}
