using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityClient.Utilities
{
    public interface IConsoleCommand
    {
        string CommandWord { get; }
        bool Process(string[] args);
    }
    
    public abstract class ConsoleCommand : ScriptableObject, IConsoleCommand
    {
        [SerializeField] private string commandWord = string.Empty;

        public string CommandWord => commandWord;

        public abstract bool Process(string[] args);
    }

    public class Console
    {
        private readonly string _prefix;
        private readonly IEnumerable<IConsoleCommand> _commands;
        
        public Console(string prefix, IEnumerable<IConsoleCommand> commands)
        {
            _prefix = prefix;
            _commands = commands;
        }

        public void ProcessCommand(string inputValue)
        {
            if (!inputValue.StartsWith(_prefix))
            {
                return; 
            }

            inputValue = inputValue.Remove(0, _prefix.Length);
            string[] args = inputValue.Split(' ').Skip(1).ToArray();
            
            ProcessCommand(inputValue, args);
        }

        public void ProcessCommand(string commandInput, string[] args)
        {
            foreach (var command in _commands)
            {
                if (!commandInput.Equals(command.CommandWord, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (command.Process(args))
                {
                    return;
                }
            }
            
            //handle unfound command here
        }
    }
}