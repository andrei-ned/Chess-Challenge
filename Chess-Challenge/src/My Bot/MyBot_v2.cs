using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot_v2_TTable : IChessBot
{
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private TranspositionTable _transpositionTable = new TranspositionTable(16000000);
    private Board _board;
    private Move _bestMove;

//#if DEBUG
//    private Dictionary<Move, int> _moveScores = new Dictionary<Move, int>();
//    private int _evalCount;
//    private int _tableHitCount;
//#endif

    public Move Think(Board board, Timer timer)
    {
        _board = board;
//#if DEBUG
//        _moveScores.Clear();
//        //ConsoleHelper.Log($"Entry size is {TranspositionTable.Entry.GetSize()} bytes", false);
//        _evalCount = 0;
//        _tableHitCount = 0;
//#endif
        int bestEval = Search(4, -1000000000, 1000000000, true);
//#if DEBUG
//        //ConsoleHelper.Log($"Transposition hits: {_tableHitCount}, table has {_transpositionTable.Count} / {_transpositionTable.Capacity} entries", false, ConsoleColor.DarkRed);
//        //ConsoleHelper.Log($"Evaluated {_evalCount} positions in {timer.MillisecondsElapsedThisTurn} milliseconds.");
//        List<KeyValuePair<Move, int>> kvps = _moveScores.ToList();
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

        if (!recordMoves && _transpositionTable.TryGetEvaluation(_board.ZobristKey, depth, alpha, beta, out int tableEval))
        {
//#if DEBUG
//            _tableHitCount++;
//#endif
            return tableEval;
        }

        if (depth == 0)
            return Evaluate() * (_board.IsWhiteToMove ? 1 : -1);

        var evalType = TableEvalType.Alpha;
        foreach (Move move in _board.GetLegalMoves())
        {
            //_board.ZobristKey
            _board.MakeMove(move);
            int eval = -Search(depth - 1, -beta, -alpha);
            _board.UndoMove(move);
//#if DEBUG
//            if (recordMoves)
//                _moveScores[move] = eval;
//#endif
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

    // Evaluates a board, positive score is good for white, negative for black
    private int Evaluate()
    {
//#if DEBUG
//        _evalCount++;
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

//#if DEBUG
//        public int Capacity => _capacity;
//        public int Count => _entryDict.Count;
//#endif


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

//#if DEBUG
//            public static int GetSize()
//            {
//                return System.Runtime.InteropServices.Marshal.SizeOf<Entry>();
//            }
//#endif
        }
    }

    public enum TableEvalType : byte
    {
        Exact,
        Alpha,
        Beta
    }
}