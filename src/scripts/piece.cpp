#include "scripts/piece.h"
#include "scripts/board.h"
#include "scripts/square.h"

Piece::Piece()
{
}

Piece::~Piece()
{
}

void Piece::Init()
{
    active = true;
    GetEntity()->GetComponent<Nomu::UI_Sprite>()->SetTexture(Nomu::ResourceManager::GetTexture(std::string(1, pieceType)));
    GetEntity()->GetComponent<Nomu::UI_Sprite>()->active = true;
    GetEntity()->GetComponent<Nomu::Transform>()->SetScale(glm::vec2(100, 100));
    wasClicked = false;
}

void Piece::Update(float dt)
{
    if(GetEntity()->GetComponent<Nomu::EventListener>()->isLeftClickedandHeld()){
        GetEntity()->GetComponent<Nomu::Transform>()->SetPosition(mApp->mousePos);
        GetEntity()->GetComponent<Nomu::UI_Sprite>()->SetZ(1);
        wasClicked = true;
    }
    else if (wasClicked){
        GetEntity()->GetComponent<Nomu::UI_Sprite>()->SetZ(0);
        Nomu::Entity* parent = GetEntity()->GetParent();
        for(int i = 0; i < 8; i++){
            for(int j = 0; j < 8; j++){
                if(parent->GetComponent<Board>()->board[i][j]->GetComponent<Nomu::EventListener>()->isHovered()){
                    if(parent->GetComponent<Board>()->board[i][j]->GetComponent<Square>()->piece != nullptr){
                        if(parent->GetComponent<Board>()->board[i][j]->GetComponent<Square>()->piece == this){
                            GetEntity()->GetComponent<Nomu::Transform>()->SetPosition(square->GetEntity()->GetComponent<Nomu::Transform>()->GetPosition());
                            GetEntity()->GetComponent<Nomu::EventListener>()->active = false;
                            wasClicked = false;
                            return;
                        }
                        else
                            parent->GetComponent<Board>()->board[i][j]->GetComponent<Square>()->piece->GetEntity()->Destroy();
                    }
                    square->piece = nullptr;
                    parent->GetComponent<Board>()->board[i][j]->GetComponent<Square>()->piece = this;
                    square = parent->GetComponent<Board>()->board[i][j]->GetComponent<Square>();
                    GetEntity()->GetComponent<Nomu::Transform>()->SetPosition(parent->GetComponent<Board>()->board[i][j]->GetComponent<Nomu::Transform>()->GetPosition());
                }
            }
        }
        GetEntity()->GetComponent<Nomu::EventListener>()->active = false;
        wasClicked = false;
        return;
    }
}

void Piece::Render()
{
}

Piece* Piece::Clone()
{
    Piece* piece = new Piece();
    piece->pieceType = pieceType;
    return piece;
}

void Piece::ProcessInput(float dt)
{
}
