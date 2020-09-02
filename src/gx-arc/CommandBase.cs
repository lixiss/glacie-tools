using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

using Glacie.ExtendedConsole;

namespace Glacie.Tools.Arc
{
    internal abstract class CommandBase
    {
        protected InvocationContext Context { get; }

        protected IConsole Console => Context.Console;

        protected CommandBase(InvocationContext context)
        {
            Context = context;
        }

        protected ProgressBar CreateProgressBar()
        {
            return new ConsoleProgressBar();
        }
    }
}
