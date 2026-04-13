using CoachingFit.Identity.Shared.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoachingFit.Identity.Services.Abstraction
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailMessage email);

    }
}
