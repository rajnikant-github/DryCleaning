using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Push;

/// <summary>One push for one user (sent to all of that user's registered phones).</summary>
public sealed class PushMessage
{
    public required Guid NotificationId { get; init; }
    public required Guid UserId { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required Guid? OrderId { get; init; }
    public required string Type { get; init; }
}
