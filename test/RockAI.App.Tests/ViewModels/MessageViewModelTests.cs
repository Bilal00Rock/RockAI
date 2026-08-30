using FluentAssertions;
using RockAI.App.ViewModels;
using RockAI.Common.Tests.Builders;
using RockAI.Domain.Messages;

namespace RockAI.App.Tests.ViewModels;

public sealed class MessageViewModelTests
{
    [Fact]
    public void CanRetry_WhenFailed_IsTrue()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Failed)
            .WithContent("partial")
            .Build();
        var vm = new MessageViewModel(message, retryAction: _ => Task.CompletedTask);

        vm.CanRetry.Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WhenCancelled_IsTrue()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Cancelled)
            .WithContent("partial")
            .Build();
        var vm = new MessageViewModel(message, retryAction: _ => Task.CompletedTask);

        vm.CanRetry.Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WhenCompleted_IsFalse()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Completed)
            .WithContent("done")
            .Build();
        var vm = new MessageViewModel(message, retryAction: _ => Task.CompletedTask);

        vm.CanRetry.Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WhenNoRetryAction_IsFalse()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Failed)
            .WithContent("x")
            .Build();
        var vm = new MessageViewModel(message);

        vm.CanRetry.Should().BeFalse();
    }

    [Fact]
    public void CanEdit_WhenUserAndActionsEnabled_IsTrue()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.User)
            .WithStatus(MessageStatus.Completed)
            .Build();
        var vm = new MessageViewModel(message, editAction: _ => Task.CompletedTask);

        vm.CanEdit.Should().BeTrue();
    }

    [Fact]
    public void CanEdit_WhenAssistant_IsFalse()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Completed)
            .WithContent("x")
            .Build();
        var vm = new MessageViewModel(message, editAction: _ => Task.CompletedTask);

        vm.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void CanEdit_WhenStreaming_IsFalse()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.User)
            .WithStatus(MessageStatus.Streaming)
            .Build();
        var vm = new MessageViewModel(message, editAction: _ => Task.CompletedTask);

        vm.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void CanEdit_WhenActionsDisabled_IsFalse()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.User)
            .WithStatus(MessageStatus.Completed)
            .Build();
        var vm = new MessageViewModel(message, editAction: _ => Task.CompletedTask);

        vm.SetActionsEnabled(false);

        vm.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void CanDelete_WhenNotStreaming_IsTrue()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.User)
            .WithStatus(MessageStatus.Completed)
            .Build();
        var vm = new MessageViewModel(message, deleteAction: _ => Task.CompletedTask);

        vm.CanDelete.Should().BeTrue();
    }

    [Fact]
    public void CanDelete_WhenStreaming_IsFalse()
    {
        var message = new Message(
            MessageRole.Assistant,
            "",
            Guid.NewGuid(),
            status: MessageStatus.Streaming);
        var vm = new MessageViewModel(message, deleteAction: _ => Task.CompletedTask);

        vm.CanDelete.Should().BeFalse();
    }

    [Fact]
    public void Append_ConcatenatesContent()
    {
        var message = new Message(
            MessageRole.Assistant,
            "Hello",
            Guid.NewGuid(),
            status: MessageStatus.Streaming);
        var vm = new MessageViewModel(message);

        vm.Append(" world");

        vm.Content.Should().Be("Hello world");
    }

    [Fact]
    public void ResetForRetry_ClearsContentAndSetsStreaming()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Failed)
            .WithContent("old")
            .Build();
        var vm = new MessageViewModel(message, retryAction: _ => Task.CompletedTask);

        vm.ResetForRetry();

        vm.Content.Should().BeEmpty();
        vm.Status.Should().Be(MessageStatus.Streaming);
        vm.CanRetry.Should().BeFalse();
    }

    [Fact]
    public void SetStatus_UpdatesStatusText()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
            .WithContent("")
            .Build();
        var vm = new MessageViewModel(message);

        vm.SetStatus(MessageStatus.Cancelled);
        vm.StatusText.Should().Be("[Stopped]");

        vm.SetStatus(MessageStatus.Failed);
        vm.StatusText.Should().Be("[Failed]");

        vm.SetStatus(MessageStatus.Completed);
        vm.StatusText.Should().BeEmpty();
    }

    [Fact]
    public void SetContent_ReplacesContent()
    {
        var message = new MessageBuilder().WithContent("old").Build();
        var vm = new MessageViewModel(message);

        vm.SetContent("new");

        vm.Content.Should().Be("new");
    }

    [Fact]
    public void Role_MapsFromMessageRoleName()
    {
        var message = new MessageBuilder().WithRole(MessageRole.User).Build();
        var vm = new MessageViewModel(message);

        vm.Role.Should().Be("User");
        vm.MessageRole.Should().Be(MessageRole.User);
    }
}
