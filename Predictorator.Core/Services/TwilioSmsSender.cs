using Microsoft.Extensions.Options;
using Predictorator.Core.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Predictorator.Core.Services;

public class TwilioSmsSender : ITwilioSmsSender
{
    private readonly TwilioOptions _options;

    public TwilioSmsSender(IOptions<TwilioOptions> options)
    {
        _options = options.Value;
    }

    public Task SendSmsAsync(string to, string message)
    {
        TwilioClient.Init(_options.AccountSid, _options.AuthToken);
        return MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber(_options.FromNumber),
            body: message);
    }
}
