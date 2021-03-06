﻿using System;
using System.Threading;
using Robust.Client.Interfaces;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client
{
    internal partial class GameController
    {
        private IGameLoop _mainLoop = default!;

        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private static bool _hasStarted;

        public static void Main(string[] args)
        {
            Start(args);
        }

        public static void Start(string[] args, bool contentStart = false)
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("Cannot start twice!");
            }

            _hasStarted = true;

            if (CommandLineArgs.TryParse(args, out var parsed))
            {
                ParsedMain(parsed, contentStart);
            }
        }

        private static void ParsedMain(CommandLineArgs args, bool contentStart)
        {
            IoCManager.InitThread();

            var mode = args.Headless ? DisplayMode.Headless : DisplayMode.Clyde;

            InitIoC(mode);

            var gc = (GameController) IoCManager.Resolve<IGameController>();
            gc.SetCommandLineArgs(args);

            // When the game is ran with the startup executable being content,
            // we have to disable the separate load context.
            // Otherwise the content assemblies will be loaded twice which causes *many* fun bugs.
            gc._disableAssemblyLoadContext = contentStart;
            if (!gc.Startup())
            {
                Logger.Fatal("Failed to start game controller!");
                return;
            }
            gc.MainLoop(mode);

            Logger.Debug("Goodbye");
            IoCManager.Clear();
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            _mainLoop = gameLoop;
        }

        public void MainLoop(DisplayMode mode)
        {
            if (_mainLoop == null)
            {
                _mainLoop = new GameLoop(_gameTiming)
                {
                    SleepMode = mode == DisplayMode.Headless ? SleepMode.Delay : SleepMode.None
                };
            }

            _mainLoop.Tick += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    Update(args);
                }
            };

            _mainLoop.Render += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _gameTiming.CurFrame++;
                    _clyde.Render();
                }
            };
            _mainLoop.Input += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _clyde.ProcessInput(args);
                }
            };

            _mainLoop.Update += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _frameProcessMain(args);
                }
            };

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
        }
    }
}
