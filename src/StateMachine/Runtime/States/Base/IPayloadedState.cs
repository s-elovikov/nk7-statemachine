using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Nk7.StateMachine
{
    public interface IPayloadedState<TTrigger, in TPayload> : IState<TTrigger>
        where TTrigger : Enum
        where TPayload : IPayload
    {
        UniTask OnBeforeEnterAsync(TTrigger state, TPayload payload, CancellationToken cancellationToken);
        UniTask OnEnterAsync(TTrigger state, TPayload payload, CancellationToken cancellationToken);
    }
}