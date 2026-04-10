Feature: Rule Processor
    As a developer
    I want to evaluate catalog-defined rules against domain objects
    So that I can get structured results with optional diagnostics

Scenario: Processor resolves context and evaluates matching rule
    Given a rules library with a LoanApplication context
    And the context has an expression "NoBorrowers" with rule "BorrowerCount = 0" and result code "BORR-001" and message "Must have at least one borrower"
    And the expression is in category "Validation"
    When I evaluate a LoanApplication with no borrowers against category "Validation"
    Then the evaluation result should have 1 match
    And the first match should have code "BORR-001" and message "Must have at least one borrower"

Scenario: Processor returns mismatch when rule does not fire
    Given a rules library with a LoanApplication context
    And the context has an expression "NoBorrowers" with rule "BorrowerCount = 0" and result code "BORR-001" and message "Must have at least one borrower"
    And the expression is in category "Validation"
    When I evaluate a LoanApplication with 2 borrowers against category "Validation"
    Then the evaluation result should have 0 matches
    And the evaluation result should have 1 mismatch

Scenario: Mismatch diagnostics include parameter values when requested
    Given a rules library with a LoanApplication context
    And the context has an expression "NoBorrowers" with rule "BorrowerCount = 0" and result code "BORR-001" and message "Must have at least one borrower"
    And the expression is in category "Validation"
    When I evaluate a LoanApplication with 2 borrowers against category "Validation" with diagnostics
    Then the evaluation result should have 1 mismatch
    And the first mismatch diagnostic should contain parameter "BorrowerCount"

Scenario: Processor evaluates all active expressions when no category specified
    Given a rules library with a LoanApplication context
    And the context has an expression "NoBorrowers" with rule "BorrowerCount = 0" and result code "BORR-001" and message "Must have at least one borrower"
    And the context has an expression "HasBorrowers" with rule "BorrowerCount > 0" and result code "BORR-002" and message "Borrowers present"
    When I evaluate a LoanApplication with 2 borrowers without a category
    Then the evaluation result should have 1 match
    And the first match should have code "BORR-002" and message "Borrowers present"
    And the evaluation result should have 1 mismatch

Scenario: Expression versioning preserves history
    Given a rules library with a LoanApplication context
    And the context has an expression "NoBorrowers" with rule "BorrowerCount = 0" and result code "BORR-001" and message "Must have at least one borrower"
    When I revise expression "NoBorrowers" to "BorrowerCount < 1" with note "Use less-than for clarity"
    Then expression "NoBorrowers" should be at version 2
    And expression "NoBorrowers" should have 2 version history entries

Scenario: Inactive expressions are skipped during evaluation
    Given a rules library with a LoanApplication context
    And the context has an expression "NoBorrowers" with rule "BorrowerCount = 0" and result code "BORR-001" and message "Must have at least one borrower"
    And expression "NoBorrowers" is deactivated
    When I evaluate a LoanApplication with no borrowers without a category
    Then the evaluation result should have 0 matches
    And the evaluation result should have 0 mismatches

Scenario: Processor throws when context cannot be resolved
    Given a rules library with a LoanApplication context
    When I evaluate an unregistered object type
    Then a context resolution error should be raised
