Feature: MathHelper
    As a developer
    I want to use MathHelper utility methods
    So that I can perform GCF, LCM, and rounding calculations

Scenario: GCF of two numbers with common factor
    Given two unsigned integers 12 and 8
    When I calculate the GCF
    Then the GCF result should be 4

Scenario: GCF of two coprime numbers
    Given two unsigned integers 7 and 13
    When I calculate the GCF
    Then the GCF result should be 1

Scenario: GCF where one divides the other
    Given two unsigned integers 5 and 25
    When I calculate the GCF
    Then the GCF result should be 5

Scenario: LCM of two numbers
    Given two unsigned integers 4 and 6
    When I calculate the LCM
    Then the LCM result should be 12

Scenario: LCM of identical numbers
    Given two unsigned integers 7 and 7
    When I calculate the LCM
    Then the LCM result should be 7

Scenario: RoundUp rounds to specified digits
    Given a double value 1.2345 and 2 digits
    When I round up
    Then the rounded result should be 1.24

Scenario: RoundUp with no fractional part
    Given a double value 5.0 and 0 digits
    When I round up
    Then the rounded result should be 5
