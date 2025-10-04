using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Nk7.StateMachine
{
    public interface IStateMachine<TTrigger> where TTrigger : Enum
    {
        TTrigger CurrentTrigger { get; }

        StateMachine<TTrigger>.StateMachineTrigger Register<T>(TTrigger trigger)
            where T : IState<TTrigger>;
        StateMachine<TTrigger>.StateMachineTrigger AllowTransition(TTrigger from, TTrigger to);
        UniTask PushAsync<TPayload>(TTrigger trigger, TPayload payload, CancellationToken cancellationToken)
            where TPayload : IPayload;
    }
}