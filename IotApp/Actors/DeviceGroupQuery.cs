using System;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using IotApp.Protocols;

namespace IotApp.Actors
{
    public class DeviceGroupQuery : UntypedActor
    {
        private ICancelable queryTimeoutTimer;

        public DeviceGroupQuery(IImmutableDictionary<IActorRef, string> actorToDeviceId, long requestId, IActorRef requester, TimeSpan timeout)
        {
            ActorToDeviceId = actorToDeviceId;
            RequestId = requestId;
            Requester = requester;
            Timeout = timeout;

            Become(WaitingForReplies(ImmutableDictionary<string, ITemperatureReading>.Empty, ActorToDeviceId.Keys.ToImmutableHashSet()));

            queryTimeoutTimer = Context.System.Scheduler.ScheduleTellOnceCancelable(timeout, Self, CollectionTimeout.Instance, Self);
        }

        protected override void PreStart()
        {
            foreach (var deviceActor in ActorToDeviceId.Keys)
            {
                Context.Watch(deviceActor);
                deviceActor.Tell(new ReadTemperature(0));
            }
        }

        protected override void PostStop()
        {
            queryTimeoutTimer.Cancel();
        }

        protected ILoggingAdapter Log { get; } = Context.GetLogger();
        public IImmutableDictionary<IActorRef, string> ActorToDeviceId { get; }
        public long RequestId { get; }
        public IActorRef Requester { get; }
        public TimeSpan Timeout { get; }

        protected override void OnReceive(object message) { }

        public UntypedReceive WaitingForReplies(
            IImmutableDictionary<string, ITemperatureReading> repliesSoFar,
            IImmutableSet<IActorRef> stillWaiting)
        {
            return message =>
            {
                switch (message)
                {
                    case RespondTemperature response when response.RequestId == 0:
                        var deviceActor = Sender;
                        ITemperatureReading reading = null;
                        if (response.Value.HasValue)
                        {
                            reading = new Temperature(response.Value.Value);
                        }
                        else
                        {
                            reading = TemperatureNotAvailable.Instance;
                        }
                        ReceivedResponse(deviceActor, reading, stillWaiting, repliesSoFar);
                        break;
                    case Terminated t:
                        ReceivedResponse(t.ActorRef, DeviceNotAvailable.Instance, stillWaiting, repliesSoFar);
                        break;
                    case CollectionTimeout _:
                        var replies = repliesSoFar;
                        foreach (var actor in stillWaiting)
                        {
                            var deviceId = ActorToDeviceId[actor];
                            replies = replies.Add(deviceId, DeviceTimedOut.Instance);
                        }
                        Requester.Tell(new RespondAllTemperatures(RequestId, replies));
                        Context.Stop(Self);
                        break;
                }
            };
        }

        public void ReceivedResponse(
            IActorRef deviceActor,
            ITemperatureReading reading,
            IImmutableSet<IActorRef> stillWaiting,
            IImmutableDictionary<string, ITemperatureReading> repliesSoFar)
        {
            Context.Unwatch(deviceActor);
            var deviceId = ActorToDeviceId[deviceActor];

            var newStillWaiting = stillWaiting.Remove(deviceActor);
            var newRepliesSoFar = repliesSoFar.Add(deviceId, reading);

            if (newStillWaiting.Count == 0)
            {
                Requester.Tell(new RespondAllTemperatures(RequestId, newRepliesSoFar));
                Context.Stop(Self);
            }
            else
            {
                Context.Become(WaitingForReplies(newRepliesSoFar, newStillWaiting));
            }
        }

        public static Props Props(IImmutableDictionary<IActorRef, string> actorToDeviceId, long requestId, IActorRef requester, TimeSpan timeout) =>
            Akka.Actor.Props.Create(() => new DeviceGroupQuery(actorToDeviceId, requestId, requester, timeout));
    }
}