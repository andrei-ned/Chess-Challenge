﻿using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
#if DEBUG
using ChessChallenge.Application;
#endif


public class MyBot_v7_rng : IChessBot
{
    // { None, Pawn, Knight, Bishop, Rook, Queen, King}
    private int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private int[] _bonusPointsPerAttackEarly = { 0, 0, 4, 5, 1, 1, 0 };
    private int[] _bonusPointsPerAttackLate = { 0, 0, 2, 3, 5, 3, 1 };
    private TranspositionTable _transpositionTable = new TranspositionTable(16000000);
    private Board _board;
    private Move _bestMove;

    private bool IsLategame => _board.PlyCount > 30;

    private int[] _moveScores = new int[218]; // for sorting moves
    //private Dictionary<Move, int> _moveScoresDict = new Dictionary<Move, int>();

    private int _currentDepth;

    // Time management
    //private Timer _timer;
    //private int _maxMillis;
    //private bool ShouldCancel => _timer.MillisecondsElapsedThisTurn > _maxMillis;

    Random rng = new Random();

#if DEBUG
    private int _evalCount;
    private int _tableHitCount;
#endif

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        //_timer = timer;

        // Forced move, don't waste time searching
        Span<Move> legalMoves = stackalloc Move[218];
        _board.GetLegalMovesNonAlloc(ref legalMoves);
        if (legalMoves.Length == 1)
#if DEBUG
        {
            ConsoleHelper.Log($"Forced {legalMoves[0]}", false, ConsoleColor.Magenta);
#endif
            return legalMoves[0];
#if DEBUG
        }
#endif

#if DEBUG
        //_moveScoresDict.Clear();
        //ConsoleHelper.Log($"Entry size is {TranspositionTable.Entry.GetSize()} bytes", false);
        _evalCount = 0;
        _tableHitCount = 0;
        //int bestEval =
        int bestEval = 0;
#endif

        //_maxMillis = 100;
        int targetMillis = (Math.Min(1000, timer.MillisecondsRemaining / 40) + timer.IncrementMilliseconds) / 4;

        // Iterative deepening
        for (_currentDepth = 1; _currentDepth <= 256; _currentDepth++)
        {
            if (timer.MillisecondsElapsedThisTurn > targetMillis)
                break;
            //_moveScoresDict.Clear();
#if DEBUG
            bestEval =
#endif
                Search(_currentDepth, -1000000000, 1000000000, true);
        }

#if DEBUG
        //ConsoleHelper.Log($"Transposition hits: {_tableHitCount}, table has {_transpositionTable.Count} / {_transpositionTable.Capacity} entries", false, ConsoleColor.DarkRed);
        ConsoleHelper.Log($"Best move {_bestMove} with eval: {bestEval}. Evaluated {_evalCount} positions at depth {_currentDepth} in {timer.MillisecondsElapsedThisTurn} milliseconds.", false, ConsoleColor.Cyan);
        //List<KeyValuePair<Move, int>> kvps = _moveScoresDict.ToList();
        //kvps.Sort((a, b) => b.Value.CompareTo(a.Value));
        //foreach (var kvp in kvps)
        //{
        //    ConsoleHelper.Log($"{kvp.Key} was scored {kvp.Value}.");
        //}

        //var whiteQueen = _board.GetPieceList(PieceType.Queen, true)[0];
        //var whiteQueenBitboard = BitboardHelper.GetPieceAttacks(PieceType.Queen, whiteQueen.Square, _board, true);
        ////var whiteQueenBitboard = BitboardHelper.GetPieceAttacks(PieceType.Queen, whiteQueen.Square, _board, true);
        //BitboardHelper.VisualizeBitboard(whiteQueenBitboard);
#endif
        return _bestMove;
    }

    private int Search(int depth, int alpha, int beta, bool isRoot = false)
    {
        // First check if there's a checkmate
        if (_board.IsInCheckmate())
            return -100000 + 1000 * (_currentDepth - depth); // multiply by depth, the sooner the mate the better
        if (_board.IsDraw())
            return 0; // evaluate draw a little negative, better try than draw by repetition

        if (!isRoot && _transpositionTable.TryGetEvaluation(_board.ZobristKey, depth, alpha, beta, out int tableEval))
#if DEBUG
        {
            _tableHitCount++;
#endif
            return tableEval;
#if DEBUG
        }
#endif

        //if (ShouldCancel)
        //    return 0;

        if (depth == 0)
            return QSearch(alpha, beta);

        var evalType = TableEvalType.Alpha;
        Span<Move> legalMoves = stackalloc Move[218];
        _board.GetLegalMovesNonAlloc(ref legalMoves);

        OrderMoves(ref legalMoves, isRoot && depth > 1);
        foreach (Move move in legalMoves)
        {
            _board.MakeMove(move);
            int eval = -Search(depth - 1, -beta, -alpha);
            _board.UndoMove(move);

            if (eval >= beta)
            {
                _transpositionTable.StoreEvaluation(depth, eval, _board.ZobristKey, TableEvalType.Beta);
                return beta;
            }
            if (eval > alpha)
            {
                evalType = TableEvalType.Exact;
                alpha = eval;
                if (isRoot)
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
        OrderMoves(ref legalMoves, false);
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
        var bonusPointsPerAttack = IsLategame ? _bonusPointsPerAttackLate : _bonusPointsPerAttackEarly;
        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            if (pieceType == PieceType.None)
                continue;
            EvaluatePieces(pieceType, true);
            EvaluatePieces(pieceType, false);
            //var whitePieceList = _board.GetPieceList(pieceType, true);
            //var blackPieceList = _board.GetPieceList(pieceType, false);
            //evaluation += whitePieceList.Count * pieceValues[(int)pieceType];
            //evaluation -= blackPieceList.Count * pieceValues[(int)pieceType];
            //foreach (var piece in whitePieceList)
            //{
            //    var pieceBitboard = BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, _board, true);
            //    var attacks = BitboardHelper.GetNumberOfSetBits(pieceBitboard);
            //    evaluation += attacks;
            //}
            //foreach (var piece in blackPieceList)
            //{
            //    var pieceBitboard =  BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, _board, false);
            //    var attacks = BitboardHelper.GetNumberOfSetBits(pieceBitboard);
            //    evaluation -= attacks;
            //}
        }
        // Add a tiny bit of rng to eval, this way we can pick evaluated positions with same score
        evaluation += rng.Next(-1, 2);
        return evaluation;

        void EvaluatePieces(PieceType pieceType, bool isWhite)
        {
            int sign = isWhite ? 1 : -1;
            var pieceList = _board.GetPieceList(pieceType, isWhite);
            evaluation += pieceList.Count * _pieceValues[(int)pieceType] * sign;
            foreach (var piece in pieceList)
            {
                var pieceBitboard = BitboardHelper.GetPieceAttacks(pieceType, piece.Square, _board, isWhite);
                var attacks = BitboardHelper.GetNumberOfSetBits(pieceBitboard);
                evaluation += attacks * bonusPointsPerAttack[(int)pieceType] * sign;
            }
        }
    }

    private void OrderMoves(ref Span<Move> moves, bool useBestMove)
    {
        for (int i = 0; i < moves.Length; i++)
            _moveScores[i] = GuessMoveScore(moves[i]);

        _moveScores.AsSpan().Slice(0, moves.Length).Sort(moves, (a, b) => b.CompareTo(a));

        int GuessMoveScore(Move move)
        {
            if (useBestMove && move == _bestMove)
                return 10000000;
            int guess = 0;
            var movedPiece = move.MovePieceType;
            var capturedPiece = move.CapturePieceType;

            if (capturedPiece != PieceType.None)
                guess = 10 * _pieceValues[(int)capturedPiece] - _pieceValues[(int)movedPiece];

            if (move.IsPromotion)
                guess += _pieceValues[(int)move.PromotionPieceType];

            if (move.IsCastles)
                guess += 10000;

            return guess;
        }
    }

    public class TranspositionTable
    {
        private ulong _capacity;
        private Entry[] _entries;

        private ulong GetIndex(ulong zobristkey) => zobristkey % _capacity;

        public TranspositionTable(ulong capacity)
        {
            _capacity = capacity;
            _entries = new Entry[capacity];
        }

        public void StoreEvaluation(int depth, int eval, ulong zobristKey, TableEvalType nodeType)
        {
            _entries[GetIndex(zobristKey)] = new Entry(eval, (byte)depth, nodeType, zobristKey);
        }

        public bool TryGetEvaluation(ulong zobristKey, int depth, int alpha, int beta, out int eval)
        {
            Entry entry = _entries[GetIndex(zobristKey)];
            if (entry._zobristKey == zobristKey)
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

        public struct Entry
        {
            public readonly int _evalValue;
            public readonly byte _depth;
            public readonly TableEvalType _nodeType;
            public readonly ulong _zobristKey;

            public Entry(int eval, byte depth, TableEvalType nodeType, ulong zobristKey)
            {
                _evalValue = eval;
                _depth = depth;
                _nodeType = nodeType;
                _zobristKey = zobristKey;
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