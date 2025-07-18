
namespace Moneytree.Account.Src.Internal.Schemas;

public class ServerResponse
{
    public string Message { get; set; } = "success";
    public dynamic? Data { get; set; } = null;
}
