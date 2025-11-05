#if NK7_CONTAINER
using Cysharp.Threading.Tasks;
using System.Threading;
using Nk7.Container;
using System;

namespace Nk7.StateMachine
{
    public sealed class ContainerStateMachine<TTrigger> : StateMachine<TTrigger>
        where TTrigger : Enum
    {
        private readonly IFactoryService<IState<TTrigger>> _statesFactory;
        private readonly IScopeService _scopeService;

        private int _scope;

        [UnityEngine.Scripting.Preserve]
        public ContainerStateMachine(IFactoryService<IState<TTrigger>> statesFactory, IScopeService scopeService)
            : base()
        {
            _statesFactory = statesFactory;
            _scopeService = scopeService;
        }

        public override async UniTask PushAsync<TPayload>(TTrigger trigger, TPayload payload, CancellationToken cancellationToken)
        {
            if (!TryGetStateType(trigger, out var stateType))
            {
                return;
            }

            int newScope = _scopeService.CreateScope();
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
                _scopeService.ReleaseScope(_scope);
            }

            CurrentState = payloadedState;
            CurrentTrigger = trigger;

            _scope = newScope;

            await payloadedState.OnEnterAsync(trigger, payload, cancellationToken);
        }
    }
}
#endif