using System;
using System.Reflection;
using NUnit.Framework;

namespace NWR.Tests
{
    public static class TestProgram
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0) {
                int scenarioResult = ScenarioRunner.Run(args);
                if (scenarioResult >= 0) {
                    return scenarioResult;
                }
            }

            int failures = 0;
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
                if (type.GetCustomAttributes(typeof(TestFixtureAttribute), false).Length == 0) {
                    continue;
                }
                object instance;
                try {
                    instance = Activator.CreateInstance(type);
                } catch (Exception ex) {
                    Console.WriteLine("FAIL fixture " + type.Name + ": " + ex.Message);
                    failures++;
                    continue;
                }

                foreach (MethodInfo method in type.GetMethods()) {
                    if (method.GetCustomAttributes(typeof(TestAttribute), false).Length == 0) {
                        continue;
                    }
                    try {
                        method.Invoke(instance, null);
                        Console.WriteLine("OK  " + type.Name + "." + method.Name);
                    } catch (TargetInvocationException tie) {
                        Exception inner = tie.InnerException ?? tie;
                        Console.WriteLine("FAIL " + type.Name + "." + method.Name + ": " + inner.Message);
                        failures++;
                    } catch (Exception ex) {
                        Console.WriteLine("FAIL " + type.Name + "." + method.Name + ": " + ex.Message);
                        failures++;
                    }
                }
            }

            if (failures == 0) {
                Console.WriteLine("NWR.Tests: all passed");
                return 0;
            }
            Console.WriteLine("NWR.Tests: " + failures + " failure(s)");
            return 1;
        }
    }
}
