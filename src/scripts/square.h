#pragma once
#include <core/app.h>
#include <components/script.h>

#include "scripts/piece.h"

class Square : public Nomu::Script
{
public:
    Square();
    ~Square();

    void Init();
    void Update(float dt);
    void Render();
    void ProcessInput(float dt);

    Square* Clone() override;
    int coordX, coordY;
    Piece* piece; 
    bool sqaureClicked = false;
private:
};
