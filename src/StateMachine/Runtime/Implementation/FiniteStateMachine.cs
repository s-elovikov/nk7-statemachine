using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Nk7.StateMachine
{
    public sealed class FiniteStateMachine<TTrigger> : StateMachine<TTrigger>
        where TTrigger : Enum
    {
        private readonly IStatesFactoryService<IState<TTrigger>> _statesFactory;

        public FiniteStateMachine(IStatesFactoryService<IState<TTrigger>> statesFactory)
            : base()
        {
            _statesFactory = statesFactory;
        }

        public override async UniTask PushAsync<TPayload>(TTrigger trigger, TPayload payload, CancellationToken cancellationToken)
        {
            if (!TryGetStateType(trigger, out var stateType))
            {
                return;
            }

            bool currentStateIsExist = CurrentState != null;

            var state = _statesFactory.GetService(stateType);
            var payloadedState = (IPayloadedState<TTrigger, TPayload>)state;

            if (currentStateIsExist)
            {
                await CurrentState.OnBeforeExitAsync(CurrentTrigger, trigger, cancellationToken);
            }

            await payloadedState.OnBeforeEnterAsync(trigger, payload, cancellationToken);

            if (currentStateIsExist)
            {
                await CurrentState.OnExitAsync(CurrentTrigger, trigger, cancellationToken);
            }

            CurrentState = payloadedState;
            CurrentTrigger = trigger;

            await payloadedState.OnEnterAsync(trigger, payload, cancellationToken);
        }
	}
}