using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using static PerformanceTests.Tester<PerformanceTests.TestClass>;

namespace PerformanceTests
{
    class TestClass
    {
        private int _val;
        public TestClass()
        {
            _val = 42;
        }
    }

    class Program
    {
        private const int Iterations = 10000000;

        static void Main(string[] args)
        {
            var values = new TestClass[Iterations];
            var index = 0;

            var testClassObj = new TestClass();

            var tester = new Tester<TestClass>(
                new[]
                {
                    Create("Empty (dummy) code", () => testClassObj),

                    Create("Simple constructor call", () => new TestClass()),

                    Create("Create with delegate", () => new Func<TestClass>(() => new TestClass()), action => action()),

                    Create("Create with Activator", Activator.CreateInstance<TestClass>),

                    Create("Create with raw reflection", () => (TestClass) typeof(TestClass).GetConstructor(Type.EmptyTypes).Invoke(null)),

                    Create("Create with cached reflection", () => typeof(TestClass).GetConstructor(Type.EmptyTypes), info => (TestClass) info.Invoke(null)),

                    //Create("Create with raw Expression", () => Expression.Lambda<Action>(Expression.New(typeof(TestClass))).Compile()()),

                    Create("Create with compiled Expression (Func)", () => Expression.Lambda<Func<TestClass>>(Expression.New(typeof(TestClass))).Compile(), action => action()),

                },
                () =>
                {
                    index = 0;
                    values = new TestClass[Iterations];
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    GC.WaitForPendingFinalizers();
                },
                result => values[index++] = result,
                Iterations);

            Console.ReadKey();
            Thread.Sleep(500);

            tester.Run();

            Console.ReadKey();
        }
    }
}
