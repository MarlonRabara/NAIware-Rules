using FluentAssertions;
using NAIware.Core.Collections;
using Reqnroll;

namespace NAIware.Core.Tests.StepDefinitions;

[Binding]
public class BinaryTreeNodeSteps
{
    private BinaryTreeNode<int>? _rootNode;

    [Given(@"a binary tree node with value (.*)")]
    public void GivenABinaryTreeNodeWithValue(int value)
    {
        _rootNode = new BinaryTreeNode<int>(value);
    }

    [When(@"I add a left child with value (.*)")]
    public void WhenIAddALeftChildWithValue(int value)
    {
        _rootNode!.LeftChild = new BinaryTreeNode<int>(value);
    }

    [When(@"I add a right child with value (.*)")]
    public void WhenIAddARightChildWithValue(int value)
    {
        _rootNode!.RightChild = new BinaryTreeNode<int>(value);
    }

    [When(@"I add a left grandchild with value (.*)")]
    public void WhenIAddALeftGrandchildWithValue(int value)
    {
        _rootNode!.LeftChild!.LeftChild = new BinaryTreeNode<int>(value);
    }

    [Then(@"the node value should be (.*)")]
    public void ThenTheNodeValueShouldBe(int expected)
    {
        _rootNode!.Value.Should().Be(expected);
    }

    [Then(@"the node should have no children")]
    public void ThenTheNodeShouldHaveNoChildren()
    {
        _rootNode!.HasChildren.Should().BeFalse();
    }

    [Then(@"the node should have children")]
    public void ThenTheNodeShouldHaveChildren()
    {
        _rootNode!.HasChildren.Should().BeTrue();
    }

    [Then(@"the left child value should be (.*)")]
    public void ThenTheLeftChildValueShouldBe(int expected)
    {
        _rootNode!.LeftChild!.Value.Should().Be(expected);
    }

    [Then(@"the right child value should be (.*)")]
    public void ThenTheRightChildValueShouldBe(int expected)
    {
        _rootNode!.RightChild!.Value.Should().Be(expected);
    }

    [Then(@"the tree depth should be (.*)")]
    public void ThenTheTreeDepthShouldBe(int expected)
    {
        _rootNode!.Depth.Should().Be(expected);
    }
}
