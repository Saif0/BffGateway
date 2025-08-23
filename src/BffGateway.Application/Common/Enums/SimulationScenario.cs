using System.ComponentModel;

namespace BffGateway.Application.Common.Enums;

public enum SimulationScenario
{
    [Description("Normal operation - no simulation")]
    None = 0,

    [Description("Simulate server failure (500 error)")]
    Fail = 1,

    [Description("Simulate timeout delay")]
    Timeout = 2,

    [Description("Simulate rate limit exceeded (429 error)")]
    LimitExceeded = 3
}
