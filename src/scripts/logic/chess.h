#pragma once
#include <string>
#include <sstream>
#include <iostream>
#include <vector>

enum piece_type_l {
	p, n, b, r, q, k, P, N, B, R, Q, K, EMPTY
};

//Cell struct
struct Cell_l {
	char piece;
	bool empty;
	char x;
	char y;
};

//Pieces struct
//Contains the piece names and their values
struct Piece_l {
	char name;
	bool white;
	bool is_king;
	bool alive;
	struct Cell_l *cell;
	struct attack_cells *attack_cells[64];
};
struct Board_l {
	struct Cell_l cells[8][8];
	struct Piece_l pieces[32];
	bool white_turn;
	bool white_in_check;
	bool black_in_check;
	bool white_in_checkmate;
	bool black_in_checkmate;
	bool stalemate;
	bool wOO, wOOO, bOO, bOOO;
	std::string en_pass_sq;
	int half_move_counter;
	int full_move_counter;
};

//FEN string parser
//Takes a FEN string and returns a board struct

//Initialize the board
class Chess{
public:
	//Initialize the board
	static void init_board(struct Board_l *board);

	static void place_piece(struct Board_l *board, char piece, int x, int y, bool white, int piece_num);

	static void parse_fen(struct Board_l *board, std::string fen);
};