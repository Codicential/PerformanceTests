using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static System.Console;
using static System.Environment;

namespace PerformanceTests
{
    public class Tester<TResult>
    {
        private readonly Test[] _tests;
        private readonly Action _beforeAction;
        private readonly Action<TResult> _afterAction;
        private readonly long _iterations;
        private readonly string _resultsFile;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private double _dummyTime;
        private double _dummyInitTime;
        private bool _isDummyDone;

        public abstract class Test
        {
            private Tester<TResult> _tester;

            public static Test CreateTest<TSeed>(string description, Func<TSeed> seedFunc, Func<TSeed, TResult> action) => new TestInternal<TSeed>(seedFunc, action)
            {
                Description = description
            };
            public static Test CreateTest(string description, Func<TResult> action) => new TestInternal(action)
            {
                Description = description
            };

            public string Description { get; private set; }

            public abstract void Run(Action<TResult> afterAction);

            public abstract void Initialize();

            private Test()
            {
            }

            private class TestInternal : Test
            {
                private readonly Func<TResult> _action;

                public TestInternal(Func<TResult> action)
                {
                    _action = action;
                }

                #region Overrides of Test

                public override void Run(Action<TResult> afterAction)
                {
                    var iterations = _tester._iterations;
                    for (var i = 0; i < iterations; i++)
                        afterAction(_action());
                }

                public override void Initialize() { }

                #endregion
            }

            private class TestInternal<TSeed> : Test
            {
                private TSeed _seed;
                private readonly Func<TSeed> _seedFunc;
                private readonly Func<TSeed, TResult> _action;

                public TestInternal(Func<TSeed> seedFunc, Func<TSeed, TResult> action)
                {
                    _seedFunc = seedFunc;
                    _action = action;
                }

                #region Overrides of Test

                public override void Run(Action<TResult> afterAction)
                {
                    var iterations = _tester._iterations;
                    for (var i = 0; i < iterations; i++)
                        afterAction(_action(_seed));
                }

                public override void Initialize() => _seed = _seedFunc();

                #endregion
            }

            public void SetHost(Tester<TResult> tester)
            {
                _tester = tester;
            }
        }
        public static Test Create<TSeed>(string description, Func<TSeed> seedFunc, Func<TSeed, TResult> action) => Test.CreateTest(description, seedFunc, action);
        public static Test Create(string description, Func<TResult> action) => Test.CreateTest(description, action);

        public Tester(IEnumerable<Test> tests, Action beforeAction = null, Action<TResult> afterAction = null, long iterations = 1000000, string resultsFile = "results.csv")
        {
            _tests = tests.ToArray();
            _beforeAction = beforeAction ?? (() => {});
            _afterAction = afterAction ?? (result => { });
            _iterations = iterations;
            _resultsFile = resultsFile;

            foreach (var test in _tests)
                test.SetHost(this);
        }

        public void Run()
        {
            WriteLine($"Starting tests for {_iterations} iterations...");
            File.Delete(_resultsFile);

            foreach (var test in _tests)
                RunTest(test);

            WriteLine($"{NewLine}Testing is finished.");
        }

        private void RunTest(Test test)
        {
            //WriteLine("--cleaning...");
            _beforeAction();

            WriteLine($"{NewLine}\tStaring test '{test.Description}'...");

            _stopwatch.Restart();
            test.Initialize();
            _stopwatch.Stop();
            var initTime = Math.Max(_stopwatch.Elapsed.TotalSeconds - _dummyInitTime, 0d);

            _stopwatch.Restart();
            test.Run(_afterAction);
            _stopwatch.Stop();

            var totalSeconds = Math.Max(_stopwatch.Elapsed.TotalSeconds - _dummyTime, 0d);

            WriteLine($"\tTest done in {totalSeconds:0.00000} sec. with init time: {initTime:0.#####} sec.");
            File.AppendAllLines(_resultsFile, new [] { $"{test.Description};{totalSeconds};{initTime}" });

            if (!_isDummyDone)
            {
                _dummyTime = _stopwatch.Elapsed.TotalSeconds;
                _dummyInitTime = initTime;
                _isDummyDone = true;
            }

            Thread.Sleep(250);
        }
    }
}