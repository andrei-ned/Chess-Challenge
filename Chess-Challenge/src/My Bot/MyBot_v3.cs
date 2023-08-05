using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.Application;

public class MyBot_v3_QSearch : IChessBot
{
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private TranspositionTable _transpositionTable = new TranspositionTable(32000);
    private Board _board;
    private Move _bestMove;
    private Move _bestMoveLastIteration;

    private int[] _moveScores = new int[218];

#if DEBUG
    private Dictionary<Move, int> _moveScoresDict = new Dictionary<Move, int>();
    private int _evalCount;
    private int _tableHitCount;
#endif

    public Move Think(Board board, Timer timer)
    {
        _board = board;
#if DEBUG
        _moveScoresDict.Clear();
        //ConsoleHelper.Log($"Entry size is {TranspositionTable.Entry.GetSize()} bytes", false);
        _evalCount = 0;
        _tableHitCount = 0;
        int bestEval =
#endif
        Search(4, -1000000000, 1000000000, true);
#if DEBUG
        //ConsoleHelper.Log($"Transposition hits: {_tableHitCount}, table has {_transpositionTable.Count} / {_transpositionTable.Capacity} entries", false, ConsoleColor.DarkRed);
        //ConsoleHelper.Log($"Eval value: {bestEval}. Evaluated {_evalCount} positions in {timer.MillisecondsElapsedThisTurn} milliseconds.");
        List<KeyValuePair<Move, int>> kvps = _moveScoresDict.ToList();
        kvps.Sort((a, b) => a.Value.CompareTo(b.Value));
        foreach (var kvp in kvps)
        {
            //ConsoleHelper.Log($"{kvp.Key} was scored {kvp.Value}.");
        }
#endif
        _bestMoveLastIteration = _bestMove;
        return _bestMove;
    }

    private int Search(int depth, int alpha, int beta, bool recordMoves = false)
    {
        // First check if there's a checkmate
        if (_board.IsInCheckmate())
            return -10000 * (depth + 1); // multiply by depth, the sooner the mate the better
        if (_board.IsDraw())
            return 0;

        if (!recordMoves && _transpositionTable.TryGetEvaluation(_board.ZobristKey, depth, alpha, beta, out int tableEval))
        {
#if DEBUG
            _tableHitCount++;
#endif
            return tableEval;
        }

        if (depth == 0)
            return QSearch(alpha, beta);

        var evalType = TableEvalType.Alpha;
        Span<Move> legalMoves = stackalloc Move[218];
        _board.GetLegalMovesNonAlloc(ref legalMoves);
        OrderMoves(ref legalMoves);
        foreach (Move move in legalMoves)
        {
            _board.MakeMove(move);
            int eval = -Search(depth - 1, -beta, -alpha);
            _board.UndoMove(move);
#if DEBUG
            if (recordMoves)
                _moveScoresDict[move] = eval;
#endif
            if (eval >= beta)
            {
                _transpositionTable.StoreEvaluation(depth, eval, _board.ZobristKey, TableEvalType.Beta);
                return beta;
            }
            if (eval > alpha)
            {
                evalType = TableEvalType.Exact;
                alpha = eval;
                if (recordMoves)
                    _bestMove = move;
            }
        }

        _transpositionTable.StoreEvaluation(depth, alpha, _board.ZobristKey, evalType);
        return alpha;
    }

    // Search only captures
    private int QSearch(int alpha, int beta)
    {
        int eval = Evaluate() * (_board.IsWhiteToMove ? 1 : -1);
        if (eval >= beta)
            return beta;
        if (alpha < eval)
            alpha = eval;

        Span<Move> legalMoves = stackalloc Move[256];
        _board.GetLegalMovesNonAlloc(ref legalMoves, true);
        foreach (Move move in legalMoves)
        {
            _board.MakeMove(move);
            eval = -QSearch(-beta, -alpha);
            _board.UndoMove(move);

            if (eval >= beta)
                return beta;
            if (eval > alpha)
                alpha = eval;
        }

        return alpha;
    }

    // Evaluates a board, positive score is good for white, negative for black
    private int Evaluate()
    {
#if DEBUG
        _evalCount++;
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

    private void OrderMoves(ref Span<Move> moves)
    {
        for (int i = 0; i < moves.Length; i++)
            _moveScores[i] = GuessMoveScore(moves[i]);

        _moveScores.AsSpan().Slice(0, moves.Length).Sort(moves, (a, b) => b.CompareTo(a));

        int GuessMoveScore(Move move)
        {
            int guess = 0;
            var movedPiece = move.MovePieceType;
            var capturedPiece = move.CapturePieceType;

            if (capturedPiece != PieceType.None)
                guess = 10 * pieceValues[(int)capturedPiece] - pieceValues[(int)movedPiece];

            if (move.IsPromotion)
                guess += pieceValues[(int)move.PromotionPieceType];

            if (move.IsCastles)
                guess += 10000;

            return guess;
        }
    }

    public class TranspositionTable
    {
        private Dictionary<ulong, Entry> _entryDict;
        private Queue<ulong> _keys;
        private int _capacity;

        public TranspositionTable(int capacity)
        {
            _capacity = capacity;
            _entryDict = new Dictionary<ulong, Entry>();
            _keys = new Queue<ulong>();
        }

        public void StoreEvaluation(int depth, int eval, ulong zobristKey, TableEvalType nodeType)
        {
            while (_entryDict.Count >= _capacity && _keys.Count > 0)
            {
                var oldestKey = _keys.Dequeue();
                _entryDict.Remove(oldestKey);
            }

            _entryDict[zobristKey] = new Entry(eval, (byte)depth, nodeType);
            _keys.Enqueue(zobristKey);
        }

        public bool TryGetEvaluation(ulong zobristKey, int depth, int alpha, int beta, out int eval)
        {
            if (_entryDict.TryGetValue(zobristKey, out Entry entry))
            {
                if (entry._depth >= depth)
                {
                    eval = entry._evalValue;
                    if (entry._nodeType == TableEvalType.Exact)
                        return true;
                    if (entry._nodeType == TableEvalType.Alpha && eval <= alpha)
                        return true;
                    if (entry._nodeType == TableEvalType.Beta && eval >= beta)
                        return true;
                }
            }
            eval = 0;
            return false;
        }

        public bool TryGetEvaluation(ulong zobristKey, out int eval)
        {
            if (_entryDict.TryGetValue(zobristKey, out Entry entry))
            {
                eval = entry._evalValue;
                return true;
            }
            eval = 0;
            return false;
        }

#if DEBUG
        public int Capacity => _capacity;
        public int Count => _entryDict.Count;
#endif


        public struct Entry
        {
            public readonly int _evalValue;
            public readonly byte _depth;
            public readonly TableEvalType _nodeType;

            public Entry(int eval, byte depth, TableEvalType nodeType)
            {
                _evalValue = eval;
                _depth = depth;
                _nodeType = nodeType;
            }

#if DEBUG
            public static int GetSize()
            {
                return System.Runtime.InteropServices.Marshal.SizeOf<Entry>();
            }
#endif
        }
    }

    public enum TableEvalType : byte
    {
        Exact,
        Alpha,
        Beta
    }
}