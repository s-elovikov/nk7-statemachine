using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Nk7.StateMachine
{
    public interface IStateMachine<TTrigger> where TTrigger : Enum
    {
        public readonly ref struct StateMachineTrigger
        {
            private readonly IStateMachine<TTrigger> _stateMachine;
            private readonly TTrigger _trigger;

            public StateMachineTrigger(IStateMachine<TTrigger> stateMachine, TTrigger trigger)
            {
                _stateMachine = stateMachine;
                _trigger = trigger;
            }

            public StateMachineTrigger AllowTransition(TTrigger trigger)
            {
                _stateMachine.AllowTransition(_trigger, trigger);
                return this;
            }
        }


        TTrigger CurrentTrigger { get; }

        StateMachineTrigger Register<T>(TTrigger trigger)
            where T : IState<TTrigger>;
        StateMachineTrigger AllowTransition(TTrigger from, TTrigger to);
        UniTask PushAsync<TPayload>(TTrigger trigger, TPayload payload, CancellationToken cancellationToken)
            where TPayload : IPayload;
    }
}