﻿using Basket.API.Services.Interfaces;
using Shared.Configurations;

namespace Basket.API.Services
{
    public class BasketEmailTemplateService : EmailTemplateService, IEmailTemplateService
    {
        public BasketEmailTemplateService(BackgroundJobSettings backgroundJobSettings) : base(backgroundJobSettings)
        {
        }

        public string GenerateReminderCheckoutOrderEmail(string username, string checkoutUrl = "baskets/checkout")
        {
            var _checkoutUrl = $"{_backgroundJobSettings.ApiGwUrl}/{checkoutUrl}/{username}";
            var emailText = ReadEmailTemplateContent("reminder-checkout-order");
            var emailReplacedText = emailText.Replace("[username]", username)
                                             .Replace("[checkoutUrl]", _checkoutUrl);
            return emailReplacedText; 
        }
    }
}
