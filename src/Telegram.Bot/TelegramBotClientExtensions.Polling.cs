﻿using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Telegram.Bot.Extensions;
using Telegram.Bot.Polling;
using ErrorContext = Telegram.Bot.Polling.ErrorContext;

// ReSharper disable once CheckNamespace
namespace Telegram.Bot;

/// <summary>
/// Provides extension methods for <see cref="ITelegramBotClient"/> that allow for <see cref="Update"/> polling
/// </summary>
[PublicAPI]
public static partial class TelegramBotClientExtensions
{
    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> for each.
    /// <para>
    /// This method does not block. GetUpdates will be called AFTER the
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> returns
    /// </para>
    /// </summary>
    /// <typeparam name="TUpdateHandler">
    /// The <see cref="IUpdateHandler"/> used for processing <see cref="Update"/>s
    /// </typeparam>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="receiverOptions">Options used to configure getUpdates request</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    public static void StartReceiving<TUpdateHandler>(
        this ITelegramBotClient botClient,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default
    ) where TUpdateHandler : IUpdateHandler, new() =>
        StartReceiving(
            botClient: botClient,
            updateHandler: new TUpdateHandler(),
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );

    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking  <paramref name="updateHandler"/>
    /// for each.
    /// <para>
    /// This method does not block. GetUpdates will be called AFTER the <paramref name="updateHandler"/> returns
    /// </para>
    /// </summary>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing <see cref="Update"/>s</param>
    /// <param name="errorHandler">Delegate used for processing errors</param>
    /// <param name="receiverOptions">Options used to configure getUpdates request</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    public static void StartReceiving(
        this ITelegramBotClient botClient,
        Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
        Func<ITelegramBotClient, ErrorContext, CancellationToken, Task> errorHandler,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default
    ) =>
        StartReceiving(
            botClient: botClient,
            updateHandler: new DefaultUpdateHandler(
                updateHandler: updateHandler,
                errorHandler: errorHandler
            ),
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );

    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking  <paramref name="updateHandler"/>
    /// for each.
    /// <para>
    /// This method does not block. GetUpdates will be called AFTER the <paramref name="updateHandler"/> returns
    /// </para>
    /// </summary>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing <see cref="Update"/>s</param>
    /// <param name="pollingErrorHandler">Delegate used for processing polling errors</param>
    /// <param name="receiverOptions">Options used to configure getUpdates request</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    public static void StartReceiving(
        this ITelegramBotClient botClient,
        Action<ITelegramBotClient, Update, CancellationToken> updateHandler,
        Action<ITelegramBotClient, ErrorContext, CancellationToken> pollingErrorHandler,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default
    ) =>
        StartReceiving(
            botClient: botClient,
            updateHandler: new DefaultUpdateHandler(
                updateHandler: (bot, update, token) =>
                {
                    updateHandler.Invoke(bot, update, token);
                    return Task.CompletedTask;
                },
                errorHandler: (bot, context, token) =>
                {
                    pollingErrorHandler.Invoke(bot, context, token);
                    return Task.CompletedTask;
                }
            ),
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );

    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> for each.
    /// <para>
    /// This method does not block. GetUpdates will be called AFTER the
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> returns
    /// </para>
    /// </summary>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="updateHandler">
    /// The <see cref="IUpdateHandler"/> used for processing <see cref="Update"/>s
    /// </param>
    /// <param name="receiverOptions">Options used to configure getUpdates request</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    public static void StartReceiving(
        this ITelegramBotClient botClient,
        IUpdateHandler updateHandler,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default)
    {
        botClient.ThrowIfNull();
        updateHandler.ThrowIfNull();

        // ReSharper disable once MethodSupportsCancellation
#pragma warning disable CA2016
        _ = Task.Run(async () =>
#pragma warning restore CA2016
        {
            await ReceiveAsync(
                botClient: botClient,
                updateHandler: updateHandler,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> for each.
    /// <para>
    /// This method will block if awaited. GetUpdates will be called AFTER the
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> returns
    /// </para>
    /// </summary>
    /// <typeparam name="TUpdateHandler">
    /// The <see cref="IUpdateHandler"/> used for processing <see cref="Update"/>s
    /// </typeparam>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="receiverOptions">Options used to configure getUpdates request</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that will be completed when cancellation will be requested through
    /// <paramref name="cancellationToken"/>
    /// </returns>
    public static async Task ReceiveAsync<TUpdateHandler>(
        this ITelegramBotClient botClient,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default
    ) where TUpdateHandler : IUpdateHandler, new() =>
        await ReceiveAsync(
            botClient: botClient,
            updateHandler: new TUpdateHandler(),
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> for each.
    /// <para>
    /// This method will block if awaited. GetUpdates will be called AFTER the <paramref name="updateHandler"/>
    /// returns
    /// </para>
    /// </summary>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing <see cref="Update"/>s</param>
    /// <param name="errorHandler">Delegate used for processing polling errors</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that will be completed when cancellation will be requested through
    /// <paramref name="cancellationToken"/>
    /// </returns>
    public static async Task ReceiveAsync(
        this ITelegramBotClient botClient,
        Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
        Func<ITelegramBotClient, ErrorContext, CancellationToken, Task> errorHandler,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default
    ) =>
        await ReceiveAsync(
            botClient: botClient,
            updateHandler: new DefaultUpdateHandler(
                updateHandler: updateHandler,
                errorHandler: errorHandler
            ),
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> for each.
    /// <para>
    /// This method will block if awaited. GetUpdates will be called AFTER the <paramref name="updateHandler"/>
    /// returns
    /// </para>
    /// </summary>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="updateHandler">Delegate used for processing <see cref="Update"/>s</param>
    /// <param name="pollingErrorHandler">Delegate used for processing polling errors</param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that will be completed when cancellation will be requested through
    /// <paramref name="cancellationToken"/>
    /// </returns>
    public static async Task ReceiveAsync(
        this ITelegramBotClient botClient,
        Action<ITelegramBotClient, Update, CancellationToken> updateHandler,
        Action<ITelegramBotClient, ErrorContext, CancellationToken> pollingErrorHandler,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default
    ) =>
        await ReceiveAsync(
            botClient: botClient,
            updateHandler: new DefaultUpdateHandler(
                updateHandler: (bot, update, token) =>
                {
                    updateHandler.Invoke(bot, update, token);
                    return Task.CompletedTask;
                },
                errorHandler: (bot, context, token) =>
                {
                    pollingErrorHandler.Invoke(bot, context, token);
                    return Task.CompletedTask;
                }
            ),
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

    /// <summary>
    /// Starts receiving <see cref="Update"/>s on the ThreadPool, invoking
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> for each.
    /// <para>
    /// This method will block if awaited. GetUpdates will be called AFTER the
    /// <see cref="IUpdateHandler.HandleUpdateAsync"/> returns
    /// </para>
    /// </summary>
    /// <param name="botClient">The <see cref="ITelegramBotClient"/> used for making GetUpdates calls</param>
    /// <param name="updateHandler">
    /// The <see cref="IUpdateHandler"/> used for processing <see cref="Update"/>s
    /// </param>
    /// <param name="receiverOptions">Options used to configure getUpdates requests</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> with which you can stop receiving
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that will be completed when cancellation will be requested through
    /// <paramref name="cancellationToken"/>
    /// </returns>
    public static async Task ReceiveAsync(
        this ITelegramBotClient botClient,
        IUpdateHandler updateHandler,
        ReceiverOptions? receiverOptions = default,
        CancellationToken cancellationToken = default
    ) =>
        await new DefaultUpdateReceiver(botClient: botClient, receiverOptions: receiverOptions)
            .ReceiveAsync(updateHandler: updateHandler, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
}
