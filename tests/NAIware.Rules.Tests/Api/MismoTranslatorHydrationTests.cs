using System.Reflection;
using FluentAssertions;
using Xunit;

namespace NAIware.Rules.Tests.Api;

/// <summary>
/// Regression guard for the Rule Editor's "Load &amp; Run" test-data hydration path.
/// </summary>
/// <remarks>
/// <para>
/// The <c>sample-loan-application.xml</c> file is a MISMO document and cannot be deserialized
/// directly into <c>Mortgage.Model.Loans.LoanApplication</c> by the built-in
/// <see cref="System.Xml.Serialization.XmlSerializer"/> — doing so fails with
/// "There is an error in XML document (2,2)." The editor must instead route the file through the
/// context's configured serializer/translator (<c>Mortgage.Model.Translators.MISMO</c>), which
/// exposes <c>Deserialize(string filePath)</c>.
/// </para>
/// <para>
/// This test exercises that translator path against the real resource assemblies — mirroring
/// <c>MainForm.DeserializeContextData</c> — so the regression cannot silently return. It avoids
/// referencing the WinForms editor assembly directly to keep the test host UI-free.
/// </para>
/// </remarks>
public sealed class MismoTranslatorHydrationTests
{
    private const string ModelTypeName =
        "Mortgage.Model.Loans.LoanApplication, Mortgage.Model, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    private const string TranslatorTypeName =
        "Mortgage.Model.Translators.MISMO.MortgageFileMismoTranslator, Mortgage.Model.Translators.MISMO, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    [Fact]
    public void Mismo_sample_hydrates_into_loan_application_via_translator()
    {
        // Arrange: load the model and translator assemblies exactly as the editor does on demand.
        Assembly modelAssembly = Assembly.LoadFrom(TestResources.Path("Mortgage.Model.dll"));
        Assembly translatorAssembly = Assembly.LoadFrom(TestResources.Path("Mortgage.Model.Translators.MISMO.dll"));

        Type modelType = ResolveType(modelAssembly, ModelTypeName);
        Type translatorType = ResolveType(translatorAssembly, TranslatorTypeName);

        MethodInfo deserialize = FindDeserializeMethod(translatorType);
        string samplePath = TestResources.Path("sample-loan-application.xml");

        // Act: invoke the translator's Deserialize(string filePath) — the editor's hydration path.
        object? translator = deserialize.IsStatic ? null : Activator.CreateInstance(translatorType);
        object? result = deserialize.Invoke(translator, [samplePath]);

        // Assert: a non-null model instance assignable to the configured context type.
        result.Should().NotBeNull("the MISMO translator must hydrate the sample document");
        modelType.IsInstanceOfType(result).Should().BeTrue(
            "the translator must return an object assignable to '{0}', but returned '{1}'",
            modelType.FullName, result!.GetType().FullName);
    }

    [Fact]
    public void Built_in_xml_deserialization_fails_for_mismo_sample()
    {
        // Documents the underlying cause: the built-in XmlSerializer cannot read the MISMO root,
        // which is why the translator path is required. Locks in the rationale for the fix.
        Assembly modelAssembly = Assembly.LoadFrom(TestResources.Path("Mortgage.Model.dll"));
        Type modelType = ResolveType(modelAssembly, ModelTypeName);

        using FileStream stream = File.OpenRead(TestResources.Path("sample-loan-application.xml"));
        Action act = () => new System.Xml.Serialization.XmlSerializer(modelType).Deserialize(stream);

        act.Should().Throw<InvalidOperationException>(
            "the MISMO document does not map onto the model type via the built-in serializer");
    }

    private static Type ResolveType(Assembly assembly, string typeName) =>
        assembly.GetType(typeName)
        ?? assembly.GetTypes().FirstOrDefault(t =>
            string.Equals(t.AssemblyQualifiedName, typeName, StringComparison.Ordinal)
            || string.Equals(t.FullName, typeName, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Type '{typeName}' could not be resolved.");

    private static MethodInfo FindDeserializeMethod(Type translatorType) =>
        translatorType
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(method =>
                string.Equals(method.Name, "Deserialize", StringComparison.Ordinal)
                && method.GetParameters() is [{ ParameterType: var p }]
                && p == typeof(string)
                && method.ReturnType != typeof(void))
        ?? throw new InvalidOperationException(
            $"Translator '{translatorType.FullName}' must expose Deserialize(string filePath).");
}
