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
    private PieceQueue pieceQueue;

    private const int LINES_TO_CLEAR = 40;

    private bool enableHold = true;
    private GameObject currentTetromino; //only written by spawntetromino and handlehold
    private GameObject currentShadowTetromino;
    [SerializeField] private GameObject[] Tetrominos;
    [SerializeField] private GameObject[] ShadowTetrominos;

    private Label totalLinesClearedText;
    public UIDocument uiDocument;
    private GameState gameState = GameState.Playing;
    private int totalLinesCleared;

    private enum GameState {Playing, Paused, GameOverSuccess, GameOverFailure}

    #endregion

    #region RUNTIME
    void Awake()
    {
        gridScript = GetComponent<GridScript>(); //any other classes that need GridScript will come after this line
        rotationSystem = new RotationSystem(gridScript);
        movementSystem = new MovementSystem(gridScript);
        lockDelaySystem = new LockDelaySystem(gridScript);
        pieceQueue = new PieceQueue(Tetrominos, ShadowTetrominos);
    }

    void Start()
    {
        totalLinesClearedText = uiDocument.rootVisualElement.Q<Label>("totalLinesClearedLabel");
        SpawnTetromino();
    }

    void Update()
    {
        CheckGameState();

        if (totalLinesCleared >= LINES_TO_CLEAR)
        {
            gameState = GameState.GameOverSuccess;
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
        if (frameInput.PausePressed)
        {
            gameState = GameState.Paused;
            return;
        }

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
        if (!frameInput.HoldPressed || !enableHold)
            return;

        SetCurrentPiece(pieceQueue.SwapHold());
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
        SetCurrentPiece(pieceQueue.TakeNextPiece());
        enableHold = true;
    }

    private void SetCurrentPiece(GameObject piece)
    {
        currentTetromino = piece;
        currentShadowTetromino = pieceQueue.currentShadow;
        if (!gridScript.IsValidPosition(currentTetromino.transform))
            GameOver();

        lockDelaySystem.ClearMoveResets();
    }

    private void LockCurrentPiece()
    {
        gridScript.UpdateGrid(currentTetromino.transform);
        int linesCleared = gridScript.CheckForLines();
        totalLinesCleared += linesCleared; //40 lines as the goal

        pieceQueue.PopQueue();
        SpawnTetromino();
    }

    #endregion

    private void EndGame()
    {
        
    }

    private void CheckGameState()
    {
        switch (gameState)
        {
            case GameState.Playing:
                break;
            case GameState.Paused:
                HandlePause(input.GetSnapshot());
                break;
            default:
                EndGame();
        }
    }
}
