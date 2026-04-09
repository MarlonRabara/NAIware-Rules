Feature: Mortgage Processing Rules
    As a mortgage processor
    I want to evaluate loan application rules
    So that I can determine borrower eligibility and required actions

Scenario: No borrowers on the loan application
    Given a loan application for property "123 Main St" "Springfield" "IL" "62704"
    And the loan application has no borrowers
    When I evaluate the borrower count rule
    Then the result message should be "Must have at least one borrower"

Scenario: Two borrowers both above the age of 62 qualify for reverse mortgage
    Given a loan application for property "456 Oak Ave" "Miami" "FL" "33101"
    And the loan application has a borrower "John" "Smith" born on "1955-03-15"
    And the loan application has a borrower "Jane" "Smith" born on "1958-07-22"
    When I evaluate the reverse mortgage eligibility rule
    Then the result message should be "You are eligible for a Reverse Mortgage!"
