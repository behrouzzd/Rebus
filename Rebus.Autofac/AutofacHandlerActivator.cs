﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.Autofac
{
    public class AutofacHandlerActivator : IContainerAdapter
    {
        readonly IContainer _container;

        public AutofacHandlerActivator(IContainer container)
        {
            _container = container;
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var lifetimeScope = transactionContext.Items
                .GetOrAdd("current-autofac-lifetime-scope", () =>
                {
                    var scope = _container.BeginLifetimeScope();

                    transactionContext.OnDisposed(() => scope.Dispose());

                    return scope;
                });

            var handledMessageTypes = typeof (TMessage).GetBaseTypes()
                .Concat(new[] {typeof (TMessage)});

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(handledMessageType);
                    var implementedInterfaceSequence = typeof(IEnumerable<>).MakeGenericType(implementedInterface);

                    return (IEnumerable<IHandleMessages>)lifetimeScope.Resolve(implementedInterfaceSequence);
                })
                .Cast<IHandleMessages<TMessage>>();
        }

        public void SetBus(IBus bus)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(bus).SingleInstance();
            containerBuilder.Update(_container);
        }
    }
}
