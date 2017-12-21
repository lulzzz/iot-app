using System;
using Akka;
using Akka.Actor;
using Akka.Configuration;
using IotApp.Actors;

namespace IotApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("my-actor-server"))
            {
                var supervisor = system.ActorOf<IotSupervisor>("iot-supervisor");
                Console.ReadKey();
            }
        }
    }
}
