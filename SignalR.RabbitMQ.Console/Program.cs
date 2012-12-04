
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
//using Newtonsoft.Json;
using RabbitMQ.Client;
using SignalR.RabbitMq.Example;

namespace SignalR.RabbitMQ.Console
{

    class Program
    {
        public static RabbitConnection _rabbitConnection;



        static void Main(string[] args)
        {
            var connectionFactory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest"
            };

            var rabbitMqExchangeName = "SignalRExchange";

            GlobalHost.DependencyResolver.UseRabbitMq(connectionFactory, rabbitMqExchangeName);            
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<Chat>();
            
            _rabbitConnection = new RabbitConnection(connectionFactory, rabbitMqExchangeName);
            /*
              var eavesdropper = new RabbitConnectionEavesdropper(_rabbitConnection);
            
              //we are going to listen in to messages sent to and from the Chat Hub in the web project.
              eavesdropper.ListenInOnClientMessages("userJoined", (origin, invocation) => { System.Console.WriteLine("User joined with connectionid : {0}", invocation.Args[0]); });
              eavesdropper.ListenInOnClientMessages("onDisconnected", (origin, invocation) => { System.Console.WriteLine("User disconnected with connectionid : {0}", invocation.Args[0]); });
            
              eavesdropper.ListenInOnClientMessages("addMessage", (origin, invocation) =>
                                                                      {
                                                                          var receivedMessage = invocation.Args[0];
                                                                          System.Console.WriteLine("Message sent from the web application : {0}", receivedMessage);
                                                                          context.Clients.Client(origin).onConsoleMessage(string.Format("You said - {0} (from the console application.)",receivedMessage));
                                                                          context.Clients.AllExcept(origin).onConsoleMessage(string.Format("Somebody else just said {0}.)", receivedMessage));
                                                                        
                                                                      });
            */
            _rabbitConnection.StartListening();
              
            System.Console.ReadLine();
        }
    }
}
