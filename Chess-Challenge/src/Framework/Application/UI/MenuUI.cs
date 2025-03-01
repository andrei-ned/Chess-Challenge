﻿using Raylib_cs;
using System.Numerics;
using System;
using System.IO;
using ChessChallenge.Chess;
using System.Diagnostics;
using ChessChallenge.API;

namespace ChessChallenge.Application
{
    public static class MenuUI
    {
        private static ChallengeController.PlayerType lastSelectedBot = ChallengeController.PlayerType.MyBot;

        private static bool isBitboardVis = false;

        public static void DrawButtons(ChallengeController controller)
        {
            Vector2 buttonPos = UIHelper.Scale(new Vector2(135, 100));
            Vector2 buttonSize = UIHelper.Scale(new Vector2(255, 55));
            float spacing = buttonSize.Y * 1.2f;
            float breakSpacing = spacing * 0.6f;

            ChallengeController.PlayerType[] humanMatchups = {
                ChallengeController.PlayerType.MyBot,
                //ChallengeController.PlayerType.MyBot_v1,
                //ChallengeController.PlayerType.MyBot_v2,
                //ChallengeController.PlayerType.MyBot_v3,
                //ChallengeController.PlayerType.MyBot_v4,
                //ChallengeController.PlayerType.MyBot_v5,
                //ChallengeController.PlayerType.MyBot_v6,
                //ChallengeController.PlayerType.MyBot_v7,
                ChallengeController.PlayerType.MyBot_v8,
                ChallengeController.PlayerType.MyBot_v9,
                ChallengeController.PlayerType.MyBot_v10,
                ChallengeController.PlayerType.MyBot_v11,
                ChallengeController.PlayerType.MyBot_v12,
            };

            ChallengeController.PlayerType[] myBotMatchups = {
                ChallengeController.PlayerType.EvilBot,
                ChallengeController.PlayerType.MyBot,
                //ChallengeController.PlayerType.MyBot_v1,
                //ChallengeController.PlayerType.MyBot_v2,
                //ChallengeController.PlayerType.MyBot_v3,
                //ChallengeController.PlayerType.MyBot_v4,
                //ChallengeController.PlayerType.MyBot_v5,
                //ChallengeController.PlayerType.MyBot_v6,
                //ChallengeController.PlayerType.MyBot_v7,
                ChallengeController.PlayerType.MyBot_v8,
                ChallengeController.PlayerType.MyBot_v9,
                ChallengeController.PlayerType.MyBot_v10,
                ChallengeController.PlayerType.MyBot_v11,
                ChallengeController.PlayerType.MyBot_v12,
            };

            // Game Buttons
            foreach (var botPlayer in humanMatchups)
            {
                if (NextButtonInRow($"Human vs {botPlayer}", ref buttonPos, spacing, buttonSize))
                {
                    lastSelectedBot = botPlayer;
                    var whiteType = controller.HumanWasWhiteLastGame ? botPlayer : ChallengeController.PlayerType.Human;
                    var blackType = !controller.HumanWasWhiteLastGame ? botPlayer : ChallengeController.PlayerType.Human;
                    controller.StartNewGame(whiteType, blackType);
                }
            }

            foreach (var botPlayer in myBotMatchups)
            {
                if (NextButtonInRow($"MyBot vs {botPlayer}", ref buttonPos, spacing, buttonSize))
                {
                    controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, botPlayer);
                }
            }
            //if (NextButtonInRow("Human vs MyBot", ref buttonPos, spacing, buttonSize))
            //{
            //    var whiteType = controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
            //    var blackType = !controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
            //    controller.StartNewGame(whiteType, blackType);
            //}
            //if (NextButtonInRow("MyBot vs MyBot", ref buttonPos, spacing, buttonSize))
            //{
            //    controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.MyBot);
            //}
            //if (NextButtonInRow("MyBot vs EvilBot", ref buttonPos, spacing, buttonSize))
            //{
            //    controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.EvilBot);
            //}

            buttonPos = UIHelper.Scale(new Vector2(135 + 260, 100));

            // FEN
            if (NextButtonInRow("Load FEN as human", ref buttonPos, spacing, buttonSize))
            {
                CreateGameFromFENInput(true);
            }
            if (NextButtonInRow("Load FEN as bot", ref buttonPos, spacing, buttonSize))
            {
                CreateGameFromFENInput(false);
            }
            if (NextButtonInRow("Get FEN", ref buttonPos, spacing, buttonSize))
            {
                ConsoleHelper.Log(controller.GetFEN());
            }
#if DEBUG
            //if (NextButtonInRow("Evaluate FEN", ref buttonPos, spacing, buttonSize))
            //{
            //    var myBot = new MyBot();
            //    var myBotv11 = new MyBot_v11_isolated_pawns();
            //    string? fen = GetFENFromInput();

            //    Stopwatch stopwatch = new Stopwatch();
            //    if (fen != null)
            //    {
            //        //ConsoleHelper.Log("MyBot:");
            //        //myBot.DebugEvaluate(fen);
            //        //ConsoleHelper.Log("MyBot_v11:");
            //        //myBotv11.DebugEvaluate(fen);

            //        int eval = 0;
            //        stopwatch.Start();
            //        for (int i = 0; i < 1000000; i++)
            //        {
            //            eval = myBot.DebugEvaluate(fen, false);
            //        }
            //        stopwatch.Stop();
            //        ConsoleHelper.Log($"MyBot elapsed {stopwatch.Elapsed.TotalMilliseconds} ms and evaluated {eval}");

            //        stopwatch.Restart();
            //        for (int i = 0; i < 1000000; i++)
            //        {
            //            eval = myBotv11.DebugEvaluate(fen, false);
            //        }
            //        stopwatch.Stop();
            //        ConsoleHelper.Log($"MyBot v11 elapsed {stopwatch.Elapsed.TotalMilliseconds} ms and evaluate {eval}");
            //    }
            //}
            if (NextButtonInRow("Toggle bitboard vis", ref buttonPos, spacing, buttonSize))
            {
                HandleVisualizingBitboard();
            }
#endif

            // Page buttons
            buttonPos.Y += breakSpacing;

            if (NextButtonInRow("Save Games", ref buttonPos, spacing, buttonSize))
            {
                string pgns = controller.AllPGNs;
                string directoryPath = Path.Combine(FileHelper.AppDataPath, "Games");
                Directory.CreateDirectory(directoryPath);
                string fileName = FileHelper.GetUniqueFileName(directoryPath, "games", ".txt");
                string fullPath = Path.Combine(directoryPath, fileName);
                File.WriteAllText(fullPath, pgns);
                ConsoleHelper.Log("Saved games to " + fullPath, false, ConsoleColor.Blue);
            }
            if (NextButtonInRow("Rules & Help", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://github.com/SebLague/Chess-Challenge");
            }
            if (NextButtonInRow("Documentation", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://seblague.github.io/chess-coding-challenge/documentation/");
            }
            if (NextButtonInRow("Submission Page", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://forms.gle/6jjj8jxNQ5Ln53ie6");
            }

            // Window and quit buttons
            buttonPos.Y += breakSpacing;

            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            string windowButtonName = isBigWindow ? "Smaller Window" : "Bigger Window";
            if (NextButtonInRow(windowButtonName, ref buttonPos, spacing, buttonSize))
            {
                Program.SetWindowSize(isBigWindow ? Settings.ScreenSizeSmall : Settings.ScreenSizeBig);
            }
            if (NextButtonInRow("Exit (ESC)", ref buttonPos, spacing, buttonSize))
            {
                Environment.Exit(0);
            }

            bool NextButtonInRow(string name, ref Vector2 pos, float spacingY, Vector2 size)
            {
                bool pressed = UIHelper.Button(name, pos, size);
                pos.Y += spacingY;
                return pressed;
            }

            void CreateGameFromFENInput(bool humanToMove)
            {
                string? input = GetFENFromInput();
                if (input == null)
                {
                    return;
                }
                var pos = FenUtility.PositionFromFen(input);
                var whiteType = pos.whiteToMove == humanToMove ? ChallengeController.PlayerType.Human : lastSelectedBot;
                var blackType = pos.whiteToMove != humanToMove ? ChallengeController.PlayerType.Human : lastSelectedBot;
                controller.StartNewGame(whiteType, blackType);
                controller.SetBoardFromFEN(pos);
            }

            string? GetFENFromInput()
            {
                try
                {
                    string? input = Console.ReadLine();
                    if (input == null)
                    {
                        ConsoleHelper.Log("Input was null");
                    }
                    return input;
                }
                catch (Exception e)
                {
                    ConsoleHelper.Log($"Error creating board from FEN: {e.Message}", true);
                }
                return null;
            }
        }

        private static void HandleVisualizingBitboard()
        {
            // BITBOARD FOR PASSED PAWNS
            if (isBitboardVis)
            {
                BitboardHelper.StopVisualizingBitboard();
                isBitboardVis = false;
                return;
            }

            string input = Console.ReadLine() ?? "";
            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            var square = new Square(input);
            bool isWhite = true;
            ulong passedPawnMask = 0;
            BitboardHelper.SetSquare(ref passedPawnMask, square);
            if (square.File < 7)
                passedPawnMask |= passedPawnMask << 1;
            if (square.File > 0)
                passedPawnMask |= passedPawnMask >> 1;
            if (isWhite)
            {
                passedPawnMask <<= 8;
                passedPawnMask |= passedPawnMask << 8;
                passedPawnMask |= passedPawnMask << 16;
                passedPawnMask |= passedPawnMask << 32;
            }
            else
            {
                passedPawnMask >>= 8;
                passedPawnMask |= passedPawnMask >> 8;
                passedPawnMask |= passedPawnMask >> 16;
                passedPawnMask |= passedPawnMask >> 32;
            }
            isBitboardVis = true;
            BitboardHelper.VisualizeBitboard(passedPawnMask);
        }
    }
}