using UnityEngine;
using System.Collections.Generic;

public class PieceQueue
{
    private readonly GameObject[] tetrominoPrefabs;
    private readonly GameObject[] shadowPrefabs;

    public PieceQueue(GameObject[] tetrominoPrefabs, GameObject[] shadowPrefabs)
    {
        this.tetrominoPrefabs = tetrominoPrefabs;
        this.shadowPrefabs = shadowPrefabs;
    }

    private const int BAG_SIZE = 7;
    private const int MIN_QUEUE_SIZE = 14;

    /* Queue preview coordinates */
    private const int PREVIEW_COUNT = 5;
    private const float PREVIEW_COLUMN_X = 12f;
    private const float PREVIEW_START_Y = 22f;
    private const float PREVIEW_Y_OFFSET = 4f;
    private static readonly Vector3 NEW_PIECE_SPAWN = new Vector3(12f, 40f, 0);
    private static readonly Vector3 I_PIECE_SPAWN = new Vector3(4.5f, 20.5f, 0);
    private static readonly Vector3 O_PIECE_SPAWN = new Vector3(4.5f, 21.5f, 0);
    private static readonly Vector3 DEFAULT_SPAWN = new Vector3(4.0f, 21f, 0);

    private List<GameObject> queue = new List<GameObject>();
    private List<GameObject> shadowQueue = new List<GameObject>();
    private int[] generatedBag = new int[BAG_SIZE];

    public GameObject currentShadow { get; private set; }
    private GameObject holdPiece;
    private GameObject holdPieceShadow;

    private static readonly Vector3 HOLD_PIECE_COORDS = new Vector3(-3, 17, 0);
    private static readonly Vector3 HOLD_SHADOW_PIECE_COORDS = new Vector3(-50, 17, 0);

    public GameObject SwapHold()
    {
        if (holdPiece == null)
        {
            holdPiece = queue[0];
            holdPieceShadow = shadowQueue[0];
            MoveToHoldSlot();
            queue.RemoveAt(0);
            shadowQueue.RemoveAt(0);
        }
        else
        {
            (queue[0], holdPiece) = (holdPiece, queue[0]);
            (shadowQueue[0], holdPieceShadow) = (holdPieceShadow, shadowQueue[0]);
            MoveToHoldSlot();
        }

        return TakeNextPiece();
    }

    private void MoveToHoldSlot()
    {
        holdPiece.transform.position = HOLD_PIECE_COORDS;
        holdPiece.transform.rotation = Quaternion.identity;
        holdPieceShadow.transform.position = HOLD_SHADOW_PIECE_COORDS;
    }

    public void PopQueue()
    {
        Object.Destroy(currentShadow.gameObject);
        queue.RemoveAt(0); //shift the queue up
        shadowQueue.RemoveAt(0);
    }

    public GameObject TakeNextPiece()
    {
        /* Top up queue if there isn't two full bags ready */
        if (queue.Count < MIN_QUEUE_SIZE)
        {
            GenerateBag();
            foreach (int i in generatedBag)
            {
                queue.Add(Object.Instantiate(tetrominoPrefabs[i], NEW_PIECE_SPAWN, Quaternion.identity));
                shadowQueue.Add(Object.Instantiate(shadowPrefabs[i], NEW_PIECE_SPAWN, Quaternion.identity));
            }
        } 

        GameObject piece = queue[0];
        currentShadow = shadowQueue[0];

        UpdateQueueDisplay();

        /* Adjust spawn positions dependent on the piece */
        piece.transform.position = GetSpawnLocation(piece);
        return piece;
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
        for (int i = 1; i <= PREVIEW_COUNT; i++)
        {
            queue[i].transform.position = new Vector3(PREVIEW_COLUMN_X, PREVIEW_START_Y - i * PREVIEW_Y_OFFSET, 0);
        }
    }

    private Vector3 GetSpawnLocation(GameObject currentPiece) => currentPiece.name switch
    {
        "I(Clone)" => I_PIECE_SPAWN,
        "O(Clone)" => O_PIECE_SPAWN,
        _ => DEFAULT_SPAWN
    };
}
