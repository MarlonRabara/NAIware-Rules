Feature: Document Analysis Test Rules
    As a developer
    I want to author complex rules for document analysis
    And hydrate sample json data from a file
    so that I can text various rule combinations

Scenario: Hydrate DocumentAnalysisPackage from sample file
	Given the document analysis model assembly
    And the reflected type "DocumentAnalysis.Model.DocumentAnalysisPackage"
    When I hydrate the reflected type
    Then the hydrated object should not be null
    And the hydrated object type should be "DocumentAnalysis.Model.DocumentAnalysisPackage"