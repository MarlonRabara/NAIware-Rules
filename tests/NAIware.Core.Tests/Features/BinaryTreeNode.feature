Feature: BinaryTreeNode
    As a developer
    I want to use BinaryTreeNode for tree-based data structures
    So that I can build and traverse binary trees

Scenario: Create a binary tree node with a value
    Given a binary tree node with value 10
    Then the node value should be 10
    And the node should have no children

Scenario: Add left and right children
    Given a binary tree node with value 10
    When I add a left child with value 5
    And I add a right child with value 15
    Then the left child value should be 5
    And the right child value should be 15
    And the node should have children

Scenario: Calculate depth of a tree
    Given a binary tree node with value 10
    When I add a left child with value 5
    And I add a left grandchild with value 2
    Then the tree depth should be 3
