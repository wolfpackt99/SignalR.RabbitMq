using System.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.RabbitMQ
{
    public class RabbitConnection : IDisposable
    {
        private readonly ConnectionFactory _rabbitMqConnectionfactory;
        private readonly string _rabbitMqExchangeName;
        private IModel _channel;
        private Action<RabbitMqMessageWrapper> _handler;
        private CancellationTokenSource _cancellationTokenSource;
        
        public RabbitConnection(ConnectionFactory connectionfactory, string rabbitMqExchangeName)
        {
            _rabbitMqConnectionfactory = connectionfactory;
            _rabbitMqExchangeName = rabbitMqExchangeName;
        }

        public void OnMessage(Action<RabbitMqMessageWrapper> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            _handler = handler;
        }

        public Task Send(RabbitMqMessageWrapper message)
        {
            try
            {
                if (_channel.IsOpen)
                {
                    return Task.Factory.StartNew((msg) =>
                                                     {
                                                         _channel.BasicPublish(_rabbitMqExchangeName,
                                                                               message.Key,
                                                                               null,
                                                                               ((RabbitMqMessageWrapper) msg).GetBytes());
                                                         Debug.Write(">");
                                                     }, message
                    );
                }
                else
                {
                    throw new Exception("RabbitMQ channel is not open.");
                }
            }
            catch (Exception exception)
            {
                TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
                completionSource.SetException(exception);
                return completionSource.Task;
            }
        }

        public Task StartListening()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            var task = Task.Factory.StartNew(() =>
                        {
                            var connection = _rabbitMqConnectionfactory.CreateConnection();
                            _channel = connection.CreateModel();
                            _channel.ExchangeDeclare(_rabbitMqExchangeName, "topic", true);

                            var queue = _channel.QueueDeclare("", false, false, true, null);
                            _channel.QueueBind(queue.QueueName, _rabbitMqExchangeName, "#");

                            var consumer = new QueueingBasicConsumer(_channel);
                            _channel.BasicConsume(queue.QueueName, false, consumer);

                            while (_channel.IsOpen && !token.IsCancellationRequested)
                            {
                                try
                                {
                                    var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                                    _channel.BasicAck(ea.DeliveryTag, false);

                                    Task.Factory.StartNew((handler) =>
                                                              {
                                                                  var message =
                                                                      RabbitMqMessageWrapper.Deserialize(ea.Body);
                                                                  var hndlr = handler as Action<RabbitMqMessageWrapper>;
                                                                  if (hndlr != null)
                                                                  {
                                                                      hndlr.Invoke(message);
                                                                      Debug.Write("<");
                                                                  }

                                                              }, _handler);

                                }
                                catch (EndOfStreamException eose)
                                {
                                    //ignore
                                }
                            }
                        },token);

            return task;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}