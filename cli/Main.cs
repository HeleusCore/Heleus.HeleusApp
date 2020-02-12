using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;

namespace Heleus.Apps.HeleusApp
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            Command.RegisterCommand<ShowKeysCommand>();
            Command.RegisterCommand<ImportKeyCommand>();
            Command.RegisterCommand<ExportKeyCommand>();
            Command.RegisterCommand<RegisterCommand>();
            Command.RegisterCommand<BalanceCommand>();
            Command.RegisterCommand<TransferCommand>();
            Command.RegisterCommand<JoinCommand>();

            return await CLI.Run(args);
        }
    }
}
