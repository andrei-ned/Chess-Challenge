using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
#if DEBUG
using ChessChallenge.Application;
#endif

// Brain Bot Capacity: 978
// Fixed Q-Search: 958
// With no Q-Search: 845


// TODO
// Fix moves not being evaluated as expected
// 8/8/4k3/8/3K4/8/3R4/8 w - - 0 1
// white doesn't see when king can take rook
// is ok in v4
// differences from v4:
// tt table
// time management
// forced move
// move ordering
// qsearch doesn't eval checkmates and draws

public class MyBot_v5_1_backup : IChessBot
{
    // { None, Pawn, Knight, Bishop, Rook, Queen, King}
    private int[] _pieceValues = { 0, 100, 320, 330, 500, 900, 10000 };
    private int[] _bonusPointsPerAttackEarly = { 0, 0, 4, 5, 1, 1, 0 };
    private int[] _bonusPointsPerAttackLate = { 0, 0, 2, 3, 5, 3, 1 };

    private TranspositionTable _transpositionTable = new TranspositionTable(32000);
    private Board _board;
    private Move _bestMove;
    private int _currentDepth;

    private bool IsEndgame => _board.PlyCount > 40;

    private int[] _moveScores = new int[218]; // for sorting moves
    private Dictionary<Move, int> _moveScoresDict = new Dictionary<Move, int>();

#if DEBUG
    private int _evalCount;
    private int _tableHitCount;
#endif

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        //_timer = timer;
#if DEBUG
        _moveScoresDict.Clear();
        //ConsoleHelper.Log($"Entry size is {TranspositionTable.Entry.GetSize()} bytes", false);
        _evalCount = 0;
        _tableHitCount = 0;
        int bestEval = 0;
#endif

        // Timing
        int targetMillis = (Math.Min(1000, timer.MillisecondsRemaining / 40) + timer.IncrementMilliseconds) / 4;

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

        // Iterative deepening
        _currentDepth = 1;
        for (; _currentDepth <= 256; _currentDepth++)
        {
            if (timer.MillisecondsElapsedThisTurn > targetMillis && _currentDepth > 3)
                break;

#if DEBUG
            ConsoleHelper.Log($"Searching at depth {_currentDepth}", false);
            bestEval = 
#endif
            Search(_currentDepth, -1000000000, 1000000000, true);

    //        ConsoleHelper.Log(
    //$"Best move {_bestMove} with eval: {bestEval}. Evaluated {_evalCount} positions at depth {_currentDepth} in {timer.MillisecondsElapsedThisTurn} " +
    //$"(/{targetMillis}) milliseconds.", false, ConsoleColor.White);
    //        List<KeyValuePair<Move, int>> asdfs = _moveScoresDict.ToList();
    //        asdfs.Sort((a, b) => b.Value.CompareTo(a.Value));
    //        foreach (var kvp in asdfs)
    //        {
    //            ConsoleHelper.Log($"{kvp.Key} was scored {kvp.Value}.");
    //        }
        }

#if DEBUG
        //ConsoleHelper.Log($"Transposition hits: {_tableHitCount}, table has {_transpositionTable.Count} / {_transpositionTable.Capacity} entries", false, ConsoleColor.DarkRed);
        ConsoleHelper.Log(
            $"Best move {_bestMove} with eval: {bestEval}. Evaluated {_evalCount} positions at depth {_currentDepth} in {timer.MillisecondsElapsedThisTurn} " +
            $"(/{targetMillis}) milliseconds.", false, ConsoleColor.DarkBlue);
        List<KeyValuePair<Move, int>> kvps = _moveScoresDict.ToList();
        kvps.Sort((a, b) => b.Value.CompareTo(a.Value));
        foreach (var kvp in kvps)
        {
            //ConsoleHelper.Log($"{kvp.Key} was scored {kvp.Value}.");
        }

        //var whiteQueen = _board.GetPieceList(PieceType.Queen, true)[0];
        //var whiteQueenBitboard = BitboardHelper.GetPieceAttacks(PieceType.Queen, whiteQueen.Square, _board, true);
        ////var whiteQueenBitboard = BitboardHelper.GetPieceAttacks(PieceType.Queen, whiteQueen.Square, _board, true);
        //BitboardHelper.VisualizeBitboard(whiteQueenBitboard);

        //ulong bitboard = 0;
        //foreach (var pieceList in _board.GetAllPieceLists())
        //{
        //    if (!pieceList.IsWhitePieceList)
        //        continue;
        //    foreach (var piece in pieceList)
        //    {
        //        bitboard |= BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, _board, true);
        //    }
        //}
        //var blackKing = _board.GetPieceList(PieceType.King, false)[0];
        //ulong kingBitboard = BitboardHelper.GetPieceAttacks(PieceType.King, blackKing.Square, _board, true);

        //ulong testBitboard = kingBitboard & bitboard;
        //ulong test2 = kingBitboard ^ testBitboard;
        //BitboardHelper.VisualizeBitboard(test2);
#endif
        // TODO
        // use rng to pick one of the best moves
        return _bestMove;
    }

    // TODO search extensions
    private int Search(int depth, int alpha, int beta, bool isRoot = false)
    {
        // First check if there's a checkmate
        // TODO: fix mate in x, this is not good for transposition table
        if (_board.IsInCheckmate())
            return -100000 + 1000 * (_currentDepth - depth); // the sooner the mate the better
        if (_board.IsDraw())
            return 0;

//        if (!isRoot && _transpositionTable.TryGetEvaluation(_board.ZobristKey, depth, alpha, beta, out int tableEval))
//#if DEBUG
//        {
//            _tableHitCount++;
//#endif
//            return tableEval;
//#if DEBUG
//        }
//#endif

        //if (ShouldCancel)
        //    return 0;

        if (depth == 0)
            return QSearch(alpha, beta);

        var evalType = TableEvalType.Alpha;
        Span<Move> legalMoves = stackalloc Move[218];
        _board.GetLegalMovesNonAlloc(ref legalMoves);
        OrderMoves(ref legalMoves, isRoot && depth > 1 && _moveScoresDict.Count > 0);
        if (isRoot)
            _moveScoresDict.Clear();
        foreach (Move move in legalMoves)
        {
            _board.MakeMove(move);
            int eval = -Search(depth - 1, -beta, -alpha);
            _board.UndoMove(move);
            if (isRoot)
                _moveScoresDict[move] = eval;
#if DEBUG
            if (isRoot)
                ConsoleHelper.Log($"{move} was scored {eval}.");
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
                if (isRoot)
                    _bestMove = move;
            }
        }

        _transpositionTable.StoreEvaluation(depth, alpha, _board.ZobristKey, evalType);
        return alpha;
    }

    // TODO: collapse QSearch with Search to try get more brain capacity
    // Search only captures
    private int QSearch(int alpha, int beta)
    {
        //if (_board.IsInCheckmate())
        //    return -100000;
        //if (_board.IsDraw())
        //    return 0;

        int eval = Evaluate() * (_board.IsWhiteToMove ? 1 : -1);
        if (eval >= beta)
            return beta;
        if (alpha < eval)
            alpha = eval;

        Span<Move> legalMoves = stackalloc Move[256];
        _board.GetLegalMovesNonAlloc(ref legalMoves, true);
        //OrderMoves(ref legalMoves, false);
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
        var bonusPointsPerAttack = IsEndgame ? _bonusPointsPerAttackLate : _bonusPointsPerAttackEarly;
        ulong allWhiteAttacks = 0;
        ulong allBlackAttacks = 0;
        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            if (pieceType == PieceType.None)
                continue;
            allWhiteAttacks |= EvaluatePieces(pieceType, true);
            allBlackAttacks |= EvaluatePieces(pieceType, false);
        }
        evaluation += EvaluateKingEndgame(allWhiteAttacks, false);
        evaluation -= EvaluateKingEndgame(allBlackAttacks, true);


        return evaluation;

        // TODO
        // endgames
        // pawn structure
        // castles
        ulong EvaluatePieces(PieceType pieceType, bool isWhite)
        {
            int sign = isWhite ? 1 : -1;
            var pieceList = _board.GetPieceList(pieceType, isWhite);
            evaluation += pieceList.Count * _pieceValues[(int)pieceType] * sign;
            ulong allAttacksBitboard = 0;
            foreach (var piece in pieceList)
            {
                var pieceBitboard = BitboardHelper.GetPieceAttacks(pieceType, piece.Square, _board, isWhite);
                var attacks = BitboardHelper.GetNumberOfSetBits(pieceBitboard);
                allAttacksBitboard |= pieceBitboard;
                evaluation += attacks * bonusPointsPerAttack[(int)pieceType] * sign;
            }
            return allAttacksBitboard;
            //var enemyKingBitboard = BitboardHelper.GetKingAttacks(_board.GetKingSquare(!isWhite));
            //var attackIntersection = enemyKingBitboard & allAttacksBitboard;
            //var availableKingSquares = attackIntersection ^ enemyKingBitboard;
            //var noOfBits = BitboardHelper.GetNumberOfSetBits(availableKingSquares);
            //ConsoleHelper.Log(noOfBits.ToString());
            //evaluation -= BitboardHelper.GetNumberOfSetBits(availableKingSquares) * 10;

            //if (IsEndgame)
            //{
            //    var king = _board.GetPieceList(PieceType.King, isWhite).GetPiece(0);
            //    var x = king.Square.File - 3;
            //    var y = king.Square.Rank - 3;
            //    var distToCenterSquared = x * x + y * y;

            //    evaluation -= distToCenterSquared;

            //    var enemyKing = _board.GetPieceList(PieceType.King, !isWhite).GetPiece(0);
            //    evaluation -= Math.Abs(enemyKing.Square.File - king.Square.File) * 5;
            //    evaluation -= Math.Abs(enemyKing.Square.Rank - king.Square.Rank) * 5;
            //}
        }

        int EvaluateKingEndgame(ulong opponentAttacks, bool isWhite)
        {
            var myKingAttacks = BitboardHelper.GetKingAttacks(_board.GetKingSquare(isWhite));
            var attackIntersection = myKingAttacks & opponentAttacks;
            var availableKingSquares = attackIntersection ^ myKingAttacks;
            var noOfBits = BitboardHelper.GetNumberOfSetBits(availableKingSquares);
            return BitboardHelper.GetNumberOfSetBits(availableKingSquares) * 10;
        }
    }

    // TODO
    // killer moves
    private void OrderMoves(ref Span<Move> moves, bool orderByLastIterationScores)
    {
        for (int i = 0; i < moves.Length; i++)
            _moveScores[i] = orderByLastIterationScores ? GetScoreFromLastIteration(moves[i]) : GuessMoveScore(moves[i]);

        _moveScores.AsSpan().Slice(0, moves.Length).Sort(moves, (a, b) => b.CompareTo(a));

        int GetScoreFromLastIteration(Move move)
        {
            return _moveScoresDict[move];
        }

        int GuessMoveScore(Move move)
        {
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