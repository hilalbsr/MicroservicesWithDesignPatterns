using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Order.API.DTOs;
using Order.API.Models;
using Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Order.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly IPublishEndpoint _publishEndpoint;

        //exchange değil de kuyruğa mesaj gönderme
        private readonly ISendEndpointProvider _sendEndpointProvider;

        public OrdersController(AppDbContext context, IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _sendEndpointProvider = sendEndpointProvider;
        }

        /// <summary>
        /// Create Order
        /// </summary>
        /// <param name="orderCreate"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Create(OrderCreateDto orderCreate)
        {
            #region CreateOrder

            var newOrder = new Models.Order
            {
                BuyerId = orderCreate.BuyerId,
                Status = OrderStatus.Suspend,
                Address = new Address { Line = orderCreate.Address.Line, Province = orderCreate.Address.Province, District = orderCreate.Address.District },
                CreatedDate = DateTime.Now
            };

            orderCreate.orderItems.ForEach(item =>
            {
                newOrder.Items.Add(new OrderItem() { Price = item.Price, ProductId = item.ProductId, Count = item.Count });
            });

            await _context.AddAsync(newOrder);

            await _context.SaveChangesAsync();

            #endregion

            #region PublishCreateOrder

            var orderCreatedEvent = new OrderCreatedEvent()
            {
                BuyerId = orderCreate.BuyerId,
                OrderId = newOrder.Id,
                Payment = new PaymentMessage
                {
                    CardName = orderCreate.payment.CardName,
                    CardNumber = orderCreate.payment.CardNumber,
                    Expiration = orderCreate.payment.Expiration,
                    CVV = orderCreate.payment.CVV,
                    TotalPrice = orderCreate.orderItems.Sum(x => x.Price * x.Count)
                },
            };

            orderCreate.orderItems.ForEach(item =>
            {
                orderCreatedEvent.OrderItems.Add(new OrderItemMessage
                {
                    Count = item.Count, 
                    ProductId = item.ProductId
                });
            });

            //kuyruğa göndermek için
            //Eğer bu mesaja subscribe olan yoksa boşa gider mesaj.
            //Direk exchange gider.Kuyruğa değil.
            //Buradaki dataları almak için mutlaka subscribe olmak lazım.
            await _publishEndpoint.Publish(orderCreatedEvent);

            //Sadece 1 servis dinlicekse
            //exchange değil de kuyruğa mesaj gönderme
            //await _sendEndpointProvider.Send

            #endregion

            return Ok();
        }
    }
}