using Firewall.Chains;
using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firewall.Commands
{

    public class AddCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "Add";

        public virtual Table<T> Table { get; }

        private RuleCommandParser<T> _parser = new RuleCommandParser<T>();

        public AddCommand(Table<T> table)
        {
            Table = table;
        }

        public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length == 0)
                return;

            try
            {
                _parser.Rule = new T();
                await _parser.ExecuteAsync(args, helper);
                Table.Rules.AddLast(_parser.Rule);
                helper.Logger.Unpreformat("§aAdd success.");
            }
            catch (Exception e)
            {
                helper.Logger.Exception(e);
                helper.Logger.Unpreformat("§cAdd failed.");
            }
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _parser.GetTabCompletions(args);
        }
    }
}
