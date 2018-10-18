using Akka.Actor;
using System;

namespace AkkaTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a new actor system (a container for your actors)
            var system = ActorSystem.Create("MySystem");

            // Create your actor and get a reference to it.
            var greeter = system.ActorOf<GreetingActor>("greeter");

            // Send a message to the actor
            greeter.Tell(new GreetingMessage());

            Console.ReadLine();
        }
    }
}
