Feature: StringHelper
    As a developer
    I want to use StringHelper utility methods
    So that I can validate and manipulate strings

Scenario: Valid variable name is recognized
    Given a variable name "myVariable"
    When I check if the variable name is valid
    Then the validity result should be true

Scenario: Invalid variable name starting with number is rejected
    Given a variable name "1invalid"
    When I check if the variable name is valid
    Then the validity result should be false

Scenario: Boolean keyword is not a valid variable
    Given a variable name "true"
    When I check if the variable name is valid
    Then the validity result should be false

Scenario: Valid variable name with underscores and dollars
    Given a variable name "_myVar$1"
    When I check if the variable name is valid
    Then the validity result should be true

Scenario: ToSafeUrlString escapes special characters
    Given an input string "it's a test & more"
    When I convert it to a safe URL string
    Then the safe URL string should be "it%27s a test %26 more"

Scenario: FromSafeUrlString restores special characters
    Given an input string "it%27s a test %26 more"
    When I convert it from a safe URL string
    Then the restored string should be "it's a test & more"
