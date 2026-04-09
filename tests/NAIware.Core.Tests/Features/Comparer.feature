Feature: Comparer
    As a developer
    I want to use the reflection-based Comparer
    So that I can deep-compare objects by their fields and properties

Scenario: Equal objects compare as zero
    Given two identical test objects with Name "Alice" and Age 30
    When I compare them using the comparer
    Then the comparison result should be 0

Scenario: Objects with different field values are not equal
    Given a test object with Name "Alice" and Age 30
    And a second test object with Name "Alice" and Age 25
    When I compare them using the comparer
    Then the comparison result should not be 0

Scenario: Null objects compare as equal
    Given two null test objects
    When I compare them using the comparer
    Then the comparison result should be 0

Scenario: Null vs non-null returns negative
    Given a null first test object
    And a second test object with Name "Bob" and Age 40
    When I compare them using the comparer
    Then the comparison result should be -1

Scenario: Compare using properties only
    Given two identical test objects with Name "Charlie" and Age 50
    When I compare them using properties only
    Then the comparison result should be 0

Scenario: Compare with excluded members
    Given a test object with Name "Dave" and Age 30
    And a second test object with Name "Dave" and Age 99
    When I compare them excluding "Age"
    Then the comparison result should be 0
