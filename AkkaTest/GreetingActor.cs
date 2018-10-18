using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace AkkaTest
{
    public class GreetingActor: ReceiveActor
    {
        public GreetingActor()
        {
            Receive<GreetingMessage>(greet => Console.WriteLine("Hello World"));
        }
    }
}
