using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Nk7.StateMachine
{
    public abstract class StateMachine<TTrigger> : IStateMachine<TTrigger>
        where TTrigger : Enum
    {
        public IState<TTrigger> CurrentState { get; protected set; }
        public TTrigger CurrentTrigger { get; protected set; }

        private readonly Dictionary<TTrigger, HashSet<TTrigger>> _transitions;
        private readonly Dictionary<TTrigger, Type> _stateTypes;

        public StateMachine()
        {
            _transitions = new Dictionary<TTrigger, HashSet<TTrigger>>();
            _stateTypes = new Dictionary<TTrigger, Type>();
        }

        public abstract UniTask PushAsync<TPayload>(TTrigger trigger, TPayload payload, CancellationToken cancellationToken)
            where TPayload : IPayload;

        public IStateMachine<TTrigger>.StateMachineTrigger Register<T>(TTrigger trigger)
            where T : IState<TTrigger>
        {
            _stateTypes[trigger] = typeof(T);

            return new IStateMachine<TTrigger>.StateMachineTrigger(this, trigger);
        }

        public IStateMachine<TTrigger>.StateMachineTrigger AllowTransition(TTrigger from, TTrigger to)
        {
            if (!_transitions.TryGetValue(from, out var transitions))
            {
                transitions = new HashSet<TTrigger>();
                _transitions.Add(from, transitions);
            }

            transitions.Add(to);

            return new IStateMachine<TTrigger>.StateMachineTrigger(this, from);
        }
        
        protected bool TryGetStateType(TTrigger trigger, out Type stateType)
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