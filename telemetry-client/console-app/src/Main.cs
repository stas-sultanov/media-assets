// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

var cancellationTokenSource = new CancellationTokenSource();

using var program = new Program();

await program.Run(cancellationTokenSource.Token);
