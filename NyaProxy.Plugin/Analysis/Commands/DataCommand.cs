using NyaProxy.API.Command;

namespace Analysis.Commands
{
    public class DataCommand : Command
    {
        public override string Name => "data";
        public DataCommand()
        {
            RegisterChild(new SimpleCommand("save", (args, helper) => AnalysisData.SaveAsync()));
            RegisterChild(new SimpleCommand("clear", async (args, helper) => AnalysisData.Clear()));
        }
    }
}