using System;

namespace TicketEasy.Models;

public class TicketModel
{
    public string? Code { get; set; }
    public string? Category { get; set; }
    public string? CreateTime { get; set; }
    public string? ExpireTime { get; set; }
}
