using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    #region DEFINITIONS

    [Header("Imports")]
    private GridScript gridScript;
    private InputHandler input = new InputHandler(); //InputHandler is not a MonoBehaviour class (attached to a gameobject as a component), so doesn't need a GetComponent call

    [Header("Constants")]

    [SerializeField] private float DAS = 0.1f; //NES Tetris: 0.27f ---- My Settings: 0.1f
    [SerializeField] private float ARR = 0.005f; //NES Tetris: 0.1f ---- My Settings: 0.005f

    private float lockDelay = 2f; //starts counting the timer after the next gravity trigger, so is in practice gravity + lockDelay
    private float lockDelayTimer = 0f;

    private int moveResetCount = 0;
    private const int MOVE_RESET_LIMIT = 15;
    private const int BOARD_WIDTH = 10;
    private const int BAG_SIZE = 7;

    private Vector3 NEW_PIECE_SPAWN  = new Vector3(12f, 40f, 0);
    private static readonly Vector3 I_PIECE_SPAWN = new Vector3(4.5f, 20.5f, 0);
    private static readonly Vector3 O_PIECE_SPAWN = new Vector3(4.5f, 21.5f, 0);
    private static readonly Vector3 DEFAULT_SPAWN = new Vector3(4.0f,21f,0);

    [Header("Handling Values/Timers")]
    
    [SerializeField] private float baseMovementFrequency = 0.8f;
    [SerializeField] private float softDropFrequency = 0.05f;
    private float movementFrequency;
    private bool enableHold = true;
    private float passedTime = 0;
    
    private GameObject currentTetromino;
    private GameObject currentShadowTetromino;
    [SerializeField] private GameObject[] Tetrominos;
    [SerializeField] private GameObject[] ShadowTetrominos;

    private enum Direction { None, Left, Right }
    private Direction activeDirection = Direction.None;
    private List<GameObject> queue = new List<GameObject>();
    private List<GameObject> shadowQueue = new List<GameObject>();
    private int[] generatedBag = new int[BAG_SIZE];
    private GameObject holdPiece;
    private GameObject holdPieceShadow;
    private Label totalLinesClearedText;
    public UIDocument uiDocument;
    private int totalLinesCleared;

    private class DirectionState //can probably use this to splinter off to an own file for PieceHandler
    {
        public float dasTimer;
        public float arrTimer;
        public Vector3 MoveVector;
    }

    private enum Tetromino
    {
        
    }

    private DirectionState left = new DirectionState{MoveVector = Vector3.left};
    private DirectionState right = new DirectionState{MoveVector = Vector3.right};

    #endregion

    #region RUNTIME
    void Awake()
    {
        gridScript = GetComponent<GridScript>();
    }

    void Start()
    {
        totalLinesClearedText = uiDocument.rootVisualElement.Q<Label>("totalLinesClearedLabel");
        SpawnTetromino();
    }

    void Update()
    {
        if (totalLinesCleared >= 40)
        {
            Debug.Log("GAME!"); //works
        }

        LockDelayCheck();

        /* Gravity */
        passedTime += Time.deltaTime;
        if (passedTime >= movementFrequency)
        {
            passedTime -= movementFrequency;
            MoveTetromino(Vector3.down);
        }

        HandleInput(input.GetSnapshot());
        totalLinesClearedText.text = "Lines: " + totalLinesCleared + " / 40";
    }
    #endregion

    #region INPUTHANDLING
    private void HandleInput(InputSnapshot frameInput)
    {
        HandlePause(frameInput);
        HandleMovement(frameInput);
        HandleRotation(frameInput);
        HandleSoftDrop(frameInput);
        HandleHardDrop(frameInput);
        HandleHold(frameInput);
        HandleShadowPiece();
    }

    private void HandlePause(InputSnapshot frameInput)
    {
        if (frameInput.PausePressed)
        {
            //create a pause menu game object with menus, and freeze the pieces and game controls
        }
    }

    private void HandleMovement(InputSnapshot frameInput)
    {
        TryInitialMove(frameInput.LeftPressed, Direction.Left, left);
        TryInitialMove(frameInput.RightPressed, Direction.Right, right);

        UpdateActiveDirection(frameInput);

        HandleDAS(left, frameInput.LeftHeld);
        HandleDAS(right,frameInput.RightHeld);

        if (activeDirection == Direction.Left)
            HandleARR(left, frameInput.LeftHeld);

        else if (activeDirection == Direction.Right)
            HandleARR(right, frameInput.RightHeld);
    }

    private void TryInitialMove(bool pressed, Direction direction, DirectionState state)
    {
        if (!pressed)
            return;
        
        MoveTetromino(state.MoveVector);
        state.dasTimer = DAS;
        state.arrTimer = 0f;
        activeDirection = direction;

        if (!CanMoveDown(currentTetromino))
            moveResetCount++;
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

    private void HandleARR (DirectionState state, bool held)
    {
        if (!held || state.dasTimer > 0f) 
            return;
        
        state.arrTimer -= Time.deltaTime;
        int moves = 0;

        while (state.arrTimer <= 0f && moves < BOARD_WIDTH)
        {
            MoveTetromino(state.MoveVector);
            state.arrTimer += ARR;
            moves++;
        }
    }

    private void HandleRotation(InputSnapshot frameInput)
    {
        int direction;
        if (!frameInput.ClockwisePressed && !frameInput.CounterClockwisePressed && !frameInput.InvertPressed)
            return;
        
        if (currentTetromino.name.Contains("O"))
            return;

        if (frameInput.ClockwisePressed) { direction = -90; }
        else if (frameInput.CounterClockwisePressed) { direction = 90; }
        else { direction = 180; }

        Vector3Int[] kicks = GetWallKickTests(currentTetromino, frameInput); // need to read the intended rotation before applying it
        currentTetromino.transform.Rotate(0, 0, direction);
        ApplyWallKickTests(kicks);

        if (!IsValidPosition(currentTetromino.transform))
        {
            currentTetromino.transform.Rotate(0, 0, -direction);
        }
        else if(!CanMoveDown(currentTetromino))
        {
            lockDelayTimer = 0f;
            moveResetCount++;
        }
    }

    private enum RotationState
    {
        Spawn = 0,
        Right = 1,
        Inverted = 2,
        Left = 3
    }

    private Vector3Int[] GetWallKickTests(GameObject tetromino, InputSnapshot frameInput)
    {
        float currentZ = Mathf.Round(tetromino.transform.eulerAngles.z);
        currentZ = ((currentZ % 360) + 360) % 360; // normalize just in case
        var kickList = tetromino.name == "I(Clone)" ? WallKickTables.WallKicksI : WallKickTables.WallKicksJLOSTZ;

        if (currentZ == 0   && frameInput.ClockwisePressed)        return kickList[0]; // 0->R
        if (currentZ == 270 && frameInput.CounterClockwisePressed) return kickList[1]; // R->0
        if (currentZ == 270 && frameInput.ClockwisePressed)        return kickList[2]; // R->2
        if (currentZ == 180 && frameInput.CounterClockwisePressed) return kickList[3]; // 2->R
        if (currentZ == 180 && frameInput.ClockwisePressed)        return kickList[4]; // 2->L
        if (currentZ == 90  && frameInput.CounterClockwisePressed) return kickList[5]; // L->2
        if (currentZ == 90  && frameInput.ClockwisePressed)        return kickList[6]; // L->0
        if (currentZ == 0   && frameInput.CounterClockwisePressed) return kickList[7]; // 0->L

        return kickList[7]; // fallback (also hit by the 180°/invert case, since it's neither CW nor CCW — worth a closer look later)
    }

    private void ApplyWallKickTests(Vector3Int[] kickList)
    {
        Vector3 shiftCoords;
        for (int i = 0; i < kickList.Length; i++)
        {
            shiftCoords = kickList[i];

            currentTetromino.transform.position += shiftCoords;
            if (IsValidPosition(currentTetromino.transform))
                return;

            currentTetromino.transform.position -= shiftCoords;
        }
    }

    private void HandleSoftDrop(InputSnapshot frameInput)
    {
        if (frameInput.SoftDropPressed) { passedTime = 0; }
        movementFrequency = frameInput.SoftDropHeld ? softDropFrequency : baseMovementFrequency;
    }

    private void HandleHardDrop(InputSnapshot frameInput)
    {
        if (!frameInput.HardDropPressed)
            return;
  
        while (CanMoveDown(currentTetromino))
        {
            currentTetromino.transform.position += Vector3.down;
        }
        LockCurrentPiece();
        
    }

    private void HandleHold(InputSnapshot frameInput)
    {
        if (!(frameInput.HoldPressed && enableHold))
            return;

        if (holdPiece == null)
        {
            holdPiece = queue[0];
            holdPieceShadow = shadowQueue[0];
            holdPieceShadow.transform.position = new Vector3(-50,17,0);
            holdPiece.transform.position = new Vector3(-3, 17, 0);
            holdPiece.transform.rotation = Quaternion.identity; //reset rotation when putting into the hold slot
            queue.RemoveAt(0); //shift the queue up

            shadowQueue.RemoveAt(0);
            SpawnTetromino();
            enableHold = false;
            return;
        }

        GameObject switchPiece;
        GameObject switchPieceShadow;

        switchPiece = queue[0];
        queue[0] = holdPiece;
        holdPiece = switchPiece;

        switchPieceShadow = shadowQueue[0];
        shadowQueue[0] = holdPieceShadow;
        holdPieceShadow = switchPieceShadow;

        holdPieceShadow.transform.position = new Vector3(-50,17,0);
        holdPiece.transform.position = new Vector3(-3, 17, 0);
        holdPiece.transform.rotation = Quaternion.identity;

        currentTetromino = queue[0];
        currentShadowTetromino = shadowQueue[0];

        /* adjust spawn coordinates based on the piece, and place it on the grid */
        currentTetromino.transform.position = GetSpawnLocation(currentTetromino);

        /* Check for game over */
        if (!IsValidPosition(currentTetromino.transform))
        {
            Time.timeScale = 0f;
        }
        enableHold = false;

    }

    void HandleShadowPiece()
    {
        currentShadowTetromino.transform.position = currentTetromino.transform.position;
        currentShadowTetromino.transform.rotation = currentTetromino.transform.rotation;
        while (CanMoveDown(currentShadowTetromino))
        {
            currentShadowTetromino.transform.position += Vector3.down;
        }
    }

    #endregion
    
    #region PIECEHANDLING
    private void SpawnTetromino()
    {
        /* Top up queue if there isn't two full bags ready */
        if (queue.Count < 14)
        {
            GenerateBag();
            foreach (int i in generatedBag)
            {
                queue.Add(Instantiate(Tetrominos[i], NEW_PIECE_SPAWN, Quaternion.identity));
                shadowQueue.Add(Instantiate(ShadowTetrominos[i], NEW_PIECE_SPAWN, Quaternion.identity));
            }
        } 

        currentTetromino = queue[0];
        currentShadowTetromino = shadowQueue[0];

        UpdateQueueDisplay();

        /* Adjust spawn positions dependent on the piece */
        currentTetromino.transform.position = GetSpawnLocation(currentTetromino);

        /* Temporary game over measure, doesn't actually end the game as you can still hard drop. */
        if (!IsValidPosition(currentTetromino.transform))
        {
            Time.timeScale = 0f;
        }

        lockDelayTimer = 0f;
        moveResetCount = 0;
        enableHold = true;
    }


    /* Moves piece upon calling in a given direction */
    private void MoveTetromino(Vector3 direction)
    {
        currentTetromino.transform.position += direction;
        if (!IsValidPosition(currentTetromino.transform))
        {
            currentTetromino.transform.position -= direction;
        }
    }

    private bool CanMoveDown(GameObject tetromino)
    {
        tetromino.transform.position += Vector3.down;
        bool canMove = IsValidPosition(tetromino.transform);
        tetromino.transform.position -= Vector3.down;
        return canMove;
    }

    void LockDelayCheck()
    {
        if (CanMoveDown(currentTetromino))
        {
            lockDelayTimer = 0f;
            return;
        }

        lockDelayTimer += Time.deltaTime;
        //Debug.Log("Move reset count: " + moveResetCount);

        if (lockDelayTimer >= lockDelay || moveResetCount > MOVE_RESET_LIMIT)
        {
            LockCurrentPiece();
        }
    }

    /* Call this function to shuffle the bag */
    private void GenerateBag()
    {
        // Fill the array
        for (int i = 0; i < generatedBag.Length; i++)
        {
            generatedBag[i] = i;
        }

        // Fisher-Yates shuffle
        for (int i = generatedBag.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (generatedBag[i], generatedBag[j]) = (generatedBag[j], generatedBag[i]);
        }
    }
    
    private void UpdateQueueDisplay()
    {
        for (int i = 1; i <= 5; i++)
        {
            queue[i].transform.position = new Vector3(12, 22 - i*4, 0);
        }
    }

    private Vector3 GetSpawnLocation(GameObject piece) => piece.name switch
    {
        "I(Clone)" => I_PIECE_SPAWN,
        "O(Clone)" => O_PIECE_SPAWN,
        _ => DEFAULT_SPAWN
    };

    private void LockCurrentPiece()
    {
        gridScript.UpdateGrid(currentTetromino.transform);
        int linesCleared = gridScript.CheckForLines();

        totalLinesCleared += linesCleared; //40 lines as the goal

        Destroy(currentShadowTetromino.gameObject);
        queue.RemoveAt(0); //shift the queue up
        shadowQueue.RemoveAt(0);
        SpawnTetromino();
    }

    #endregion
    /* Ported functions from GridScript */
    private bool IsValidPosition( Transform pieceTransform)
    {
        return gridScript.IsValidPosition(pieceTransform.transform);
    }
}
