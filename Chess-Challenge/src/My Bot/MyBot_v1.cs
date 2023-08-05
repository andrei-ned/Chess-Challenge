using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.Application;

public class MyBot_v1_basic : IChessBot
{
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private Board _board;
    private Move _bestMove;
    public int bestEval;

//#if DEBUG
//    private Dictionary<Move, int> moveScores = new Dictionary<Move, int>();
//    private int evalCount;
//#endif

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        //#if DEBUG
        //        moveScores.Clear();
        //        //ConsoleHelper.Log("\nThonk\n", false, ConsoleColor.DarkRed);
        //        evalCount = 0;
        //#endif
        bestEval = Search(4, -1000000000, 1000000000, true);
//#if DEBUG
//        ConsoleHelper.Log($"Evaluated {evalCount} positions in {timer.MillisecondsElapsedThisTurn} milliseconds.");
//        List<KeyValuePair<Move, int>> kvps = moveScores.ToList();
//        kvps.Sort((a, b) => a.Value.CompareTo(b.Value));
//        foreach (var kvp in kvps)
//        {
//            //ConsoleHelper.Log($"{kvp.Key} was scored {kvp.Value}.");
//        }
//#endif
        return _bestMove;
    }

    private int Search(int depth, int alpha, int beta, bool recordMoves = false)
    {
        // First check if there's a checkmate
        if (_board.IsInCheckmate())
            return -10000 * (depth + 1); // multiply by depth, the sooner the mate the better
        if (_board.IsDraw())
            return 0;

        if (depth == 0)
            return Evaluate() * (_board.IsWhiteToMove ? 1 : -1);

        foreach (Move move in _board.GetLegalMoves())
        {
            _board.MakeMove(move);
            int eval = -Search(depth - 1, -beta, -alpha);
            _board.UndoMove(move);
//#if DEBUG
//            if (recordMoves)
//                moveScores[move] = eval;
//#endif
            if (eval >= beta)
            {
                return beta;
            }
            if (eval > alpha)
            {
                alpha = eval;
                if (recordMoves)
                    _bestMove = move;
            }
        }
        return alpha;
    }

    // Evaluates a board, positive score is good for white, negative for black
    private int Evaluate()
    {
//#if DEBUG
//        evalCount++;
//#endif
        // Evaluate based on material value
        int evaluation = 0;
        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            if (pieceType == PieceType.None)
                continue;
            evaluation += _board.GetPieceList(pieceType, true).Count * pieceValues[(int)pieceType];
            evaluation -= _board.GetPieceList(pieceType, false).Count * pieceValues[(int)pieceType];
        }
        return evaluation;
    }
}