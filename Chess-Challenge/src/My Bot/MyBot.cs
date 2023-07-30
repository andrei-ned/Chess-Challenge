using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.Application;

public class MyBot : IChessBot
{
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private Board _board;
    private Move _bestMove;

#if DEBUG
    private Dictionary<Move, int> moveScores = new Dictionary<Move, int>();
    private int evalCount;
#endif

    public Move Think(Board board, Timer timer)
    {
        _board = board;
#if DEBUG
        moveScores.Clear();
        ConsoleHelper.Log("\nThonk\n", false, ConsoleColor.DarkRed);
        evalCount = 0;
#endif
        Search(4, true);
#if DEBUG
        ConsoleHelper.Log($"Evaluated {evalCount} positions.");
        List<KeyValuePair<Move, int>> kvps = moveScores.ToList();
        kvps.Sort((a, b) => a.Value.CompareTo(b.Value));
        foreach (var kvp in kvps)
        {
            ConsoleHelper.Log($"{kvp.Key} was scored {kvp.Value}.");
        }
#endif
        return _bestMove;
    }

    private int Search(int depth, bool recordMoves = false)
    {
        // First check if there's a checkmate
        if (_board.IsInCheckmate())
            return -10000 * (depth + 1); // multiply by depth, the sooner the mate the better

        if (depth == 0)
            return Evaluate() * (_board.IsWhiteToMove ? 1 : -1);

        int bestEval = int.MinValue;
        var moves = _board.GetLegalMoves();
        foreach (Move move in moves)
        {
            _board.MakeMove(move);
            int eval = -Search(depth - 1);
#if DEBUG
            if (recordMoves)
                moveScores[move] = eval;
#endif

            if (eval > bestEval)
            {
                bestEval = eval;
                if (recordMoves)
                    _bestMove = move;
            }
            _board.UndoMove(move);
        }
        return bestEval;
    }

    // Evaluates a board, positive score is good for white, negative for black
    private int Evaluate()
    {
#if DEBUG
        evalCount++;
#endif
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