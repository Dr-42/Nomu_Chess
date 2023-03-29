using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Piece Behaviour control
/// </summary>
public enum PieceType // Types of pieces that can be present
{
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}
public class Piece : MonoBehaviour
{
    // All needed variables
    #region Vars

    private readonly List<Move> allowedMoves = new List<Move>(); // List of moves the piece can move, starting from its current position
    private MoveType moveType; // Type of move the piece is going to move
    private Piece castlingRook; // Once we know which tower the king is trying to castle, we save it here

    public List<Vector2Int> breakPoints = new List<Vector2Int>(); // Coordinates that will break the piece"s direction
    public bool started; // StartOnly MoveType controller, set to true when the piece moves for the first time
    public bool enPassantAvailable; // If the piece can be captured enPassant (Only used for pawns)
    public Square curSquare; // Where is the piece right now
    public Board board;// The board

    public PieceType pieceType;// Which piece it is

    public int team; // Whites = -1, Blacks = 1

    public Piece[] castlingRooks;// Rooks needed for castling (Only for the king piece)

    public bool captured = false; // If the piece captured some other piece
    #endregion

    #region Initialisation

    //Add moves based on each piece type
    public void InitializeMoves()
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                AddPawnAllowedMoves();
                break;
            case PieceType.Rook:
                AddLinearAllowedMoves();
                break;
            case PieceType.Knight:
                AddKnightAllowedMoves();
                break;
            case PieceType.Bishop:
                AddDiagonalAllowedMoves();
                break;
            case PieceType.Queen:
                AddLinearAllowedMoves();
                AddDiagonalAllowedMoves();
                break;
            case PieceType.King:
                AddKingAllowedMoves();
                break;
        }
    }
    #endregion

    #region Move related Functions

    // Once the user drops the piece, we"ll try to move it, if it was dropped in a non-valid square,
    // the piece will be returned to its position
    public void MovePiece(Square square)
    {
        if (CheckValidMove(square))
        {
            // Switch cases for the current move type
            switch (moveType)
            {
                case MoveType.StartOnly:
                    // If the piece is the king and can castle
                    if (pieceType == PieceType.King && CheckCastling(square))
                    {
                        // Update castling tower"s position (depending on where the tower is, we will move it 3 or 2 squares in the "x" axis)
                        if (castlingRook.curSquare.coor.x == 0)
                        {
                            castlingRook.CastleRook(castlingRook.curSquare.coor.x + 3);
                        }
                        else if (castlingRook.curSquare.coor.x == 7)
                        {
                            castlingRook.CastleRook(castlingRook.curSquare.coor.x - 2);
                        }
                        else
                        {
                            Debug.LogError("Rook is in some weird location");
                        }
                    }

                    //if the piece is pawn and it moved two steps we add the enPassant square
                    if (pieceType == PieceType.Pawn && Math.Abs(curSquare.coor.y - square.coor.y) == 2)
                    {
                        board.enPassantSquare = board.GetSquareFromCoordinate(new Vector2Int(square.coor.x, square.coor.y + team)).letterCoor;
                    }
                    break;

                // If the move type involves eating, eat the enemy piece
                case MoveType.Eat:
                    if (CheckEnPassant(square))
                    {
                        Square pawnSquare = board.GetSquareFromCoordinate(new Vector2Int(square.coor.x, square.coor.y + team));
                        EatPiece(pawnSquare.holdingPiece);
                    }
                    else
                    {
                        EatPiece(square.holdingPiece);
                    }
                    break;
                case MoveType.EatMove:
                case MoveType.EatMoveJump:
                    EatPiece(square.holdingPiece);
                    break;
                case MoveType.Move:
                    break;
                // If the move is to capture another pawn enpassant
            }

            // Record move
            board.moves.Add(RecordMove(square));

            // Update piece's current square
            curSquare.HoldPiece(null);
            square.HoldPiece(this);
            curSquare = square;

            //Sound Handling
            if(captured)
                board.PlayCaptureSound();
            else
                board.PlayMoveSound();
            
            // reset the half move clock for the 50 move rule
            if (pieceType == PieceType.Pawn || captured)
            {
                board.resetHalfMove = true;
                captured = false;
            }
            //CheckPromotion
            if (CheckPromotion())
            {
                breakPoints.Clear();
                board.moves.RemoveAt(board.moves.Count - 1);
                transform.position = new Vector2(curSquare.transform.position.x, curSquare.transform.position.y);
                board.promotingPawn = this;
                board.promotionMenu.SetActive(true);
                board.uIManager.FreezeGame();
                return;
            }


            // Change game"s turn
            board.saved = false;
            board.ChangeTurn();

            if (!started)
            {
                started = true;
                enPassantAvailable = true;
            }
        }

        // Clear break points & update piece"s position
        breakPoints.Clear();
        transform.position = new Vector2(curSquare.transform.position.x, curSquare.transform.position.y);
    }

    // Get the relative coordinate starting from this piece's current position
    public Vector2Int GetCoordinateMove(Square square)
    {
        int coorX = (square.coor.x - curSquare.coor.x) * team;
        int coorY = (square.coor.y - curSquare.coor.y) * team;

        return new Vector2Int(coorX, coorY);
    }

    // Check if the piece can move to the given square
    public bool CheckValidMove(Square square)
    {
        Vector2Int coorMove = GetCoordinateMove(square);

        for (int i = 0; i < allowedMoves.Count; i++)
        {
            if (coorMove.x == allowedMoves[i].x && coorMove.y == allowedMoves[i].y)
            {
                moveType = allowedMoves[i].type;
                switch (moveType)
                {
                    case MoveType.StartOnly:
                        // If this piece hasn"t been moved before, can move to the square or is trying to castle
                        if (!started && CheckCanMove(square) && CheckCastling(square))
                            return true;
                        break;
                    case MoveType.Move:
                        if (CheckCanMove(square))
                        {
                            return true;
                        }
                        break;
                    case MoveType.Eat:
                        if (CheckCanEat(square))
                        {
                            return true;
                        }
                        break;
                    case MoveType.EatMove:
                    case MoveType.EatMoveJump:
                        if (CheckCanEatMove(square))
                        {
                            return true;
                        }
                        break;
                }
            }
        }
        return false;
    }

    // Check if this move causes the king to be in check
    public bool CheckValidCheckKingMove(Square square)
    {
        bool avoidsCheck = false;

        Piece oldHoldingPiece = square.holdingPiece;
        Square oldSquare = curSquare;

        curSquare.HoldPiece(null);
        curSquare = square;
        square.HoldPiece(this);

        Piece king = board.GetKingPiece(board.curTurn);
        for (int i = 0; i < board.pieces.Count; i++)
        {
            if (board.pieces[i].CheckValidMove(king.curSquare))
            {
                board.checkingPieces[team] = board.pieces[i];
            }
        }
        // If my king isn"t checked or I can eat the checking piece
        if (!board.IsCheckKing(board.curTurn) || (square == board.checkingPieces[team].curSquare))
        {
            avoidsCheck = true;
        }

        curSquare = oldSquare;
        curSquare.HoldPiece(this);
        square.HoldPiece(oldHoldingPiece);
        return avoidsCheck;
    }

    // Returns if the piece can move to the given square
    private bool CheckCanMove(Square square)
    {
        Vector2Int coorMove = GetCoordinateMove(square);

        // If square is free, square isn"t further away from the breaking squares and the move won"t cause a check
        if (square.holdingPiece == null && CheckBreakPoint(coorMove) && CheckValidCheckKingMove(square)) return true;
        return false;
    }

    // Returns if the piece can eat an enemy piece that is placed in the given square
    private bool CheckCanEat(Square square)
    {
        Vector2Int coorMove = GetCoordinateMove(square);
        // If square is holding an enemy piece, square isn"t further away from the breaking squares and the move won"t cause a check
        if (square.holdingPiece != null && square.holdingPiece.team != team && CheckBreakPoint(coorMove) && CheckValidCheckKingMove(square))
        {
            return true;
        }
        if(CheckEnPassant(square))
        {
            return true;
        }
        return false;
    }

    // Returns if the piece can eat or move to the given square
    private bool CheckCanEatMove(Square square)
    {
        if (CheckCanEat(square) || CheckCanMove(square)) return true;
        return false;
    }

    #endregion

    #region Breakpoint related
    // Checks if the given coordinate isn't farther away from the breaking points.
    // Since the given coordinate is related to the current square"s position,
    // we"ll need to check all the axis possibilities (negatives and positives)
    private bool CheckBreakPoint(Vector2Int coor)
    {
        for (int i = 0; i < breakPoints.Count; i++)
        {
            if (breakPoints[i].x == 0 && coor.x == 0)
            {
                if (breakPoints[i].y < 0 && (coor.y < breakPoints[i].y))
                {
                    return false;
                }
                else if (breakPoints[i].y > 0 && (coor.y > breakPoints[i].y))
                {
                    return false;
                }
            }
            else if (breakPoints[i].y == 0 && coor.y == 0)
            {
                if (breakPoints[i].x > 0 && (coor.x > breakPoints[i].x))
                {
                    return false;
                }
                else if (breakPoints[i].x < 0 && (coor.x < breakPoints[i].x))
                {
                    return false;
                }
            }
            else if (breakPoints[i].y > 0 && (coor.y > breakPoints[i].y))
            {
                if (breakPoints[i].x > 0 && (coor.x > breakPoints[i].x))
                {
                    return false;
                }
                else if (breakPoints[i].x < 0 && (coor.x < breakPoints[i].x))
                {
                    return false;
                }
            }
            else if (breakPoints[i].y < 0 && (coor.y < breakPoints[i].y))
            {
                if (breakPoints[i].x > 0 && (coor.x > breakPoints[i].x))
                {
                    return false;
                }
                else if (breakPoints[i].x < 0 && (coor.x < breakPoints[i].x))
                {
                    return false;
                }
            }
        }
        return true;
    }

    // Add piece"s break positions, squares that are further away won"t be allowed
    public void AddBreakPoint(Square square)
    {
        Vector2Int coorMove = GetCoordinateMove(square);

        for (int j = 0; j < allowedMoves.Count; j++)
        {
            if (coorMove.x == allowedMoves[j].x && coorMove.y == allowedMoves[j].y)
            {
                switch (allowedMoves[j].type)
                {
                    case MoveType.StartOnly:
                    case MoveType.Move:
                    case MoveType.Eat:
                    case MoveType.EatMove:
                        // If square is holding a piece
                        if (square.holdingPiece != null)
                        {
                            breakPoints.Add(coorMove);
                        }
                        break;
                }
            }
        }
    }
    #endregion

    #region Special Movements
    /*Castling Promotion and en Passant related functions*/

    // Castle this tower with the king, updating its position (Function to operate the rook)
    public void CastleRook(int coorX)
    {
        Vector2Int castlingCoor = new Vector2Int(coorX, curSquare.coor.y);
        Square square = board.GetSquareFromCoordinate(castlingCoor);

        curSquare.HoldPiece(null);
        square.HoldPiece(this);
        curSquare = square;
        if (!started) started = true;

        transform.position = new Vector2(curSquare.transform.position.x, curSquare.transform.position.y);
    }

    // Check if the king can make a castle
    private bool CheckCastling(Square square)
    {
        if (pieceType == PieceType.King && (castlingRooks[0] != null || castlingRooks[1] != null))
        {
            if(square.coor.x == 6)
            {
                castlingRook = castlingRooks[0];
            }
            else if(square.coor.x == 2)
            {
                castlingRook = castlingRooks[1];
            }

            if (castlingRook == null)
                return false;
            else
            {
                bool canCastle = board.CheckCastlingSquares(curSquare, castlingRook.curSquare, team);

                return (!castlingRook.started && canCastle);
            }
        }
        else
        {
            return true;
        }
    }

    //Promotion related functions

    public bool CheckPromotion()
    {
        int rank = team == -1 ? 7 : 0;
        if (pieceType == PieceType.Pawn && curSquare.coor.y == rank)
            return true;
        return false;
    }

    //Checks if a pawn has en Passant move
    private bool CheckEnPassant(Square square)
    {
        Vector2Int coorMove = GetCoordinateMove(square);
        Square pawnSquare = board.GetSquareFromCoordinate(new Vector2Int(square.coor.x, square.coor.y + team));
        int rank = team == -1 ? 4 : 3;
        if (pieceType == PieceType.Pawn
            && square.holdingPiece == null
            && CheckBreakPoint(coorMove)
            && CheckValidCheckKingMove(square)
            && curSquare.coor.y == rank)
        {
            if (pawnSquare.holdingPiece != null &&
                pawnSquare.holdingPiece.pieceType == PieceType.Pawn &&
                pawnSquare.holdingPiece.team != team &&
                pawnSquare.holdingPiece.enPassantAvailable == true)
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Allowed Moves
    // Adds an allowed piece move
    private void AddAllowedMove(int coorX, int coorY, MoveType type)
    {
        Move newMove = new Move(coorX, coorY, type);
        allowedMoves.Add(newMove);
    }

    // Pawns allowed moves
    private void AddPawnAllowedMoves()
    {
        AddAllowedMove(0, -1, MoveType.Move);
        AddAllowedMove(0, -2, MoveType.StartOnly);
        AddAllowedMove(-1, -1, MoveType.Eat);
        AddAllowedMove(1, -1, MoveType.Eat);
    }

    // Towers & part of the Queen's alowed moves
    private void AddLinearAllowedMoves()
    {
        for (int coorX = 1; coorX < 8; coorX++)
        {
            AddAllowedMove(coorX, 0, MoveType.EatMove);
            AddAllowedMove(0, coorX, MoveType.EatMove);
            AddAllowedMove(-coorX, 0, MoveType.EatMove);
            AddAllowedMove(0, -coorX, MoveType.EatMove);
        }
    }

    // Bishops & part of the Queen's alowed moves   
    private void AddDiagonalAllowedMoves()
    {
        for (int coorX = 1; coorX < 8; coorX++)
        {
            AddAllowedMove(coorX, -coorX, MoveType.EatMove);
            AddAllowedMove(-coorX, coorX, MoveType.EatMove);
            AddAllowedMove(coorX, coorX, MoveType.EatMove);
            AddAllowedMove(-coorX, -coorX, MoveType.EatMove);
        }
    }

    // Knight allowed moves
    private void AddKnightAllowedMoves()
    {
        for (int coorX = 1; coorX < 3; coorX++)
        {
            for (int coorY = 1; coorY < 3; coorY++)
            {
                if (coorY != coorX)
                {
                    AddAllowedMove(coorX, coorY, MoveType.EatMoveJump);
                    AddAllowedMove(-coorX, -coorY, MoveType.EatMoveJump);
                    AddAllowedMove(coorX, -coorY, MoveType.EatMoveJump);
                    AddAllowedMove(-coorX, coorY, MoveType.EatMoveJump);
                }
            }
        }
    }

    // King's allowed moves (castling included)
    private void AddKingAllowedMoves()
    {
        // Castling moves
        AddAllowedMove(-2, 0, MoveType.StartOnly);
        AddAllowedMove(2, 0, MoveType.StartOnly);

        // Normal moves
        AddAllowedMove(0, -1, MoveType.EatMove);
        AddAllowedMove(-1, -1, MoveType.EatMove);
        AddAllowedMove(-1, 0, MoveType.EatMove);
        AddAllowedMove(-1, 1, MoveType.EatMove);
        AddAllowedMove(0, 1, MoveType.EatMove);
        AddAllowedMove(1, 1, MoveType.EatMove);
        AddAllowedMove(1, 0, MoveType.EatMove);
        AddAllowedMove(1, -1, MoveType.EatMove);
    }
    #endregion

    #region Other Functions
    public void SetStartSquare(Square square)
    {
        curSquare = square;
    }

    // Function called when someone eats this piece
    public void EatMe()
    {
        board.DestroyPiece(this);
        Destroy(gameObject);
    }

    // Called when this piece is eating an enemy piece
    private void EatPiece(Piece piece)
    {
        if (piece != null && piece.team != team)
        {
            captured = true;
            piece.curSquare.holdingPiece = null;
            piece.EatMe();
        }
    }

    /// <summary>
    /// Creates an algebraic move notation for a chess move
    /// </summary>
    /// <param name="targetSquare">Square where the piece is to be placed</param>
    /// <returns></returns>
    public string RecordMove(Square targetSquare)
    {
        string move = "";
        if (team == -1)
            move += board.fullMoveClock.ToString() + ".";
        switch (pieceType)
        {
            case PieceType.Bishop:
                move += "B";
                break;
            case PieceType.King:
                move += "K";
                break;
            case PieceType.Knight:
                move += "N";
                break;
            case PieceType.Queen:
                move += "Q";
                break;
            case PieceType.Rook:
                move += "R";
                break;
        }

        bool showRank = false;
        bool showFile = false;
        switch (moveType)
        {
            case MoveType.Move:
            case MoveType.Eat:
            case MoveType.EatMove:
            case MoveType.EatMoveJump:
 
                foreach (Piece piece in board.pieces)
                {
                    board.AddPieceBreakPoints(piece);
                    if(piece != this && piece.team == team && piece.pieceType == pieceType && pieceType != PieceType.Pawn && piece.CheckValidMove(targetSquare))
                    {
                        if (piece.curSquare.coor.x != curSquare.coor.x)
                        {
                            showFile = true;
                            showRank = false;
                        }
                        else if (piece.curSquare.coor.y != curSquare.coor.y)
                        {
                            showRank = true;
                            showFile = false;
                        }
                        else
                        {
                            showFile = true;
                            showRank = true;
                        }

                    }

                }
                if (showFile)
                {
                    move += curSquare.letterCoor[0];
                }
                if (showRank)
                {
                    move += curSquare.letterCoor[1];
                }

                if (captured)
                {

                    if (pieceType == PieceType.Pawn)
                    {
                        move += curSquare.letterCoor[0];
                    }
                    move += "x";
                }
                move += targetSquare.letterCoor;
                break;

            case MoveType.StartOnly:
                if (pieceType == PieceType.Pawn)
                {
                    move += targetSquare.letterCoor;
                }

                if (pieceType == PieceType.King)
                {
                    if ((curSquare.coor.x - targetSquare.coor.x) == 2)
                    {
                        move = team == -1 ? (board.fullMoveClock.ToString() + ".") : "";
                        move += "O-O-O";
                    }
                    else if ((curSquare.coor.x - targetSquare.coor.x) == -2)
                    {
                        move = team == -1 ? (board.fullMoveClock.ToString() + ".") : "";
                        move += "O-O";
                    }

                }
                break;
        }

        return move;
    }
    #endregion
}