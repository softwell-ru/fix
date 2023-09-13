using Microsoft.Extensions.Logging;
using QuickFix;

namespace SoftWell.Fix.Initiator.Rt;

public class RtFixClient : FixClient, IRtFixClient
{
    public RtFixClient(
        SessionSettings sessionSettings,
        ILogger<RtFixClient> logger) : base(sessionSettings, logger)
    {
    }
}