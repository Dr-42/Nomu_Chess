#include "game.h"
#include "scripts/board.h"
#include "scripts/square.h"
#include "scripts/piece.h"
#include <core/input.h>

#include <utils/scene_manager.h>
#include <ecs/entity.h>

MyGame::MyGame()
{
    wireframe = false;
    root = new Nomu::Entity("root");
}

MyGame::~MyGame()
{
    delete root;
}

void MyGame::SetApp(Nomu::App& app)
{
    mApp = &app;
}

void MyGame::Init(){

    bool bh = Nomu::Script::Register<Board>("Board");
    bh = Nomu::Script::Register<Piece>("Piece");
    bh = Nomu::Script::Register<Square>("Square");
    Nomu::SceneManager sceneManager(mApp);
    root = new Nomu::Entity("root");
    Nomu::Entity* square = sceneManager.LoadScene("assets/scenes/square.nsc");
    Nomu::SceneManager sceneManager2(mApp); 
    Nomu::Entity* piece = sceneManager2.LoadScene("assets/scenes/piece.nsc");

    Board* board = new Board();
    root->AddComponent(board);
    board->SetApp(*mApp);
    board->SetSquare(square->GetChild("square"));
    board->SetPiece(piece->GetChild("piece"));

    root->Init();
}

void MyGame::ProcessInput(float dt)
{

    if(mApp->Keys[NOMU_KEY_ESCAPE]){
        mApp->Close();
    }
}

void MyGame::Update(float dt)
{
    root->Update(dt);
}

void MyGame::Render()
{
    root->Render();
}