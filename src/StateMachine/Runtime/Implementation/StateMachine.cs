using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

#if NK7_CONTAINER
using Nk7.Container;
#endif

namespace Nk7.StateMachine
{
    public sealed class StateMachine<TTrigger> : IStateMachine<TTrigger>
        where TTrigger : Enum
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


        public IState<TTrigger> CurrentState { get; private set; }
        public TTrigger CurrentTrigger { get; private set; }

#if NK7_CONTAINER
        private readonly IFactoryService<IState<TTrigger>> _statesFactory;
        private readonly IScopeService _scopeService;
#else
        private readonly IStatesFactoryService<IState<TTrigger>> _statesFactory;
#endif

        private readonly Dictionary<TTrigger, HashSet<TTrigger>> _transitions;
        private readonly Dictionary<TTrigger, Type> _stateTypes;

#if NK7_CONTAINER
        private int _scope;
#endif

#if NK7_CONTAINER
        [UnityEngine.Scripting.Preserve]
        public StateMachine(IFactoryService<IState<TTrigger>> statesFactory, IScopeService scopeService) : this()
        {
            _statesFactory = statesFactory;
            _scopeService = scopeService;
        }
#else
        public StateMachine(IStatesFactoryService<IState<TTrigger>> statesFactory) : this()
        {
            _statesFactory = statesFactory;
        }
#endif

        public StateMachine()
        {
            _transitions = new Dictionary<TTrigger, HashSet<TTrigger>>();
            _stateTypes = new Dictionary<TTrigger, Type>();
        }

        public StateMachineTrigger Register<T>(TTrigger trigger) where T : IState<TTrigger>
        {
            _stateTypes[trigger] = typeof(T);

            return new StateMachineTrigger(this, trigger);
        }

        public StateMachineTrigger AllowTransition(TTrigger from, TTrigger to)
        {
            if (!_transitions.TryGetValue(from, out var transitions))
            {
                transitions = new HashSet<TTrigger>();
                _transitions.Add(from, transitions);
            }

            transitions.Add(to);

            return new StateMachineTrigger(this, from);
        }

        public async UniTask PushAsync<TPayload>(TTrigger trigger, TPayload payload, CancellationToken cancellationToken)
            where TPayload : IPayload
        {
            if (!TryGetStateType(trigger, out var stateType))
            {
                return;
            }

#if NK7_CONTAINER
            int newScope = _scopeService.CreateScope();
#endif
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
#if NK7_CONTAINER
                _scopeService.ReleaseScope(_scope);
#endif
            }

            CurrentState = payloadedState;
            CurrentTrigger = trigger;

#if NK7_CONTAINER
            _scope = newScope;
#endif

            await payloadedState.OnEnterAsync(trigger, payload, cancellationToken);
        }

        private bool TryGetStateType(TTrigger trigger, out Type stateType)
        {
            if (!_stateTypes.TryGetValue(trigger, out stateType))
            {
                return false;
            }

            if (CurrentState != null)
            {
                if (!_transitions.TryGetValue(CurrentTrigger, out var transitions))
                {
                    throw new InvalidOperationException($"No allowed transitions from current state - {CurrentTrigger}");
                }

                if (!transitions.Contains(trigger))
                {
                    throw new InvalidOperationException($"Transition from current state - {CurrentTrigger}, to - {trigger}: not allowed");
                }
            }

            return true;
        }
    }
}