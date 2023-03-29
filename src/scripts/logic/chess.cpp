#include "scripts/logic/chess.h"

void Chess::init_board(struct Board_l *board){
	//Initialize the board
	for(int i = 0; i < 8; i++){
		for(int j = 0; j < 8; j++){
			board->cells[i][j].piece = ' ';
			board->cells[i][j].empty = true;
			board->cells[i][j].x = i+'a';
			board->cells[i][j].y = j+'1';
		}
	}

	//Initialize the pieces
	for(int i = 0; i < 32; i++){
		board->pieces[i].name = ' ';
		board->pieces[i].white = false;
		board->pieces[i].is_king = false;
		board->pieces[i].alive = false;
		board->pieces[i].cell = nullptr;
		for(int j = 0; j < 64; j++){
			board->pieces[i].attack_cells[j] = nullptr;
		}
	}
}

void Chess::place_piece(struct Board_l *board, char piece, int x, int y, bool white, int piece_num){
	board->cells[x][y].piece = piece;
	board->cells[x][y].empty = false;

	board->pieces[piece_num].name = piece;
	board->pieces[piece_num].white = islower(piece);
	board->pieces[piece_num].is_king = (piece == 'k' || piece == 'K');
	board->pieces[piece_num].alive = true;
	board->pieces[piece_num].cell = &board->cells[x][y];
}

void Chess::parse_fen(struct Board_l *board, std::string fen){
	//Initialize the board
	init_board(board);

	//create a copy of the fen string
	std::string fen_copy = fen;

	//Split the FEN string into parts
	std::vector<std::string> fen_chunks;
	std::stringstream ssin(fen_copy);
    std::string inter;
	while(getline(ssin, inter, ' ')){
		fen_chunks.push_back(inter);
	}

	int xPos = 0;
	int yPos = 7;
	int piece_i = 0;
	//Place the pieces on the board
	for (auto x = 0; x < fen_chunks[0].length(); x++){
		if (isdigit(fen_chunks[0][x])){
			xPos += fen_chunks[0][x] - '0';
		}
		else if (fen_chunks[0][x] == '/'){
			yPos -= 1;
			xPos = 0;
		}
		else{
			place_piece(board, fen_chunks[0][x], xPos, yPos, true, piece_i);
			piece_i++;
			xPos += 1;
		}
	}

	board->white_turn = fen_chunks[1] == "w";
	board->wOO = true;
	board->wOOO = true;
	board->bOO = true;
	board->bOOO = true;

	for(auto i = 0; i < fen_chunks[2].length(); i++){
		switch(fen_chunks[2][i]){
			case 'K':
				board->wOO = false;
				break;
			case 'Q':
				board->wOOO = false;
				break;
			case 'k':
				board->bOO = false;
				break;
			case 'q':
				board->bOOO = false;
				break;
		}
	}

	if(fen_chunks[3].length() <= 2){
			if(fen_chunks[2] == "-")
				board->en_pass_sq = "--";
			else
				board->en_pass_sq = fen_chunks[2];
	}
	else
		std::cerr << "ERROR : En passant square not found " << fen_chunks[3] << std::endl;

	board->half_move_counter = stoi(fen_chunks[4]);
	board->full_move_counter = stoi(fen_chunks[5]);
}
