#include "scripts/square.h"

Square::Square()
{
}

Square* Square::Clone()
{
    Square* square = new Square();
    return square;
}

Square::~Square()
{
}

void Square::Init()
{
    GetEntity()->GetComponent<Nomu::UI_Sprite>()->SetZ(-1);
    if(piece != nullptr)
        piece->GetEntity()->GetComponent<Nomu::EventListener>()->active = false;
}

void Square::Update(float dt)
{
    if (piece != nullptr)
    {
        if (GetEntity()->GetComponent<Nomu::EventListener>()->isLeftClickedInside())
        {
            std::cout << "Square is being clicked " << coordX << " " << coordY << std::endl;
            piece->GetEntity()->GetComponent<Nomu::EventListener>()->active = true;
            sqaureClicked = true;
        }

        if(!sqaureClicked)
            piece->GetEntity()->GetComponent<Nomu::EventListener>()->active = false;
    }
}

void Square::Render()
{
}

void Square::ProcessInput(float dt)
{
}
