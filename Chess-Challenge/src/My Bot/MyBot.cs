using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int bestEval = int.MinValue;
        //board.IsWhiteToMove
        foreach (var move in moves)
        {
            board.MakeMove(move);
            int eval = Evaluate(board);
            if (board.IsWhiteToMove)
                eval *= -1;
            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
            board.UndoMove(move);
        }
        return bestMove;
    }

    private int Evaluate(Board board)
    {
        int evaluation = 0;
        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            if (pieceType == PieceType.None)
                continue;
            evaluation += board.GetPieceList(pieceType, true).Count * pieceValues[(int)pieceType];
            evaluation -= board.GetPieceList(pieceType, false).Count * pieceValues[(int)pieceType];
            //foreach (var piece in board.GetPieceList(pieceType, true))
            //{
            //    evaluation += pieceValues[(int)pieceType];
            //}
        }
        return evaluation;
    }
}