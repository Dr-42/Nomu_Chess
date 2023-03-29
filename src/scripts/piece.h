#pragma once
#include <utils/resource_manager.h>
#include <core/app.h>
#include <ecs/entity.h>
#include <components/everything.h>
#include <components/script.h>

class Square;

class Piece : public Nomu::Script
{
public:
    Piece();
    ~Piece();

    void Init();
    void Update(float dt);
    void Render();
    void ProcessInput(float dt);

    Piece* Clone() override;

    Nomu::App* mApp;
    char pieceType;
    Square* square;
private:
    bool wasClicked;
};
