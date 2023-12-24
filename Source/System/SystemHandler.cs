using JetBrains.Annotations;
using MediatR;

namespace AspNetCore.API.System;

public enum SomeSystem : byte
{
    One,
    Two
}

public interface ISomeRequest<out TResponse> : ISystemRequest<SomeSystem, TResponse> { }

[UsedImplicitly]
public interface IOneRequestHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : ISomeRequest<TResponse> { }

[UsedImplicitly]
public interface ITwoRequestHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : ISomeRequest<TResponse> { }