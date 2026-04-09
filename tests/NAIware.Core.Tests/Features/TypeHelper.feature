Feature: TypeHelper
    As a developer
    I want to use TypeHelper utility methods
    So that I can perform common type conversions and checks

Scenario: Convert integer to decimal
    Given a value of 42 as an object
    When I convert it to decimal
    Then the result should be 42

Scenario: Convert string to integer
    Given a string value "123"
    When I convert it to integer
    Then the integer result should be 123

Scenario: Check if nullable type is nullable
    Given a nullable int type
    When I check if the type is nullable
    Then the result should be true

Scenario: Check if regular type is nullable
    Given a regular int type
    When I check if the type is nullable
    Then the result should be false

Scenario: Get underlying type from nullable
    Given a nullable int type
    When I get the underlying type from nullable
    Then the underlying type should be System.Int32

Scenario: IsEmpty returns true for null
    Given a null value
    When I check if the value is empty
    Then the result should be true

Scenario: IsEmpty returns true for empty string
    Given a string value ""
    When I check if the value is empty
    Then the result should be true

Scenario: IsEmpty returns false for non-empty value
    Given a string value "hello"
    When I check if the value is empty
    Then the result should be false

Scenario: Coalesce returns first non-null value
    Given a null value and a value of 5
    When I coalesce the values
    Then the coalesced result should be 5
