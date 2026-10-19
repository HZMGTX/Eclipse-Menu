using Eclipse.Managers.DiscordRPC.RPC.Payload;

namespace Eclipse.Managers.DiscordRPC.RPC.Commands
{
    internal interface ICommand
    {
        IPayload PreparePayload(long nonce);
    }
}
