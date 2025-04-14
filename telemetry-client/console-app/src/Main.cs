// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

var cancellationTokenSource = new CancellationTokenSource();

using var program = new Program();

var result = await program.Run(cancellationTokenSource.Token);

return result;
