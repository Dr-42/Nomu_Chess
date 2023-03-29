#pragma once
#include <core/app.h>
#include <components/script.h>

#include "scripts/piece.h"
#include "scripts/square.h"
#include "scripts/logic/chess.h"

class Board : public Nomu::Script
{
public:
    Board();
    ~Board();

    void Init() override;
    void Update(float dt) override;
    void Render() override;
    void SetApp(Nomu::App& app);
    void ProcessInput(float dt);

    void SetSquare(Nomu::Entity* square);
    void SetPiece(Nomu::Entity* piece);

    void CreateBoard();
    Nomu::Entity* board[8][8];

    Board* Clone() override;

private:
    Nomu::App* mApp;
    Nomu::Entity* mSquare;
    Nomu::Entity* mPiece;

    std::vector<Nomu::Entity*> piece_ents;
    Square* squares[8][8];
    Piece* pieces[8][8];
};
