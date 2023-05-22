﻿using NyaFirewall.Chains;
using NyaFirewall.Rules;
using NyaFirewall.Tables;
using NyaProxy.API.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaFirewall.Commands
{

    public class AddCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "add";

        public virtual Table<T> Table { get; }

        private RuleCommandParser<T> _parser = new RuleCommandParser<T>("add");

        public AddCommand(Table<T> table)
        {
            Table = table;
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length == 0)
                return false;

            try
            {
                _parser.Rule = null;
                await _parser.ExecuteAsync(args, helper);
                if (_parser.Rule == null)
                    return true;
                Table.Rules.AddLast(_parser.Rule);
                helper.Logger.Unpreformat("§aAdd success.");
  
            }
            catch (Exception e)
            {
                if (e is CommandException)
                    throw;

                helper.Logger.Exception(e);
                helper.Logger.Unpreformat("§cAdd failed.");
            }
            return false;
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _parser.GetTabCompletions(args);
        }
    }
}
