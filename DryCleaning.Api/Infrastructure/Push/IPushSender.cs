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

public interface IPushSender
{
    /// <summary>Sends to the given FCM tokens. Returns tokens Firebase reported as no longer valid.</summary>
    Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct);
}
