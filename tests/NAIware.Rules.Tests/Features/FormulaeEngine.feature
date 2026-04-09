Feature: Formulae Engine
    As a developer
    I want to use the Formulae engine to evaluate math expressions
    So that I can compute formula results from parameters

Scenario: Simple addition formula
    Given a formulae engine with parameters "a" set to 10 and "b" set to 5
    When I add a formula "a + b"
    And I evaluate formula 0
    Then the formula result should be 15

Scenario: Simple subtraction formula
    Given a formulae engine with parameters "a" set to 10 and "b" set to 3
    When I add a formula "a - b"
    And I evaluate formula 0
    Then the formula result should be 7

Scenario: Simple multiplication formula
    Given a formulae engine with parameters "x" set to 4 and "y" set to 5
    When I add a formula "x * y"
    And I evaluate formula 0
    Then the formula result should be 20

Scenario: Simple division formula
    Given a formulae engine with parameters "x" set to 20 and "y" set to 4
    When I add a formula "x / y"
    And I evaluate formula 0
    Then the formula result should be 5

Scenario: Parse and render a formula expression
    Given a formulae engine with parameters "a" set to 1 and "b" set to 2
    When I parse the formula "a + b"
    Then the rendered formula should contain "a"
    And the rendered formula should contain "b"
    And the rendered formula should contain "+"

Scenario: Formula with parentheses
    Given a formulae engine with parameters "a" set to 2 and "b" set to 3
    When I add a formula "( a + b ) * a"
    And I evaluate formula 0
    Then the formula result should be 10

Scenario: Single parameter formula
    Given a formulae engine with a single parameter "val" set to 42
    When I add a formula "val"
    And I evaluate formula 0
    Then the formula result should be 42
