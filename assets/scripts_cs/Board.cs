using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Board main script. Controls the main game too
/// </summary>
public class Board : MonoBehaviour
{
    #region Variables

    //Special display
    private readonly List<Square> hoveredSquares = new List<Square>(); // List squares to hover
    private Square closestSquare; // Current closest square when dragging a piece
    public int curTheme = 0; // Current active board theme
    public int curPieceSet = 0; // Current piece set    
    public bool useHover = true; // Hover valid moves & closest square

    [Header("Game state")]//Game state
    public int curTurn = -1; // -1 = whites; 1 = blacks
    public Dictionary<int, Piece> checkingPieces = new Dictionary<int, Piece>(); // Which piece is checking the king (key = team)

    [Header("Prefabs")]//Prefabs
    public GameObject squarePrefab; // Base square
    public GameObject[] piecePrefabs; // All pieces with assigned piecetypes
    public GameObject pieceParent; // GameObject which acts as parent to the pieces
    public GameObject squareParent; // GameObject which acts as parent to the squares 
    public SpriteRenderer[] boardSides;// Sides of the board
    public SpriteRenderer[] boardCorners;// Corners of the board

    [Header("Managers and gameplay")]//Managers and gameplay
    public GameObject promotionMenu; // Menu which appears on promotion
    public GameObject mainAnnouncer; // Shows Checkmate or stalemate
    public UIManager uIManager; // Manages various UI components
    public ThemeManager themeManager; // Manages board theme
    public StockfishManager stockfishManager; //Manages stockfish related code

    [Header("Audio files")]
    public AudioClip moveSound;
    public AudioClip captureSound;
    private AudioSource audioSource;

    [HideInInspector] public Piece promotingPawn; // Pawn which is to promote 

    //Board State
    [TextArea] public string fen; // FEN of the current board state
    [HideInInspector] public string pgn; // PGN of the game till now
    [HideInInspector] public List<string> moves; // List of all moves made
    [HideInInspector] public string enPassantSquare = "-"; // Square where en passant id valid
    [HideInInspector] public int halfMoveClock; // Halfmoves since last pawn move or capture
    [HideInInspector] public int fullMoveClock = 1; // Number of moves in game
    [HideInInspector] public bool resetHalfMove = false; // Whether halfMoveClock is to be reset

    public List<string> gameStates; // FEN of each game state
    public int halfMoveCount = 0; // Number of halfmoves that have happened in the game
    public bool saved = false;

    // Board Contents
    [HideInInspector] public List<Square> squares = new List<Square>(); //List of all squares added by CreateBoard()
    public List<Piece> pieces = new List<Piece>(); // List of all pieces added by PlacePiece()

    #endregion

    #region BoardCreation Stuff
    void Start()
    {
        CreateBoard();
        UpdateGameTheme();
        PlaceStartPieces();
        CheckValidCreation();
    }
    
    /// <summary>
    /// Creates the Chess Board
    /// </summary>
    public void CreateBoard()
    {

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                // Create the cell
                GameObject newSquare = Instantiate(squarePrefab, squareParent.transform);
                squares.Add(newSquare.GetComponent<Square>());

                // Position
                newSquare.transform.localPosition = new Vector2((x + 0.5f), (y + 0.5f));

                if (x % 2 == y % 2)
                {
                    squares[x + 8 * y].team = 1;
                }
                else
                {
                    squares[x + 8 * y].team = -1;
                }
            }
        }
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Places the Default arrangement of pieces (Standard chess variant)
    /// </summary>
    private void PlaceStartPieces()
    {
        if (PlayerPrefs.HasKey("fen"))
        {
            fen = PlayerPrefs.GetString("fen");
            LoadFEN(fen);
            PlayerPrefs.DeleteKey("fen");
            gameStates = new List<string> { fen };
        }
        else
        {
            SaveSystem.LoadState("Last State", this);
        }
    }

    /// <summary>
    /// Creates board from a given FEN
    /// </summary>
    /// <param name="fen">FEN string</param>
    public void LoadFEN(string fen)
    {
        //Removes all pieces
        foreach (Piece piece in pieces)
        {
            Destroy(piece.gameObject);
        }

        pieces = new List<Piece>();
        AddSquareCoordinates(); // Add "local" coordinates to all squares

        #region FENStuff
        int xPos = 0;
        int yPos = 7;
        string[] fenChunks = fen.Split(' '); //Fen parts separated
        for (int x = 0; x < fenChunks[0].Length; x++)
        {
            switch (fenChunks[0][x])
            {
                case 'K':
                    PlacePiece(PieceType.King, xPos, yPos, -1);
                    break;
                case 'k':
                    PlacePiece(PieceType.King, xPos, yPos, 1);
                    break;
                case 'Q':
                    PlacePiece(PieceType.Queen, xPos, yPos, -1);
                    break;
                case 'q':
                    PlacePiece(PieceType.Queen, xPos, yPos, 1);
                    break;
                case 'R':
                    PlacePiece(PieceType.Rook, xPos, yPos, -1);
                    break;
                case 'r':
                    PlacePiece(PieceType.Rook, xPos, yPos, 1);
                    break;
                case 'N':
                    PlacePiece(PieceType.Knight, xPos, yPos, -1);
                    break;
                case 'n':
                    PlacePiece(PieceType.Knight, xPos, yPos, 1);
                    break;
                case 'B':
                    PlacePiece(PieceType.Bishop, xPos, yPos, -1);
                    break;
                case 'b':
                    PlacePiece(PieceType.Bishop, xPos, yPos, 1);
                    break;
                case 'P':
                    PlacePiece(PieceType.Pawn, xPos, yPos, -1);
                    break;
                case 'p':
                    PlacePiece(PieceType.Pawn, xPos, yPos, 1);
                    break;
            }

            if (char.IsDigit(fenChunks[0][x]))
            {
                xPos += (int)char.GetNumericValue(fen[x]);
            }
            else
                xPos += 1;

            if (fenChunks[0][x] == '/')
            {
                yPos -= 1;
                xPos = 0;
            }
        }

        SetStartPiecesCoor(); // Update all piece's coordinate
        AddCastleRooks(); // Add rooks to the king piece
        PawnFirstSquareAdjust(); //Checks if the pawns have already moved

        curTurn = fenChunks[1] == "w" ? -1 : 1;

        //fen cadtling priviledges code
        Piece kingWhite = GetKingPiece(-1);
        Piece kingBlack = GetKingPiece(1);
        bool castleWhiteKing = true, castleWhiteQueen = true, castleBlackKing = true, castleBlackQueen = true;
        for(int i = 0; i < fenChunks[2].Length; i++)
        {
            switch(fenChunks[2][i])
            {
                case 'K':
                    castleWhiteKing = false;
                    break;
                case 'Q':
                    castleWhiteQueen = false;
                    break;
                case 'k':
                    castleBlackKing = false;
                    break;
                case 'q':
                    castleBlackQueen = false;
                    break;
            }
        }

        kingWhite.started = castleWhiteKing && castleWhiteQueen;
        if(kingWhite.castlingRooks[0] != null)
            kingWhite.castlingRooks[0].started = castleWhiteKing;
        if(kingWhite.castlingRooks[1] != null)
            kingWhite.castlingRooks[1].started = castleWhiteQueen;

        kingBlack.started = castleBlackKing && castleBlackQueen;
        if (kingBlack.castlingRooks[0] != null)
            kingBlack.castlingRooks[0].started = castleBlackKing;
        if (kingBlack.castlingRooks[1] != null)
            kingBlack.castlingRooks[1].started = castleBlackQueen;

        if (fenChunks[3] != "-")
        {
            string coordinate = fenChunks[3];
            string row = coordinate[1] == '3' ? "4" : "5";
            coordinate = coordinate[0] + row;
            GetSquareFromLetterCoordinate(coordinate).holdingPiece.enPassantAvailable = true;            
        }

        halfMoveClock = Convert.ToInt32(fenChunks[4]);
        fullMoveClock = Convert.ToInt32(fenChunks[5]);

        #endregion
        UpdateGameTheme();

        //Reset the CheckLights
        foreach (Square sq in squares)
        {
            sq.ResetCheckLight();

            if (IsCheckKing(-1))
            {
                GetKingPiece(-1).curSquare.checkLight.SetActive(true);
            }
            if (IsCheckKing(1))
            {
                GetKingPiece(1).curSquare.checkLight.SetActive(true);
            }

        }
    }

    /// <summary>
    /// Exports the FEN of the current Board state
    /// </summary>
    public void ExportFEN()
    {
        int freeCellCount = 0;
        fen = "";
        for (int y = 7; y > -1; y--)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = GetSquareFromCoordinate(new Vector2Int(x, y)).holdingPiece;
                if (piece == null)
                {
                    freeCellCount += 1;
                }
                else
                {
                    if (freeCellCount != 0)
                    {
                        fen += freeCellCount.ToString();
                        freeCellCount = 0;
                    }
                    if (piece.pieceType == PieceType.King)
                    {
                        if (piece.team == -1)
                            fen += "K";
                        else
                            fen += "k";
                    }
                    else if (piece.pieceType == PieceType.Queen)
                    {
                        if (piece.team == -1)
                            fen += "Q";
                        else
                            fen += "q";
                    }
                    else if (piece.pieceType == PieceType.Rook)
                    {
                        if (piece.team == -1)
                            fen += "R";
                        else
                            fen += "r";
                    }
                    else if (piece.pieceType == PieceType.Bishop)
                    {
                        if (piece.team == -1)
                            fen += "B";
                        else
                            fen += "b";
                    }
                    else if (piece.pieceType == PieceType.Knight)
                    {
                        if (piece.team == -1)
                            fen += "N";
                        else
                            fen += "n";
                    }
                    else if (piece.pieceType == PieceType.Pawn)
                    {
                        if (piece.team == -1)
                            fen += "P";
                        else
                            fen += "p";
                    }

                }
            }
            if (freeCellCount != 0)
            {
                fen += freeCellCount.ToString();
            }
            freeCellCount = 0;
            if (y != 0)
                fen += '/';
        }

        fen += " ";
        string turnChar = curTurn == -1 ? "w" : "b";
        fen += turnChar + " ";

        Piece kingWhite = GetKingPiece(-1);
        Piece kingBlack = GetKingPiece(1);

        if (!kingWhite.started)
        {
            if (kingWhite.castlingRooks[0] != null && !kingWhite.castlingRooks[0].started)
                fen += "K";
            if (kingWhite.castlingRooks[1] != null && !kingWhite.castlingRooks[1].started)
                fen += "Q";
        }
        if (!kingBlack.started)
        {
            if (kingBlack.castlingRooks[0] != null && !kingBlack.castlingRooks[0].started)
                fen += "k";
            if (kingBlack.castlingRooks[1] != null && !kingBlack.castlingRooks[1].started)
                fen += "q";
        }
        fen += " ";

        fen += enPassantSquare + " ";

        fen += halfMoveClock.ToString() + " " + fullMoveClock.ToString();
    }

    // Places a piece at a given square of a given team
    private Piece PlacePiece(PieceType type, int xCoord, int yCoord, int team)
    {
        Square square = GetSquareFromCoordinate(new Vector2Int(xCoord, yCoord));
        GameObject pieceObj;
        Piece piece;
        int prefabIndex = -1;
        switch (type)
        {
            case PieceType.King:
                prefabIndex = 0;
                break;
            case PieceType.Queen:
                prefabIndex = 1;
                break;
            case PieceType.Rook:
                prefabIndex = 2;
                break;
            case PieceType.Knight:
                prefabIndex = 3;
                break;
            case PieceType.Bishop:
                prefabIndex = 4;
                break;
            case PieceType.Pawn:
                prefabIndex = 5;
                break;
        }

        pieceObj = Instantiate(piecePrefabs[prefabIndex], pieceParent.transform);
        pieceObj.transform.position = square.transform.position;

        piece = pieceObj.GetComponent<Piece>();

        piece.team = team;
        piece.curSquare = square;
        piece.board = this;
        pieces.Add(piece);
        piece.InitializeMoves();
        return piece;
    }
    
    // Add the available rooks for castling to the king
    private void AddCastleRooks()
    {
        foreach (Piece piece in pieces)
        {

            if (piece.pieceType == PieceType.King)
            {
                piece.castlingRooks = new Piece[2];

                if (piece.team == -1)
                {
                    Piece rook1 = GetSquareFromCoordinate(new Vector2Int(7, 0)).holdingPiece;
                    if (rook1 != null && rook1.pieceType == PieceType.Rook)
                        piece.castlingRooks[0] = rook1;
                    else piece.castlingRooks[0] = null;

                    Piece rook2 = GetSquareFromCoordinate(new Vector2Int(0, 0)).holdingPiece;
                    if (rook2 != null && rook2.pieceType == PieceType.Rook)
                        piece.castlingRooks[1] = rook2;
                    else piece.castlingRooks[1] = null;

                }
                else if (piece.team == 1)
                {
                    Piece rook1 = GetSquareFromCoordinate(new Vector2Int(7, 7)).holdingPiece;
                    if (rook1 != null && rook1.pieceType == PieceType.Rook)
                        piece.castlingRooks[0] = rook1;
                    else piece.castlingRooks[0] = null;


                    Piece rook2 = GetSquareFromCoordinate(new Vector2Int(0, 7)).holdingPiece;
                    if (rook2 != null && rook2.pieceType == PieceType.Rook)
                        piece.castlingRooks[1] = rook2;
                    else piece.castlingRooks[1] = null;

                }
                else
                {
                    Debug.LogError("Piece with unidentified team");
                }
            }
        }
    }

    // Check if the pawn has moved or not while loading later states from FEN
    private void PawnFirstSquareAdjust()
    {
        int startRank;

        foreach (Piece piece in pieces)
        {
            startRank = piece.team == -1 ? 1 : 6;

            if (piece.pieceType == PieceType.Pawn)
            {
                if (piece.curSquare.coor.y != startRank)
                {
                    piece.started = true;
                }
            }
        }
    }

    private void CheckValidCreation()
    {
        if(IsCheckKing(-1 * curTurn))
        {
            SceneManager.LoadScene(1);
            PlayerPrefs.SetString("message", "King was in check");
        }
    }

    #endregion

    #region Square Functions

    /*
    ---------------
    Square related functions
    ---------------
    */

    // Returns closest square to hovering Position
    public Square GetClosestSquare(Vector2 pos)
    {
        Square square = squares[0];
        float closest = Vector2.Distance(pos, squares[0].transform.position);

        for (int i = 0; i < squares.Count; i++)
        {
            float distance = Vector2.Distance(pos, squares[i].transform.position);

            if (distance < closest)
            {
                square = squares[i];
                closest = distance;
            }
        }
        return square;
    }

    // Returns the square that is at the given coordinate (local position in the board)
    public Square GetSquareFromCoordinate(Vector2Int coor)
    {
        Square square = squares[0];
        for (int i = 0; i < squares.Count; i++)
        {
            if (squares[i].coor.x == coor.x && squares[i].coor.y == coor.y)
            {
                return squares[i];
            }
        }
        return square;
    }

    // Returns the square from its letter coordianate
    public Square GetSquareFromLetterCoordinate(string coordinate)
    {
        if (coordinate.Length > 2)
        {
            Debug.LogError("Invalid Square Address");
            return null;
        }

        foreach (Square square in squares)
        {
            square.SetLetterCoordinate();
            if (coordinate == square.letterCoor)
                return square;
        }
        Debug.LogError("No square found with adress " + coordinate);
        return null;
    }

    // Highlights piece's closest square when dragging
    public void HighlightClosestSquare(Square square)
    {
        if (closestSquare) closestSquare.UnHoverSquare();
        square.HoverSquare(themeManager.themes[curTheme].squareClosest);
        closestSquare = square;
    }

    // Highlight all the piece's allowed moves squares
    public void HighlightValidSquares(Piece piece)
    {
        AddPieceBreakPoints(piece);
        for (int i = 0; i < squares.Count; i++)
        {
            if (piece.CheckValidMove(squares[i]))
            {
                squares[i].HightlightSquare(themeManager.themes[curTheme].squareHover);
                hoveredSquares.Add(squares[i]);
            }
        }
    }

    // Once the piece is dropped, reset all squares colors to the default
    public void ResetHighlightedSquares()
    {
        for (int i = 0; i < hoveredSquares.Count; i++)
        {
            hoveredSquares[i].UnHighlightSquare();
        }
        hoveredSquares.Clear();
        closestSquare.ResetColor();
        closestSquare = null;
    }

    // Set start square's local coordinates
    private void AddSquareCoordinates()
    {
        int coorX;
        int coorY;
        for (int i = 0; i < squares.Count; i++)
        {
            coorX = i % 8;
            coorY = i / 8;
            squares[i].coor = new Vector2Int(coorX, coorY);
            squares[i].startCol = squares[i].GetComponent<SpriteRenderer>().color;
        }
    }

    #endregion

    #region Piece Functions
    /*
    ---------------
    Pieces related functions
    ---------------
    */

    //Promotes the pawn

    public void PromotePiece(PieceType type, Square promotionSquare)
    {
        Vector2Int sqCoor = new Vector2Int(promotionSquare.coor.x, promotionSquare.coor.y);
        promotingPawn.EatMe();

        promotionSquare.holdingPiece = PlacePiece(type, sqCoor.x, sqCoor.y, promotingPawn.team);

        string turnChar = curTurn == -1 ? (fullMoveClock.ToString() + ".") : "";
        string promotionPieceLetter = "";
        switch(promotionSquare.holdingPiece.pieceType)
        {
            case PieceType.Queen:
                promotionPieceLetter = "Q";
                break;
            case PieceType.Rook:
                promotionPieceLetter = "R";
                break;
            case PieceType.Knight:
                promotionPieceLetter = "N";
                break;
            case PieceType.Bishop:
                promotionPieceLetter = "B";
                break;
        }
        string moveName = turnChar + promotionSquare.letterCoor + "=" + promotionPieceLetter;
        moves.Add(moveName);

        UpdateGameTheme();
    }

    // Add pieces squares that are breaking the given piece's allowed positions
    public void AddPieceBreakPoints(Piece piece)
    {
        piece.breakPoints.Clear();
        for (int i = 0; i < squares.Count; i++)
        {
            piece.AddBreakPoint(squares[i]);
        }
    }

    // Get king's given team
    public Piece GetKingPiece(int team)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].team == team && pieces[i].pieceType == PieceType.King)
            {
                return pieces[i];
            }
        }
        return pieces[0];
    }

    // If the king is trying to castle with a rook, we'll check if an enemy piece is targeting any square
    // between the king and the castling rook
    public bool CheckCastlingSquares(Square square1, Square square2, int castlingTeam)
    {
        List<Square> castlingSquares = new List<Square>();

        if (square1.coor.x < square2.coor.x)
        {
            for (int i = square1.coor.x; i < square2.coor.x; i++)
            {
                Vector2Int coor = new Vector2Int(i, square1.coor.y);
                castlingSquares.Add(GetSquareFromCoordinate(coor));
            }
        }
        else
        {
            for (int i = square1.coor.x; i > square2.coor.x; i--)
            {
                Vector2Int coor = new Vector2Int(i, square1.coor.y);
                castlingSquares.Add(GetSquareFromCoordinate(coor));
            }
        }
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].team != castlingTeam)
            {
                AddPieceBreakPoints(pieces[i]);
                for (int j = 0; j < castlingSquares.Count; j++)
                {
                    if (pieces[i].CheckValidMove(castlingSquares[j])) return false;
                }
            }
        }

        return true;
    }

    // Remove the given piece from the pieces list
    public void DestroyPiece(Piece piece)
    {
        piece.curSquare.holdingPiece = null;
        pieces.Remove(piece);
    }

    ///<summary>
    /// Update each piece's coordinates getting the closest square
    /// </summary>
    private void SetStartPiecesCoor()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            Square closestSquare = GetClosestSquare(pieces[i].transform.position);
            closestSquare.HoldPiece(pieces[i]);
            pieces[i].SetStartSquare(closestSquare);
            pieces[i].board = this;
        }
    }

    //Theme update for each piece
    private void SetPieceTheme(Piece piece)
    {
        piece.GetComponent<SpriteRenderer>().sprite = themeManager.pieceSets[curPieceSet].SetPieceSprite(piece.pieceType, piece.team);
    }
    #endregion

    #region Game Functions
    /*
    ---------------
    Game related functions
    ---------------
    */

    // Change turn and checks game over and updates current fen and pgn
    public void ChangeTurn()
    {
        curTurn *= -1;

        //for fen
        if(resetHalfMove)
        {
            halfMoveClock = 0;
            resetHalfMove = false;
        }
        else
            halfMoveClock += 1;

        fullMoveClock += curTurn == -1 ? 1 : 0;
        halfMoveCount += 1;


        ExportFEN();
        gameStates.Add(fen);
        enPassantSquare = "-";

        //resets enpassant
        foreach (Piece piece in pieces)
        {
            piece.enPassantAvailable = false;
        }

        //check light management
        if (!IsCheckKing(-1 * curTurn))
        {
            foreach(Square sq in squares)
            {
                sq.ResetCheckLight();
            }
        }

        if (IsCheckKing(curTurn))
        {
            Square kingSquare = GetKingPiece(curTurn).curSquare;
            kingSquare.InCheckSquare();
            if (!IsCheckMate(curTurn))
                moves[moves.Count - 1] += "+"; // for pgn
        }

        // Various Game over scenarios
        if (IsCheckMate(curTurn))
        {
            mainAnnouncer.SetActive(true);
            Text text = mainAnnouncer.transform.Find("Text").gameObject.GetComponent<Text>();
            text.text = "Checkmate!!";
            moves[moves.Count - 1] += "#"; // for pgn
        }
        else if (IsStaleMate(curTurn))
        {
            mainAnnouncer.SetActive(true);
            Text text = mainAnnouncer.transform.Find("Text").gameObject.GetComponent<Text>();
            text.text = "Stalemate!!";
        }
        else if (IsThreeFold())
        {
            mainAnnouncer.SetActive(true);
            Text text = mainAnnouncer.transform.Find("Text").gameObject.GetComponent<Text>();
            text.text = "Draw";
            uIManager.FreezeGame();
        }
        else if (Is50MoveRule())
        {
            mainAnnouncer.SetActive(true);
            Text text = mainAnnouncer.transform.Find("Text").gameObject.GetComponent<Text>();
            text.text = "Draw";
            uIManager.FreezeGame();
        }
        else if (IsInsufficientMaterial())
        {
            mainAnnouncer.SetActive(true);
            Text text = mainAnnouncer.transform.Find("Text").gameObject.GetComponent<Text>();
            text.text = "Draw";
            uIManager.FreezeGame();
        }

        uIManager.UpdatePGN(); //pgn update
        /*if (stockfishManager.run)
            stockfishManager.ReinitiateAnalysis();*/
    }

    // Check if the king's given team is in check
    public bool IsCheckKing(int team)
    {
        Piece king = GetKingPiece(team);

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].team != king.team)
            {
                AddPieceBreakPoints(pieces[i]);
                if (pieces[i].CheckValidMove(king.curSquare))
                {
                    checkingPieces[team] = pieces[i];
                    return true;
                }
            }
        }
        king.curSquare.ResetColor();
        return false;
    }

    /// <summary>
    /// Check if it's Checkmate
    /// </summary>
    /// <param name="team">team being checkmated</param>
    /// <returns></returns>
    private bool IsCheckMate(int team)
    {
        if (IsCheckKing(team))
        {
            int validMoves = 0;

            for (int i = 0; i < squares.Count; i++)
            {
                for (int j = 0; j < pieces.Count; j++)
                {
                    if (pieces[j].team == team)
                    {
                        if (pieces[j].CheckValidMove(squares[i]))
                        {
                            validMoves++;
                            if (validMoves != 0)
                                return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if it's Stalemate
    /// </summary>
    /// <param name="team">paralysed team</param>
    /// <returns></returns>
    public bool IsStaleMate(int team)
    {
        if (!IsCheckKing(team))
        {
            int validMoves = 0;

            for (int i = 0; i < squares.Count; i++)
            {
                for (int j = 0; j < pieces.Count; j++)
                {
                    if (pieces[j].team == team)
                    {
                        if (pieces[j].CheckValidMove(squares[i]))
                        {
                            validMoves++;
                            if (validMoves != 0)
                                return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }

    //The rest are various draw scenarios
    private bool IsThreeFold()
    {
        Dictionary<string, int> fenDict = new Dictionary<string, int>();
        foreach(string fen in gameStates)
        {
            string[] fenParts = fen.Split(' ');
            string trimmedFen = fenParts[0] +" "+ fenParts[1] +" "+ fenParts[2] +" "+ fenParts[3];
            if (fenDict.ContainsKey(trimmedFen))
                fenDict[trimmedFen]++;
            else
                fenDict[trimmedFen] = 1;
        }
        foreach(string fen in fenDict.Keys)
        {
            if (fenDict[fen] > 2)
                return true;
        }
        return false;
    }

    //Draw on insufficient material
    private bool IsInsufficientMaterial()
    {
        if(pieces.Count < 4)
        {
            foreach(Piece piece in pieces)
            {
                if (piece.pieceType == PieceType.Rook || piece.pieceType == PieceType.Queen || piece.pieceType == PieceType.Pawn)
                    return false;
            }
            return true;
        }
        if(pieces.Count == 4)
        {
            List<Piece> bishops = new List<Piece>();
            List<Piece> finalPieces = new List<Piece>();
            List<Piece> nonBishopPieces = new List<Piece>();
            foreach (Piece piece in pieces)
            {
                if (piece.pieceType == PieceType.Rook || piece.pieceType == PieceType.Queen || piece.pieceType == PieceType.Pawn || piece.pieceType == PieceType.Knight)
                    nonBishopPieces.Add(piece);
                else
                    finalPieces.Add(piece);
            }

            if (nonBishopPieces.Count != 0)
                return false;
            else
            {
                foreach(Piece piece in finalPieces)
                {
                    if(piece.pieceType == PieceType.Bishop)
                    {
                        bishops.Add(piece);
                    }
                }
                if (bishops.Count != 2)
                    return false;
                else if (bishops[0].curSquare.team == bishops[1].curSquare.team)
                    return true;
            }
        }
        return false;
    }

    //50 moves draw without a pawn move or capture
    private bool Is50MoveRule()
    {
        if(halfMoveClock > 49)
        {
            return true;
        }
        return false;
    }

    //Updates the Game Theme
    public void UpdateGameTheme()
    {
        if (PlayerPrefs.HasKey("curTheme"))
            curTheme = PlayerPrefs.GetInt("curTheme");
        else
            curTheme = 0;
        
        if (PlayerPrefs.HasKey("curPieceSet"))
            curPieceSet = PlayerPrefs.GetInt("curPieceSet");
        else
            curPieceSet = 0;


        foreach (SpriteRenderer boardSide in boardSides)
        {
            boardSide.color = themeManager.themes[curTheme].boardSide;
        }

        foreach (SpriteRenderer boardCorner in boardCorners)
        {
            boardCorner.color = themeManager.themes[curTheme].boardCorner;
        }

        Camera.main.backgroundColor = themeManager.themes[curTheme].backgroundColor;

        foreach (Piece piece in pieces)
        {
            SetPieceTheme(piece);
        }
        foreach (Square square in squares)
        {
            if (square.team == -1) square.GetComponent<SpriteRenderer>().color = themeManager.themes[curTheme].squareWhite;
            else if (square.team == 1) square.GetComponent<SpriteRenderer>().color = themeManager.themes[curTheme].squareBlack;
            square.startCol = square.GetComponent<SpriteRenderer>().color;
        }
    }

    //On closing the application, save the last state
    private void OnApplicationQuit()
    {
        SaveSystem.SaveState("Last State", this);
    }
    #endregion

    #region Audio Functions

    //In case of a normal move
    public void PlayMoveSound()
    {
        audioSource.clip = moveSound;
        audioSource.Play();
    }

    //In case of a capture
    public void PlayCaptureSound()
    {
        audioSource.clip = captureSound;
        audioSource.Play();
    }
    #endregion
}

