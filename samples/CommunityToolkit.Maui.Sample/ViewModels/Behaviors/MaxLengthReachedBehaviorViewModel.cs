using System;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class MaxLengthReachedBehaviorViewModel : BaseViewModel
{
    string commandExecutions = string.Empty;

    public MaxLengthReachedBehaviorViewModel()
        => MaxLengthReachedCommand = new Command<string>(OnCommandExecuted);

    public ICommand MaxLengthReachedCommand { get; }

    public string CommandExecutions
    {
        get => commandExecutions;
        set => SetProperty(ref commandExecutions, value);
    }    

    void OnCommandExecuted(string text)
        => CommandExecutions += string.Format("MaxLength reached with value: '{0}'.", text) + Environment.NewLine;
}