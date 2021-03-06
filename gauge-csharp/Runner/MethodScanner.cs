using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gauge.CSharp.Lib;
using Gauge.CSharp.Lib.Attribute;

namespace Gauge.CSharp.Runner
{
    public class MethodScanner : IMethodScanner
    {
        private readonly GaugeApiConnection _apiConnection;
        public MethodScanner(GaugeApiConnection apiConnection)
        {
            _apiConnection = apiConnection;
        }

        public IStepRegistry GetStepRegistry()
        {
            return new StepRegistry(GetStepMethods());
        }

        private IEnumerable<KeyValuePair<string, MethodInfo>> GetStepMethods()
        {
            var stepMethods = GetAllMethodsForSpecAssemblies().Where(info => info.GetCustomAttributes().OfType<Step>().Any());
            foreach (var stepMethod in stepMethods)
            {
                var stepValues = _apiConnection.GetStepValue(stepMethod.GetCustomAttribute<Step>().Names, false);
                foreach (var stepValue in stepValues)
                {
                    yield return new KeyValuePair<string, MethodInfo>(stepValue, stepMethod);
                }
            }
        }

        public IHookRegistry GetHookRegistry()
        {
            var allMethods = GetAllMethodsForSpecAssemblies();
            var hookRegistry = new HookRegistry();
            var methodInfos = allMethods as IList<MethodInfo> ?? allMethods.ToList();
            hookRegistry.AddBeforeSuiteHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<BeforeSuite>().Any()));
            hookRegistry.AddAfterSuiteHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<AfterSuite>().Any()));
            hookRegistry.AddBeforeSpecHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<BeforeSpec>().Any()));
            hookRegistry.AddAfterSpecHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<AfterSpec>().Any()));
            hookRegistry.AddBeforeScenarioHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<BeforeScenario>().Any()));
            hookRegistry.AddAfterScenarioHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<AfterScenario>().Any()));
            hookRegistry.AddBeforeStepHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<BeforeStep>().Any()));
            hookRegistry.AddAfterStepHooks(methodInfos.Where(info => info.GetCustomAttributes().OfType<AfterStep>().Any()));
            return hookRegistry;
        }

        private IEnumerable<MethodInfo> GetAllMethodsForSpecAssemblies()
        {
            var enumerateFiles = GetAllAssemblyFiles();
            return enumerateFiles.SelectMany(GetMethodsFromAssembly);
        }

        private IEnumerable<MethodInfo> GetMethodsFromAssembly(string specAssembly)
        {
            var assembly = Assembly.LoadFile(specAssembly);
            return assembly.GetTypes().SelectMany(type => type.GetMethods());
        }

        private static IEnumerable<string> GetAllAssemblyFiles()
        {
            return Directory.EnumerateFiles(Utils.GaugeBinDir, "*.dll", SearchOption.AllDirectories);
        }
    }
}