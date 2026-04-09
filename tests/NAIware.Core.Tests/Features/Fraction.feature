Feature: Fraction
    As a developer
    I want to use the Fraction class for precise arithmetic
    So that I can perform fraction math with correct results

Scenario: Create fraction from numerator and denominator
    Given a fraction with numerator 3 and denominator 4
    Then the fraction value should be 0.75
    And the fraction string should be "3/4"

Scenario: Create fraction from decimal value
    Given a fraction from decimal 0.5
    Then the fraction numerator should be 1
    And the fraction denominator should be 2

Scenario: Reduce a fraction
    Given a fraction with numerator 6 and denominator 8
    When I reduce the fraction
    Then the fraction numerator should be 3
    And the fraction denominator should be 4

Scenario: Add two fractions
    Given a fraction with numerator 1 and denominator 4
    And a second fraction with numerator 1 and denominator 4
    When I add the fractions
    Then the fraction value should be 0.5

Scenario: Subtract two fractions
    Given a fraction with numerator 3 and denominator 4
    And a second fraction with numerator 1 and denominator 4
    When I subtract the fractions
    Then the fraction value should be 0.5

Scenario: Multiply two fractions
    Given a fraction with numerator 2 and denominator 3
    And a second fraction with numerator 3 and denominator 4
    When I multiply the fractions
    Then the fraction value should be 0.5

Scenario: Divide two fractions
    Given a fraction with numerator 1 and denominator 2
    And a second fraction with numerator 1 and denominator 4
    When I divide the fractions
    Then the fraction value should be 2

Scenario: Create negative fraction
    Given a fraction with numerator 3 and denominator 4 that is negative
    Then the fraction value should be -0.75
    And the fraction string should be "-3/4"

Scenario: Clone a fraction
    Given a fraction with numerator 5 and denominator 7
    When I clone the fraction
    Then the cloned fraction value should equal the original

Scenario: Fraction from integer value
    Given a fraction from integer 3
    Then the fraction numerator should be 3
    And the fraction denominator should be 1
