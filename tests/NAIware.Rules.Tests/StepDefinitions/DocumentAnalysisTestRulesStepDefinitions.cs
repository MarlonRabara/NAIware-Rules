using FluentAssertions;
using NAIware.Core.Collections;
using NAIware.Core.Reflection;
using Reqnroll;
using System;
using System.Reflection;

namespace NAIware.Rules.Tests.StepDefinitions
{
    [Binding]
    public class DocumentAnalysisTestRulesStepDefinitions
    {
        private Assembly? _assembly;
        private Type? _type;
        private Tree<PropertyTreeNode, ReflectedPropertyNode>? _hydratedObject;

        [Given("the document analysis model assembly")]
        public void GivenTheDocumentAnalysisModelAssembly()
        {
            string assemblyPath = Path.Combine(
                                                AppContext.BaseDirectory,
                                                "DocumentAnalysis.Models.dll");

            File.Exists(assemblyPath).Should().BeTrue($"the test assembly should exist at {assemblyPath}");
            _assembly = Assembly.LoadFrom(assemblyPath);
        }

        [Given("the reflected type {string}")]
        public void GivenTheReflectedType(string typeName)
        {
            _type = _assembly?.GetType(typeName);
            _type.Should().NotBeNull($"the type {typeName} should exist in the assembly");
        }

        [When("I hydrate the reflected type")]
        public void WhenIHydrateTheReflectedType()
        {
            object anyObject = Activator.CreateInstance(_type!);
            _hydratedObject = ObjectTreeHydrator.Create(_type, anyObject, "docAnalysisTree");
        }

        [Then("the hydrated object should not be null")]
        public void ThenTheHydratedObjectShouldNotBeNull()
        {
            _hydratedObject.Should().NotBeNull("the hydrated object should not be null");
        }

        [Then("the hydrated object type should be {string}")]
        public void ThenTheHydratedObjectTypeShouldBe(string expectedTypeName)
        {
            _hydratedObject.Should().NotBeNull("the hydrated object should not be null");
            _hydratedObject!.Root.Value.Type.FullName.Should().Be(expectedTypeName, $"the hydrated object type should be {expectedTypeName}");
        }

    }
}
