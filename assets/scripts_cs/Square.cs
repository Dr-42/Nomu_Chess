using TMPro;
using UnityEngine;

/// <summary>
/// Square Behaviour Control
/// </summary>
public class Square : MonoBehaviour
{
    #region Vars
    public Color curCol; // Current Color
    public Vector2Int coor; // Square position in the board
    public string letterCoor; // Letter coordinate of the square
    public Piece holdingPiece = null; // Current piece in this square
    public Color startCol; // Default Color

    public GameObject checkLight; // Indicator that the king in this square is in Check
    public GameObject hoverDot;
    public GameObject hoverBounds;
    public TextMeshPro rank, file; // Rank and file letters for esge squares

    public int team;

    public Board board;

    public int activeSide;
    #endregion

    #region Game Functions

    // Inits the Square
    private void Awake()
    {
        board = GetComponentInParent<Board>();
        activeSide = -1;
    }
    void Start()
    {
        startCol = GetComponent<SpriteRenderer>().color;
        SetLetterCoordinate();
        DisplayLetters();
    }

    /// <summary>
    /// Updates the current piece held by the square
    /// </summary>
    /// <param name="piece">Piece to be held</param>
    public void HoldPiece(Piece piece)
    {
        holdingPiece = piece;
    }
    
    /// <summary>
    /// Assigns letter coordinate to the square
    /// </summary>
    public void SetLetterCoordinate()
    {
        string let1 = "x";
        switch (coor.x)
        {
            case 0:
                let1 = "a";
                break;
            case 1:
                let1 = "b";
                break;
            case 2:
                let1 = "c";
                break;
            case 3:
                let1 = "d";
                break;
            case 4:
                let1 = "e";
                break;
            case 5:
                let1 = "f";
                break;
            case 6:
                let1 = "g";
                break;
            case 7:
                let1 = "h";
                break;
        }
        string let2 = (coor.y + 1).ToString();
        letterCoor = let1 + let2;
    }

    /// <summary>
    /// Shows the rank and file markers at the edge of the squares
    /// </summary>
    public void DisplayLetters()
    {
        int baseRank = activeSide == -1 ? 0 : 7;
        if (coor.x == baseRank)
            rank.text = letterCoor[1].ToString();
        else
            rank.text = "";
        if(coor.y == baseRank)
            file.text = letterCoor[0].ToString();
        else
            file.text = "";
    }
    #endregion

    #region Color Functions

    /// <summary>
    /// Highlights the square with some color
    /// </summary>
    /// <param name="col">color to highlight the square</param>
    public void HoverSquare(Color col)
    {
        curCol = GetComponent<SpriteRenderer>().color;
        GetComponent<SpriteRenderer>().color = col;
    }

    /// <summary>
    /// Unhighlights the square
    /// </summary>
    public void UnHoverSquare()
    {
        GetComponent<SpriteRenderer>().color = curCol;
    }

    public void HightlightSquare(Color col)
    {
        if(holdingPiece != null)
        {
            hoverBounds.SetActive(true);
            hoverBounds.GetComponent<SpriteRenderer>().color = col;
        }
        else
        {
            hoverDot.SetActive(true);
            hoverDot.GetComponent<SpriteRenderer>().color = col;

        }

    }

    public void UnHighlightSquare()
    {
        hoverDot.SetActive(false);
        hoverBounds.SetActive(false);
    }

    ///<summary>
    ///Reset Color to default
    ///</summary>
    public void ResetColor()
    {
        curCol = startCol;
        GetComponent<SpriteRenderer>().color = startCol;
    }

    /// <summary>
    /// Activates the check indicator
    /// </summary>
    public void InCheckSquare()
    {
        checkLight.SetActive(true);
    }
    /// <summary>
    /// Disables the check light
    /// </summary>
    public void ResetCheckLight()
    {
        checkLight.SetActive(false);
    }
    #endregion
}
