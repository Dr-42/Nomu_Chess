#include "scripts/board.h"
#include <ecs/entity.h>
#include <components/everything.h>
#include <glm/glm.hpp>

class Square;

Board::Board()
{
    mApp = nullptr;
    mSquare = nullptr;
    mPiece = nullptr;
}

Board::~Board()
{
}

void Board::Init()
{
    CreateBoard();
    Board_l board;
    Chess::init_board(&board);
    Chess::parse_fen(&board, "rnb1kbnr/pppp1p1p/5q2/8/2B1PppP/5N2/PPPP2P1/RNBQK2R w KQkq - 2 6"); 
    for(int i = 0; i < 8; i++){
        for(int j = 0; j < 8; j++){
            std::cout << board.cells[j][i].piece << "|";
            if(board.cells[j][7-i].piece == ' '){
                continue;
            }
            Nomu::Entity* piece = mPiece->Clone();
            GetEntity()->AddChild(piece);
            piece_ents.push_back(piece);
            piece->GetComponent<Nomu::Transform>()->SetPosition(squares[j][i]->GetEntity()->GetComponent<Nomu::Transform>()->GetPosition());
            squares[j][i]->piece = piece->GetComponent<Piece>(); 
            squares[j][i]->piece->square = squares[j][i];
            squares[j][i]->piece->mApp = mApp;
            if(squares[j][i]->piece != nullptr)
                squares[j][i]->piece->pieceType = board.cells[j][7-i].piece;
        }
        std::cout << std::endl;
    }
}

void Board::Update(float dt)
{
}

void Board::Render()
{
}

void Board::SetApp(Nomu::App& app)
{
    mApp = &app;
}

void Board::ProcessInput(float dt)
{
}

void Board::SetSquare(Nomu::Entity* square)
{
    mSquare = square;
}

void Board::SetPiece(Nomu::Entity* piece)
{
    mPiece = piece;
}

void Board::CreateBoard(){
    for(int i = 0; i < 8; i++){
        for(int j = 0; j < 8; j++){
            Nomu::Entity* square = mSquare->Clone();
            square->GetComponent<Nomu::Transform>()->SetPosition(glm::vec2(i*100 + 50, j*100 + 50));
            square->GetComponent<Nomu::Transform>()->SetScale(glm::vec2(100, 100));
            square->GetComponent<Nomu::Transform>()->SetRotation(0);
            if((i+j)%2 == 0){
                square->GetComponent<Nomu::UI_Sprite>()->SetColor(glm::vec4(0.8, 0.8, 0.8, 1));
            }else{
                square->GetComponent<Nomu::UI_Sprite>()->SetColor(glm::vec4(0.2, 0.2, 0.2, 1));
            }
            square->GetComponent<Square>()->coordX = i;
            square->GetComponent<Square>()->coordY = j;
            square->GetComponent<Square>()->piece = nullptr;
            GetEntity()->AddChild(square);
            board[i][j] = square;
            squares[i][j] = square->GetComponent<Square>();
            for(auto comp : square->GetComponents()){
                comp->active = true;
            }
        }
    }
}

Board* Board::Clone(){
    return new Board(*this);
}
