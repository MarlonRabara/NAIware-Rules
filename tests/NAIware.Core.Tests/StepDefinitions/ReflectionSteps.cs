using System.Reflection;
using FluentAssertions;
using NAIware.Core.Reflection;
using Reqnroll;

namespace NAIware.Core.Tests.StepDefinitions;

[Binding]
public class ReflectionSteps
{
    private Assembly? _assembly;
    private Type? _type;
    private object? _hydratedObject;

    [Given(@"the mortgage model assembly")]
    public void GivenTheMortgageModelAssembly()
    {
        string mortgageModelAssemblyPath = Path.Combine(
                                            AppContext.BaseDirectory,
                                            "Mortgage.Model.dll");

        File.Exists(mortgageModelAssemblyPath).Should().BeTrue($"the test assembly should exist at {mortgageModelAssemblyPath}");
        _assembly = Assembly.LoadFrom(mortgageModelAssemblyPath);
    }

    [Given(@"the reflected type ""(.*)""")]
    public void GivenTheReflectedType(string typeName)
    {
        _assembly.Should().NotBeNull();
        _type = _assembly!.GetType(typeName, throwOnError: true);
    }

    [When(@"I hydrate the reflected type")]
    public void WhenIHydrateTheReflectedType()
    {
        _type.Should().NotBeNull();
        _hydratedObject = ObjectGraphHydrator.Create(_type!);
    }

    [Then(@"the hydrated object should not be null")]
    public void ThenTheHydratedObjectShouldNotBeNull()
    {
        _hydratedObject.Should().NotBeNull();
    }

    [Then(@"the hydrated object type should be ""(.*)""")]
    public void ThenTheHydratedObjectTypeShouldBe(string expectedTypeName)
    {
        _hydratedObject.Should().NotBeNull();
        _hydratedObject!.GetType().FullName.Should().Be(expectedTypeName);
    }
}
