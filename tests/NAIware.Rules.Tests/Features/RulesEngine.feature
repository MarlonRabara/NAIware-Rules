Feature: Rules Engine
    As a developer
    I want to use the rules engine to evaluate boolean expressions
    So that I can implement business rule logic

Scenario: Simple equality rule evaluates to true
    Given a rules engine with an integer parameter "Age" set to 25
    When I add a rule "Age = 25"
    And I execute the rules engine
    Then the executed rule count should be 1

Scenario: Simple equality rule evaluates to false
    Given a rules engine with an integer parameter "Age" set to 30
    When I add a rule "Age = 25"
    And I execute the rules engine
    Then the executed rule count should be 0

Scenario: Greater than comparison
    Given a rules engine with an integer parameter "Score" set to 85
    When I add a rule "Score > 70"
    And I execute the rules engine
    Then the executed rule count should be 1

Scenario: Less than or equal comparison
    Given a rules engine with an integer parameter "Price" set to 100
    When I add a rule "Price <= 100"
    And I execute the rules engine
    Then the executed rule count should be 1

Scenario: Not equal comparison
    Given a rules engine with an integer parameter "Status" set to 1
    When I add a rule "Status != 0"
    And I execute the rules engine
    Then the executed rule count should be 1

Scenario: Complex rule with AND
    Given a rules engine with integer parameters "Age" set to 25 and "Score" set to 90
    When I add a rule "Age >= 18 and Score > 80"
    And I execute the rules engine
    Then the executed rule count should be 1

Scenario: Complex rule with AND fails when one condition is false
    Given a rules engine with integer parameters "Age" set to 15 and "Score" set to 90
    When I add a rule "Age >= 18 and Score > 80"
    And I execute the rules engine
    Then the executed rule count should be 0

Scenario: Parse and render a rule expression
    Given a rules engine with an integer parameter "X" set to 10
    When I parse the rule "X > 5"
    Then the rendered expression should contain "X"
    And the rendered expression should contain ">"
    And the rendered expression should contain "5"

Scenario: Multiple rules with only some passing
    Given a rules engine with an integer parameter "Value" set to 50
    When I add a rule "Value > 100"
    And I add a rule "Value = 50"
    And I add a rule "Value < 10"
    And I execute the rules engine
    Then the executed rule count should be 1
