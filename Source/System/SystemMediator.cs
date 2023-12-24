using System.Collections.Concurrent;
using JetBrains.Annotations;
using MediatR;
using MediatR.Wrappers;

namespace AspNetCore.API.System;

public interface ISystemRequest<out TEnum, out TResponse> : IRequest<TResponse> where TEnum : Enum
{
    public TEnum System { get; }
}

[UsedImplicitly]
public sealed class SystemMediator<TEnum> : IMediator where TEnum : Enum
{
    private static readonly ConcurrentDictionary<(TEnum, Type), RequestHandlerBase> RequestHandlers = new();
    private readonly IServiceProvider _serviceProvider;

    public SystemMediator(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = new())
    {
        if (request is not ISystemRequest<TEnum, TResponse> systemRequest) throw new ArgumentNullException(nameof(request));

        var handler = (RequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd((systemRequest.System, request.GetType()), static requestType =>
        {
            Type wrapperType = typeof(SystemRequestHandlerWrapperImpl<,,>).MakeGenericType(typeof(TEnum), requestType.Item2, typeof(TResponse));
            object wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
            wrapperType.GetProperty("System")!.SetValue(wrapper, requestType.Item1);
            return (RequestHandlerBase)wrapper;
        });

        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = new()) where TRequest : IRequest => throw new NotImplementedException();

    public Task<object?> Send(object request, CancellationToken cancellationToken = new()) => throw new NotImplementedException();

    public IAsyncEnumerable<TResponse>
        CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = new()) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = new()) => throw new NotImplementedException();

    public Task Publish(object notification, CancellationToken cancellationToken = new()) => throw new NotImplementedException();

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = new())
        where TNotification : INotification =>
        throw new NotImplementedException();
}

internal sealed class SystemRequestHandlerWrapperImpl<TEnum, TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse> where TEnum : Enum
{
    public TEnum System { get; [UsedImplicitly] set; } = default!;

    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        await Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>)Handler, (next, pipeline) => () => pipeline.Handle((TRequest)request, next, cancellationToken))();

        Task<TResponse> Handler()
        {
            Type type = SystemMediatRHelper.SystemHandlerConfiguration<TEnum>.HandlerTypes[System];
            Type handlerType = type.MakeGenericType(typeof(TRequest), typeof(TResponse));
            var handler = (IRequestHandler<TRequest, TResponse>)serviceProvider.GetRequiredService(handlerType);
            return handler.Handle((TRequest)request, cancellationToken);
        }
    }
}