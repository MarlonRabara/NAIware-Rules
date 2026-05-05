Feature: Reflection
    As a developer
    I want to hydrate object graphs from reflected types
    So that model objects can be explored without compile-time references

Scenario: Hydrate LoanApplication from mortgage model assembly
    Given the mortgage model assembly
    And the reflected type "Mortgage.Model.Loans.LoanApplication"
    When I hydrate the reflected type
    Then the hydrated object should not be null
    And the hydrated object type should be "Mortgage.Model.Loans.LoanApplication"
