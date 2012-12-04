using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;

namespace SignalR.RabbitMQ
{
    public class RabbitConnectionEavesdropper
    {
        private readonly RabbitConnection _rabbitConnection;
        private readonly Dictionary<string,Action<string,ClientHubInvocation>> _handlers ;
        public RabbitConnectionEavesdropper(RabbitConnection rabbitConnection)
        {
            if (rabbitConnection == null)
            {
                throw new ArgumentNullException("rabbitConnection");
            }
            _rabbitConnection = rabbitConnection;
            _handlers = new Dictionary<string, Action<string, ClientHubInvocation>>();

            _rabbitConnection.OnMessage(
              (x) =>
              {
                  var messages = x.Messages;

                  foreach (var message in messages)
                  {
                      if (!IsSignalRServerMethod(message.Key) && message.Value != null)
                      {
                          var hubMessage =
                              JsonConvert.DeserializeObject<ClientHubInvocation>(message.Value);

                          if (!string.IsNullOrEmpty(hubMessage.Method) )
                          {
                              if(_handlers.ContainsKey(hubMessage.Method))
                              {
                                  var handler = _handlers[hubMessage.Method];
                                  handler.Invoke(message.Source, hubMessage);
                              }
                          }
                      }
                  }
              }
            );
            
        }

        public void ListenInOnClientMessages(string clientsideInvocation, Action<string,ClientHubInvocation> handler)
        {
            if (string.IsNullOrEmpty(clientsideInvocation))
            {
                throw new ArgumentNullException("clientsideInvocation");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            _handlers.Add(clientsideInvocation, handler);

        }

        private bool IsSignalRServerMethod(string key)
        {
            return key.Equals("__SIGNALR__SERVER__", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
