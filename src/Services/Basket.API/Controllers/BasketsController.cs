﻿using AutoMapper;
using Basket.API.Entities;
using Basket.API.GrpcServices;
using Basket.API.Repositories.Interfaces;
using Basket.API.Services.Interfaces;
using EventBus.Messages.IntegrationEvents.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BasketsController : ControllerBase
    {
        private readonly IBasketRepository _basketRepository;
        //private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMapper _mapper;
        private readonly StockItemGrpcService _stockItemGrpcService;
        private readonly IEmailTemplateService _emailTemplateService;

        public BasketsController(
            IBasketRepository basketRepository,
            //IPublishEndpoint publishEndpoint,
            IMapper mapper,
            StockItemGrpcService stockItemGrpcService,
            IEmailTemplateService emailTemplateService)
        {
            _basketRepository = basketRepository;
            //_publishEndpoint = publishEndpoint;
            _mapper = mapper;
            _stockItemGrpcService = stockItemGrpcService;
            _emailTemplateService = emailTemplateService;
        }

        [HttpGet("{username}", Name = "GetBasket")]
        [ProducesResponseType(typeof(Cart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Cart>> GetBasketByUsername([Required]string username)
        {
            var result = await _basketRepository.GetBasketByUsername(username);
            return Ok(result ?? new Cart());
        }

        [HttpPost(Name = "UpdateBasket")]
        [ProducesResponseType(typeof(Cart), (int) HttpStatusCode.OK)]
        public async Task<ActionResult<Cart>> UpdateBasket([FromBody]Cart cart)
        {
            // Communicate with Inventory.Grpc and check quantity available of products
            foreach (var item in cart.Items)
            {
                var stock = await _stockItemGrpcService.GetStock(item.ItemNo);
                item.SetAvailableQuantity(stock.Quantity);
            }

            var options = new DistributedCacheEntryOptions()
                                .SetAbsoluteExpiration(DateTime.UtcNow.AddHours(1))
                                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            var result = await _basketRepository.UpdateBasket(cart, options);
            return Ok(result);
        }

        [HttpDelete("{username}", Name = "DeleteBasket")]
        [ProducesResponseType(typeof(bool), (int) HttpStatusCode.OK)]
        public async Task<ActionResult<bool>> DeleteBasket([Required] string username)
        {
            var result = await _basketRepository.DeleteBasketFromUsername(username);
            return Ok(result);
        }

        [Route("[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Checkout([FromBody] BasketCheckout basketCheckout)
        {
            var basket = await _basketRepository.GetBasketByUsername(basketCheckout.UserName);
            if (basket == null) return NotFound();

            // publish checkout event to Eventbus Message
            var eventMessage = _mapper.Map<BasketCheckoutEvent>(basketCheckout);
            eventMessage.TotalPrice = basket.TotalPrice;

            //await _publishEndpoint.Publish(eventMessage);

            // remove the basket
            await _basketRepository.DeleteBasketFromUsername(basketCheckout.UserName);

            return Accepted();
        }

        [HttpPost("[action]", Name = "SendEmailReminder")]
        public ContentResult SendEmailReminder()
        {
            var emailTemplate = _emailTemplateService.GenerateReminderCheckoutOrderEmail("Tester");
            var result = new ContentResult
            {
                Content = emailTemplate,
                ContentType = "text/html" 
            };

            return result;
        }
    }
}
