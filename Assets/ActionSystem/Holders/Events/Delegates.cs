﻿using System.Collections.Generic;

namespace WorldActionSystem
{
    public delegate void CommandExecute(string stepName);
    public delegate void RegistCmds(IList<IActionCommand> cmds);
    public delegate void StepComplete(string stepName);
    public delegate void UserError(string step, string info);

    public delegate ElementController GetElementController();
}