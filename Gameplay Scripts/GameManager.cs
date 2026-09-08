using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
//using UnityEngine.InputSystem;

/* Main Gameplay file for wiring logic together */
public class GameManager : MonoBehaviour
{
    #region DEFINITIONS

    /* Imports */
    private GridScript gridScript;
    private InputHandler input = new InputHandler(); //InputHandler is not a MonoBehaviour class (attached to a gameobject as a component), so doesn't need a GetComponent call
    private RotationSystem rotationSystem;
    private MovementSystem movementSystem ;
    private LockDelaySystem lockDelaySystem;

    /* Constants */

    private const int BAG_SIZE = 7;
    private const int MIN_QUEUE_SIZE = 14; 
    private const int LINES_TO_CLEAR = 40;

    /* Coordinates */
    private static readonly Vector3 NEW_PIECE_SPAWN = new Vector3(12f, 40f, 0);
    private static readonly Vector3 I_PIECE_SPAWN = new Vector3(4.5f, 20.5f, 0);
    private static readonly Vector3 O_PIECE_SPAWN = new Vector3(4.5f, 21.5f, 0);
    private static readonly Vector3 DEFAULT_SPAWN = new Vector3(4.0f,21f,0);
    private static readonly Vector3 HOLD_PIECE_COORDS = new Vector3(-3,17,0);
    private static readonly Vector3 HOLD_SHADOW_PIECE_COORDS = new Vector3(-50,17,0);
    

    private bool enableHold = true;
    private GameObject currentTetromino; //only written by spawntetromino and handlehold
    private GameObject currentShadowTetromino;
    [SerializeField] private GameObject[] Tetrominos;
    [SerializeField] private GameObject[] ShadowTetrominos;


    private List<GameObject> queue = new List<GameObject>();
    private List<GameObject> shadowQueue = new List<GameObject>();
    private int[] generatedBag = new int[BAG_SIZE];
    private GameObject holdPiece;
    private GameObject holdPieceShadow;
    private Label totalLinesClearedText;
    public UIDocument uiDocument;
    private int totalLinesCleared;

    private enum Tetromino
    {
        I, O, T, S, Z, J, L
    }

    #endregion

    #region RUNTIME
    void Awake()
    {
        gridScript = GetComponent<GridScript>(); //any other classes that need GridScript will come after this line
        rotationSystem = new RotationSystem(gridScript);
        movementSystem = new MovementSystem(gridScript);
        lockDelaySystem = new LockDelaySystem(gridScript);
    }

    void Start()
    {
        totalLinesClearedText = uiDocument.rootVisualElement.Q<Label>("totalLinesClearedLabel");
        SpawnTetromino();
    }

    void Update()
    {
        if (totalLinesCleared >= LINES_TO_CLEAR)
        {
            Debug.Log("GAME!"); //works
        }

        if (lockDelaySystem.ShouldLock(currentTetromino)) { LockCurrentPiece(); }

        if (movementSystem.ShouldApplyGravity()) //can use this if check so that gamemanager knows if a gravity drop happened
        {
            movementSystem.MoveTetromino(Vector3.down, currentTetromino); 
        }

        HandleInput(input.GetSnapshot());
        HandleShadowPiece(currentTetromino);
        totalLinesClearedText.text = "Lines: " + totalLinesCleared + " / " + LINES_TO_CLEAR;
    }
    #endregion

    #region INPUTHANDLING
    private void HandleInput(InputSnapshot frameInput) 
    {
        HandlePause(frameInput);
        bool rotated = rotationSystem.HandleRotation(frameInput, currentTetromino);
        bool moved = movementSystem.HandleMovement(frameInput, currentTetromino); //replace currentTetromino with the piecequeue currentTetromino field when that is done

        if ((moved || rotated) && !gridScript.CanMoveDown(currentTetromino))
            lockDelaySystem.RegisterMoveReset();
        
        movementSystem.HandleSoftDrop(frameInput);
        
        if (movementSystem.HandleHardDrop(frameInput, currentTetromino))
            LockCurrentPiece();
        
        HandleHold(frameInput); //keep this function in this file too as it modifies currentTetromino and currentShadowTetromino, which are only defined in this file
    }

    private void HandlePause(InputSnapshot frameInput)
    {
        if (frameInput.PausePressed)
        {
            //create a pause menu game object with menus, and freeze the pieces and game controls
        }
    }

    #endregion
    
    #region PIECEHANDLING

    private void HandleHold(InputSnapshot frameInput)
    {
        if (!(frameInput.HoldPressed && enableHold))
            return;

        if (holdPiece == null)
        {
            holdPiece = queue[0];
            holdPieceShadow = shadowQueue[0];
            holdPieceShadow.transform.position = HOLD_SHADOW_PIECE_COORDS;
            holdPiece.transform.position = HOLD_PIECE_COORDS;
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

        holdPieceShadow.transform.position = HOLD_SHADOW_PIECE_COORDS;
        holdPiece.transform.position = HOLD_PIECE_COORDS;
        holdPiece.transform.rotation = Quaternion.identity;

        currentTetromino = queue[0];
        currentShadowTetromino = shadowQueue[0];

        /* adjust spawn coordinates based on the piece, and place it on the grid */
        currentTetromino.transform.position = GetSpawnLocation(currentTetromino);

        /* Check for game over */
        if (!gridScript.IsValidPosition(currentTetromino.transform))
            GameOver();
        
        enableHold = false;
    }

    void HandleShadowPiece(GameObject currentPiece)
    {
        currentShadowTetromino.transform.position = currentPiece.transform.position;
        currentShadowTetromino.transform.rotation = currentPiece.transform.rotation;
        while (gridScript.CanMoveDown(currentShadowTetromino))
        {
            currentShadowTetromino.transform.position += Vector3.down;
        }
    }

    
    private void SpawnTetromino()
    {
        /* Top up queue if there isn't two full bags ready */
        if (queue.Count < MIN_QUEUE_SIZE)
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
        if (!gridScript.IsValidPosition(currentTetromino.transform))
        {
            GameOver();
        }

        lockDelaySystem.ClearMoveResets();
        enableHold = true;
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

    private Vector3 GetSpawnLocation(GameObject currentPiece) => currentPiece.name switch
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

    private void GameOver()
    {
        Time.timeScale = 0f;
    }
}
